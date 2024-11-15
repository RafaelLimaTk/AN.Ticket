using AN.Ticket.Application.DTOs.Ticket;
using AN.Ticket.WebUI.ViewModels.Asset;
using AN.Ticket.WebUI.ViewModels.Ticket;

namespace AN.Ticket.WebUI.ViewModels.Admin;

public class AdminViewModel
{
    public TicketMetricsDto TicketMetrics { get; set; }
    public AssetListViewModel Assets { get; set; }
    public TicketListViewModel Tickets { get; set; }
}
