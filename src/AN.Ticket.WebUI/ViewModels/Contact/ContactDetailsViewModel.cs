namespace AN.Ticket.WebUI.ViewModels.Contact;

public class ContactDetailsViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public string Mobile { get; set; }
    public string Department{ get; set; }
    public int? TotalTicketsonHold{get; set; }
    public int TotalTicketsAtribuied{ get; set; }
}
