using AN.Ticket.Application.DTOs.Email;
using AN.Ticket.Application.Helpers.EmailSender;
using AN.Ticket.Application.Interfaces;
using AN.Ticket.Domain.Entities;
using AN.Ticket.Domain.Enums;
using AN.Ticket.Domain.Extensions;
using AN.Ticket.Domain.Interfaces;
using AN.Ticket.Domain.Interfaces.Base;
using AN.Ticket.Hangfire.Enums;
using Hangfire;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Text.RegularExpressions;
using DomainEntity = AN.Ticket.Domain.Entities;

namespace AN.Ticket.Application.Services;
public class EmailMonitoringService : IEmailMonitoringService
{
    private readonly ILogger<EmailMonitoringService> _logger;
    private readonly SmtpSettings _smtpSettings;
    private readonly IContactRepository _contactRepository;
    private readonly IEmailSenderService _emailSenderService;
    private readonly ITicketRepository _ticketRepository;
    private readonly ITicketService _ticketService;
    private readonly ITicketMessageRepository _ticketMessageRepository;
    private readonly IAttachmentRepository _attachmentRepository;
    private readonly IChatGptService _chatGptService;
    private readonly IUnitOfWork _unitOfWork;

    public EmailMonitoringService(
        ILogger<EmailMonitoringService> logger,
        IOptions<SmtpSettings> smtpSettings,
        IContactRepository contactRepository,
        IEmailSenderService emailSenderService,
        ITicketRepository ticketRepository,
        ITicketService ticketService,
        ITicketMessageRepository ticketMessageRepository,
        IAttachmentRepository attachmentRepository,
        IChatGptService chatGptService,
        IUnitOfWork unitOfWork
    )
    {
        _smtpSettings = smtpSettings.Value;
        _contactRepository = contactRepository;
        _emailSenderService = emailSenderService;
        _ticketRepository = ticketRepository;
        _ticketService = ticketService;
        _ticketMessageRepository = ticketMessageRepository;
        _attachmentRepository = attachmentRepository;
        _chatGptService = chatGptService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [Queue(nameof(TypeQueue.synchronization))]
    public async Task StartMonitoringAsync(CancellationToken cancellationToken)
    {
        using var client = new ImapClient();

        try
        {
            await client.ConnectAsync(_smtpSettings.ImapHost, _smtpSettings.ImapPort, true, cancellationToken);
            await client.AuthenticateAsync(_smtpSettings.Username, _smtpSettings.Password, cancellationToken);

            var inbox = client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadWrite, cancellationToken);

            var uids = await inbox.SearchAsync(SearchQuery.NotSeen, cancellationToken);

            foreach (var uid in uids)
            {
                var message = await inbox.GetMessageAsync(uid, cancellationToken);
                MonitorEmailsAsync(message);

                await inbox.AddFlagsAsync(uid, MessageFlags.Seen, true, cancellationToken);
            }

            await Task.Delay(TimeSpan.FromSeconds(15), cancellationToken);

            BackgroundJob.Schedule<IEmailMonitoringService>(
                service => service.StartMonitoringAsync(cancellationToken),
                TimeSpan.FromSeconds(1));

            await client.DisconnectAsync(true, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error monitoring email");
            throw;
        }
    }

    private void MonitorEmailsAsync(MimeMessage email)
    {
        var fromAddress = email.From.Mailboxes.First().Address;
        var fromName = email.From.Mailboxes.First().Name;
        var subject = email.Subject;
        var body = email.TextBody;
        var priority = email.Priority;
        var messageId = email.MessageId;
        var attachments = GetAttachmentsFromEmail(email);

        BackgroundJob.Enqueue(() => ProcessEmailAsync(fromName, fromAddress, subject, body, priority, messageId, attachments));
    }

    public async Task ProcessEmailAsync(
        string fromName,
        string fromAddress,
        string subject,
        string body,
        MessagePriority priority,
        string messageId,
        List<EmailAttachment> attachments
    )
    {
        try
        {
            var contact = await _contactRepository.GetByEmailAsync(fromAddress);
            var ticketCode = ExtractTicketCode(body);
            DomainEntity.Ticket ticket = null;

            if (ticketCode.HasValue)
                ticket = await _ticketRepository.GetByTicketCodeAsync(ticketCode.Value);
            else
                ticket = await _ticketRepository.GetByEmailAndSubjectAsync(fromAddress, subject.Replace("Re: ", ""));

            var contactName = contact is not null ? contact.GetFullName() : fromName;

            var newMessages = new List<TicketMessage>();

            if (ticket is not null)
            {
                if (ticket.Status == TicketStatus.Closed)
                {

                }

                if (ticket.EmailMessageId is null)
                {
                    ticket.AssignMessageId(messageId);
                    _ticketRepository.Update(ticket);
                }

                var existingMessages = EmailParser.ParseEmailThread(body);
                //var formattedMessages = await _chatGptService.GenerateResponseAsync(body);
                await HandleAttachmentsAndAddMessages(ticket, existingMessages, attachments);

                foreach (var msg in existingMessages)
                {
                    msg.TicketId = ticket.Id;
                    var existingMessage = ticket.Messages?.FirstOrDefault(m => m.Message == msg.Message);
                    if (existingMessage is null)
                    {
                        newMessages.Add(msg);
                    }
                }

                if (newMessages.Any())
                {
                    await AddMessagesToTicket(ticket, newMessages);
                }

                //foreach (var msg in formattedMessages)
                //{
                //    var ticketMessage = new TicketMessage
                //    (
                //        msg.Mensagem,
                //        DateTime.Parse(msg.Data)
                //    );

                //    var existingMessage = ticket.Messages?.FirstOrDefault(m => m.Message == ticketMessage.Message);
                //    if (existingMessage is null)
                //    {
                //        newMessages.Add(ticketMessage);
                //    }
                //}

                //if (newMessages.Any())
                //{
                //    await AddMessagesToTicket(ticket, newMessages);
                //}

                return;
            }

            var ticketPriority = MapEmailPriorityToTicketPriority(priority);

            var newTicket = new DomainEntity.Ticket(
                fromName,
                contactName,
                fromAddress,
                contact is not null ? contact.Phone : "",
                subject.Replace("Re: ", ""),
                TicketStatus.Onhold,
                DateTime.UtcNow.ToLocal(),
                ticketPriority
            );

            newTicket.AssignMessageId(messageId);

            var userId = await _ticketService.GetUserWithLeastTicketsAsync();
            if (userId != Guid.Empty)
                newTicket.AssignUsers(userId);

            var messages = EmailParser.ParseEmailThread(body);
            messages.Select(msg => msg.TicketId = newTicket.Id);
            //var messages = await _chatGptService.GenerateResponseAsync(body);
            //var ticketMessages = messages.Select(msg => new TicketMessage
            //(
            //    msg.Mensagem,
            //    DateTime.Parse(msg.Data)
            //)).ToList();
            await HandleAttachmentsAndAddMessages(newTicket, messages, attachments);

            newTicket.AddMessages(messages);
            //newTicket.AddMessages(ticketMessages);

            await _ticketRepository.SaveAsync(newTicket);
            await _unitOfWork.CommitAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing email");
            throw;
        }
    }

    private async Task AddMessagesToTicket(DomainEntity.Ticket ticket, List<TicketMessage> messages)
    {
        await _ticketMessageRepository.SaveListAsync(messages);
        await _unitOfWork.CommitAsync();
    }

    private async Task HandleAttachmentsAndAddMessages(DomainEntity.Ticket ticket, List<TicketMessage> messages, List<EmailAttachment> attachments)
    {
        if (messages != null && messages.Count > 0)
        {
            int messageCount = messages.Count;

            for (int i = 0; i < attachments.Count; i++)
            {
                var messageIndex = i % messageCount;
                var message = messages[messageIndex];

                var ticketAttachment = new DomainEntity.Attachment(
                    attachments[i].FileName,
                    attachments[i].Content,
                    attachments[i].ContentType,
                    ticket.Id,
                    message.Id
                );

                ticket.AddAttachment(ticketAttachment);
                await _attachmentRepository.SaveAsync(ticketAttachment);
            }
        }
        else
        {
            var defaultMessage = new TicketMessage("Anexo recebido sem mensagem.", DateTime.UtcNow);
            defaultMessage.TicketId = ticket.Id;

            if (attachments.Any())
            {
                await _ticketMessageRepository.SaveAsync(defaultMessage);
                ticket.AddMessages(new List<TicketMessage> { defaultMessage });
            }

            foreach (var attachment in attachments)
            {
                var ticketAttachment = new DomainEntity.Attachment(
                    attachment.FileName,
                    attachment.Content,
                    attachment.ContentType,
                    ticket.Id,
                    defaultMessage.Id
                );

                ticket.AddAttachment(ticketAttachment);
                await _attachmentRepository.SaveAsync(ticketAttachment);
            }
        }

        await _unitOfWork.CommitAsync();
    }

    private List<EmailAttachment> GetAttachmentsFromEmail(MimeMessage emailMessage)
    {
        var attachments = new List<EmailAttachment>();

        foreach (var part in emailMessage.BodyParts)
        {
            if (part is MimePart mimePart && !string.IsNullOrEmpty(mimePart.FileName))
            {
                var isAttachment = mimePart.ContentDisposition != null &&
                                   (mimePart.ContentDisposition.Disposition == ContentDisposition.Attachment ||
                                    mimePart.ContentDisposition.Disposition == ContentDisposition.Inline);

                if (isAttachment)
                {
                    using var memoryStream = new MemoryStream();
                    mimePart.Content.DecodeTo(memoryStream);

                    attachments.Add(new EmailAttachment
                    (
                        mimePart.FileName,
                        memoryStream.ToArray(),
                        mimePart.ContentType.MimeType
                    ));
                }
            }
        }

        return attachments;
    }

    private TicketPriority MapEmailPriorityToTicketPriority(MessagePriority emailPriority)
    {
        return emailPriority switch
        {
            MessagePriority.Urgent => TicketPriority.High,
            MessagePriority.Normal => TicketPriority.Medium,
            MessagePriority.NonUrgent => TicketPriority.Low,
            _ => TicketPriority.Low,
        };
    }

    private int? ExtractTicketCode(string body)
    {
        var match = Regex.Match(body, @"#(\d+)");
        return match.Success ? int.Parse(match.Groups[1].Value) : (int?)null;
    }
}
