using AN.Ticket.Domain.Enums;

namespace AN.Ticket.WebUI.ViewModels.Asset;

public class UserContactDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; }
    public UserContactType Type { get; set; }
}