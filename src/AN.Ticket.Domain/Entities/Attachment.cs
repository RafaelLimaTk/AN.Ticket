using AN.Ticket.Domain.Entities.Base;

namespace AN.Ticket.Domain.Entities;

public class Attachment : EntityBase
{
    public string? FileName { get; private set; }
    public byte[] Content { get; private set; }
    public string? ContentType { get; private set; }
    public Guid TicketId { get; private set; }
    public Ticket? Ticket { get; private set; }
    public Guid? TicketMessageId { get; private set; }
    public TicketMessage? TicketMessage { get; private set; }

    public long Size => Content?.Length ?? 0;

    protected Attachment() { }

    public Attachment(
        string fileName,
        byte[] content,
        string contentType,
        Guid ticketId,
        Guid? ticketMessageId = null
    )
    {
        FileName = fileName;
        Content = content;
        ContentType = contentType;
        TicketId = ticketId;
        TicketMessageId = ticketMessageId;
    }

    public void AssignToMessage(Guid ticketMessageId)
    {
        if (ticketMessageId == Guid.Empty)
            throw new ArgumentException("Ticket Message ID cannot be empty.", nameof(ticketMessageId));

        TicketMessageId = ticketMessageId;
    }
}
