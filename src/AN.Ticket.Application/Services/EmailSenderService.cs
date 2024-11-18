using AN.Ticket.Application.DTOs.Email;
using AN.Ticket.Application.Helpers.EmailSender;
using AN.Ticket.Application.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Utils;

namespace AN.Ticket.Application.Services;
public class EmailSenderService : IEmailSenderService
{
    private readonly SmtpSettings _smtpSettings;

    public EmailSenderService(IOptions<SmtpSettings> smtpSettings)
    {
        _smtpSettings = smtpSettings.Value;
    }

    public async Task SendEmailAsync(string email, string subject, string message, List<MimePart>? embeddedImages = null, List<EmailAttachment>? attachments = null)
    {
        var senderName = email;
        var senderAddress = _smtpSettings.Username;

        var recipientAddress = email;

        var emailMessage = new MimeMessage();
        emailMessage.From.Add(new MailboxAddress(senderAddress, senderAddress));
        emailMessage.To.Add(new MailboxAddress(senderName, recipientAddress));
        emailMessage.Subject = subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = message };

        if (attachments != null && attachments.Any())
        {
            foreach (var attachment in attachments)
            {
                bodyBuilder.Attachments.Add(attachment.FileName, attachment.Content, ContentType.Parse(attachment.ContentType));
            }
        }
        if (embeddedImages != null && embeddedImages.Any())
        {
            foreach (var mimePart in embeddedImages)
            {
                bodyBuilder.LinkedResources.Add(mimePart);
            }
        }
        emailMessage.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_smtpSettings.Host, _smtpSettings.Port, false);
        await client.AuthenticateAsync(_smtpSettings.Username, _smtpSettings.Password);
        await client.SendAsync(emailMessage);
        await client.DisconnectAsync(true);
    }

    public async Task SendEmailResponseAsync(string email, string subject, string message, string originalMessageId, List<MimePart>? embeddedImages = null, List<EmailAttachment>? attachments = null)
    {
        var senderName = email;
        var senderAddress = _smtpSettings.Username;

        var recipientAddress = email;

        var emailMessage = new MimeMessage();
        emailMessage.From.Add(new MailboxAddress(senderAddress, senderAddress));
        emailMessage.To.Add(new MailboxAddress(senderName, recipientAddress));
        emailMessage.Subject = subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = message };

        if (attachments != null && attachments.Any())
        {
            foreach (var attachment in attachments)
            {
                bodyBuilder.Attachments.Add(attachment.FileName, attachment.Content, ContentType.Parse(attachment.ContentType));
            }
        }

        if (embeddedImages != null && embeddedImages.Any())
        {
            foreach (var mimePart in embeddedImages)
            {
                bodyBuilder.LinkedResources.Add(mimePart); // Incorpora as imagens
            }
        }

        emailMessage.Body = bodyBuilder.ToMessageBody();

        if (originalMessageId != null)
        {
            emailMessage.InReplyTo = originalMessageId;
            emailMessage.References.Add(originalMessageId);
        }

        using var client = new SmtpClient();
        await client.ConnectAsync(_smtpSettings.Host, _smtpSettings.Port, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(_smtpSettings.Username, _smtpSettings.Password);
        await client.SendAsync(emailMessage);
        await client.DisconnectAsync(true);
    }

}
