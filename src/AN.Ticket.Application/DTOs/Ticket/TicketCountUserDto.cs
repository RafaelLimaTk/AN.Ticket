using AN.Ticket.Domain.DTOs;

namespace AN.Ticket.Application.DTOs.Ticket;
public class TicketCountUserDto : ITicketCountUserDto
{
    public Guid UserId { get; set; }
    public int TicketCount { get; set; }
}
