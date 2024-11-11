namespace AN.Ticket.Application.DTOs.Attachment;
public class AttachmentDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; }
    public string? ContentType { get; set; }
    public DateTime CreatedAt { get; set; }
    public long Size { get; set; }
    public Guid? TicketMessageId { get; private set; }
}
