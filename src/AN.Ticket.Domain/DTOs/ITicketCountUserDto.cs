namespace AN.Ticket.Domain.DTOs;
public interface ITicketCountUserDto
{
    Guid UserId { get; set; }
    int TicketCount { get; set; }
}
