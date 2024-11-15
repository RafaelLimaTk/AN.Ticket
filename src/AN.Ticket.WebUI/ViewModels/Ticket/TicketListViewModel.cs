using AN.Ticket.Application.DTOs.Ticket;

namespace AN.Ticket.WebUI.ViewModels.Ticket;

public class TicketListViewModel
{
    public IEnumerable<TicketNotAssignedDto> Tickets { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public string SearchTerm { get; set; }
    public string OrderBy { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
}
