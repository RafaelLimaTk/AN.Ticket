namespace AN.Ticket.WebUI.ViewModels.Activity;

public class ActivityActionViewModel
{
    public Guid Id { get; set; }
    public bool IsEditTicket { get; set; }
    public Guid? TicketId { get; set; }
}
