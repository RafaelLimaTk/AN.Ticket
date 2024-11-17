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
using MimeKit.Utils;
using System.Text.RegularExpressions;
using DomainEntity = AN.Ticket.Domain.Entities;

namespace AN.Ticket.Application.Services;
public class EmailMonitoringService : IEmailMonitoringService
{
    private readonly ILogger<EmailMonitoringService> _logger;
    private readonly BaseUrlSettings _baseUrlSettings;
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
        IOptions<BaseUrlSettings> baseUrlSettings,
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
        _baseUrlSettings = baseUrlSettings.Value;
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
                    string emailContent = $@"
                    <html>
                        <body style='font-family: Arial, sans-serif; color: #333; padding: 20px;'>
                            <div style='max-width: 600px; margin: auto; padding: 20px; border-radius: 10px; background-color: #ffffff; border: 1px solid #ddd;'>
            
                                <h2 style='color: #0056b3; text-align: center; font-size: 24px; font-weight: bold;'>Seu Ticket foi Fechado</h2>
                
                                <p style='font-size: 16px; color: #555;'>
                                    Olá {ticket.ContactName},
                                </p>
                
                                <p style='font-size: 16px; color: #555;'>
                                    Informamos que o ticket <strong>#{ticket.TicketCode}</strong> com o assunto <strong>{ticket.Subject}</strong> foi encerrado. 
                                    Caso ainda precise de assistência, você pode abrir um novo chamado. Nossa equipe estará pronta para atendê-lo.
                                </p>
                
                                <p style='font-size: 14px; color: #777; line-height: 1.6;'>
                                    Se precisar de ajuda adicional, você também pode entrar em contato conosco pelos seguintes canais:
                                </p>
                                <ul style='list-style-type: none; padding: 0; font-size: 14px; color: #555;'>
                                    <li><strong>Email:</strong> suporte.anatlasnetwork@gmail.com</li>
                                    <li><strong>Horário de Atendimento:</strong> Segunda a Sexta, das 8h às 18h</li>
                                </ul>
                
                                <hr style='border: 0; border-top: 1px solid #ddd; margin: 30px 0;' />
                
                                <p style='text-align: center; font-size: 12px; color: #aaa;'>
                                    Este é um e-mail automático. Por favor, não responda diretamente a esta mensagem.
                                </p>
                            </div>
                        </body>
                    </html>";

                    await _emailSenderService.SendEmailAsync(ticket.Email, "Ticket Fechado", emailContent);
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

