using AN.Ticket.Application.DTOs.Email;
using MimeKit;

namespace AN.Ticket.Application.Interfaces;
public interface IEmailSenderService
{
    Task SendEmailAsync(string email, string subject, string message, List<MimePart>? embeddedImages = null, List<EmailAttachment>? attachments = null);
    Task SendEmailResponseAsync(string email, string subject, string message, string originalMessageId, List<EmailAttachment>? attachments = null, List<MimePart>? embeddedImages = null);
}
