using AN.Ticket.Application.DTOs.User;
using AN.Ticket.Domain.Enums;

namespace AN.Ticket.Application.DTOs.Ticket;
public class TicketNotAssignedDto
{
    public Guid Id { get; set; }
    public string Subject { get; set; }
    public string ContactName { get; set; }
    public TicketPriority Priority { get; set; }
    public DateTime DueDate { get; set; }
    public TicketStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public int MessagesCount { get; set; }
    public int AttachmentsCount { get; set; }
    public UserDto? User { get; set; }
}