                await _unitOfWork.CommitAsync();
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
            await SendEmailAsync(newTicket);
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
        try
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
                    await _ticketMessageRepository.SaveRangeAsync(messages);
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, "Erro ao criar o arquivo");
            throw;
        }
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

    private async Task SendEmailAsync(DomainEntity.Ticket ticket)
    {
        var code = await _ticketRepository.GetTicketCodeByIdAsync(ticket.Id);

        string imagePathOnHold = ticket.Status == TicketStatus.Onhold ? "/img/status/onhold-on.png" : "/img/status/onhold-on.png";
        string imagePathOpen = ticket.Status == TicketStatus.Open ? "/img/status/open-on.png" : "/img/status/open-off.png";
        string imagePathInProgress = ticket.Status == TicketStatus.InProgress ? "/img/status/progress-on.png" : "/img/status/progress-off.png";
        string imagePathClosed = ticket.Status == TicketStatus.Closed ? "/img/status/close-on.png" : "/img/status/close-off.png";

        var imageBytesOnHold = await GetImageBytesAsync(imagePathOnHold);
        var imageBytesOpen = await GetImageBytesAsync(imagePathOpen);
        var imageBytesInProgress = await GetImageBytesAsync(imagePathInProgress);
        var imageBytesClosed = await GetImageBytesAsync(imagePathClosed);

        var imageCidOnHold = MimeUtils.GenerateMessageId();
        var imageCidOpen = MimeUtils.GenerateMessageId();
        var imageCidInProgress = MimeUtils.GenerateMessageId();
        var imageCidClosed = MimeUtils.GenerateMessageId();

        string htmlContent = $@"
            <html>
                <body>
                    <div style='max-width: 600px; margin: auto; border: 1px solid #ddd; padding: 20px; border-radius: 8px;'>
                        <h2 style='color: #0056b3; text-align: center;'>Suporte ao Cliente</h2>
                        <h3>Olá {ticket.ContactName}</h3>
                        <p>Recebemos seu contato e um <strong>chamado de suporte</strong> foi aberto com sucesso! Nossa equipe está trabalhando para solucionar o seu problema o mais rápido possível.</p>
                        
                        <h3>Detalhes do Chamado:</h3>
                        <ul style='list-style-type: none; padding: 0;'>
                            <li><strong>Assunto:</strong> {ticket.Subject}</li>
                            <li><strong>Descrição:</strong> Olá, recebemos a sua solicitação, em breve a mesma estará sendo atendida.</li>
                            <li><strong>Código do Chamado:</strong> #{code}</li>
                            <li><strong>Data de Abertura:</strong> {DateTime.Now}</li>
                        </ul>

                        <h3>Contatos para Suporte:</h3>
                        <p>Você também pode entrar em contato conosco pelos seguintes canais de atendimento:</p>
                        <ul style='list-style-type: none; padding: 0;'>
                            <li><strong>Email:</strong> suporte.anatlasnetwork@gmail.com</li>
                            <li><strong>Horário de Atendimento:</strong> Segunda a Sexta, das 8h às 18h</li>
                        </ul>
                       <div style='margin-top: 20px; position: relative; text-align: center;'>
                            <!-- Linha tracejada -->
                            
                            <!-- Tabela para alinhar os ícones e textos -->
                            <table style='width: 100%; z-index: 1; position: relative; background-color: #fff;'>
                                <tr>
                                    <!-- Status: Em espera -->
                                    <td style='text-align: center; color: #aaa; padding: 0 10px;'>
                                        <img src='cid:{imageCidOnHold}' alt='Status Image' style='display: inline; width: 50px;' />
                                        <p style='margin-top: 5px;'>Em espera</p>
                                    </td>

                                    <!-- Status: Aberto -->
                                    <td style='text-align: center; color: #aaa; padding: 0 10px;'>
                                        <img src='cid:{imageCidOpen}' alt='Status Image' style='display: inline; width: 50px;' />
                                        <p style='margin-top: 5px;'>Aberto</p>
                                    </td>

                                    <!-- Status: Em progresso -->
                                    <td style='text-align: center; color: #aaa; padding: 0 10px;'>
                                        <img src='cid:{imageCidInProgress}' alt='Status Image' style='display: inline; width: 50px;' />
                                        <p style='margin-top: 5px;'>Em progresso</p>
                                    </td>

                                    <!-- Status: Fechado -->
                                    <td style='text-align: center; color: #aaa; padding: 0 10px;'>
                                        <img src='cid:{imageCidClosed}' alt='Status Image' style='display: inline; width: 50px;' />
                                        <p style='margin-top: 5px;'>Fechado</p>
                                    </td>
                                </tr>
                            </table>
                        </div>


                        <p style='font-size: 14px; color: #777;'>Atenciosamente,<br>Equipe de Suporte - AtlasNetworks</p>

                        <hr style='border: 0; border-top: 1px solid #ddd; margin-top: 20px;' />
                        <p style='font-size: 12px; color: #aaa; text-align: center;'>Este é um e-mail automático, por favor, não responda diretamente a esta mensagem.</p>
                        
                    </div>
                </body>
            </html>";
        var imageMimeParts = new List<MimePart>
        {
            new MimePart("image", "png")
            {
                Content = new MimeContent(new MemoryStream(imageBytesOnHold)),
                ContentId = imageCidOnHold,
                ContentDisposition = new ContentDisposition(ContentDisposition.Inline),
                ContentTransferEncoding = ContentEncoding.Base64
            },
            new MimePart("image", "png")
            {
                Content = new MimeContent(new MemoryStream(imageBytesOpen)),
                ContentId = imageCidOpen,
                ContentDisposition = new ContentDisposition(ContentDisposition.Inline),
                ContentTransferEncoding = ContentEncoding.Base64
            },
            new MimePart("image", "png")
            {
                Content = new MimeContent(new MemoryStream(imageBytesInProgress)),
                ContentId = imageCidInProgress,
                ContentDisposition = new ContentDisposition(ContentDisposition.Inline),
                ContentTransferEncoding = ContentEncoding.Base64
            },
            new MimePart("image", "png")
            {
                Content = new MimeContent(new MemoryStream(imageBytesClosed)),
                ContentId = imageCidClosed,
                ContentDisposition = new ContentDisposition(ContentDisposition.Inline),
                ContentTransferEncoding = ContentEncoding.Base64
            }
        };

        await _emailSenderService.SendEmailAsync(
            ticket.Email,
            ticket.Subject,
            htmlContent,
            imageMimeParts
        );
    }

    public async Task<byte[]> GetImageBytesAsync(string relativePath)
    {
        using (HttpClient client = new HttpClient())
        {
            var imageUrl = _baseUrlSettings.BaseUrl + relativePath;
            var response = await client.GetAsync(imageUrl);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }
            else
            {
                throw new Exception("Image not found at URL: " + imageUrl);
            }
        }
    }
}
