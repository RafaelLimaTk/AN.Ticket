using AN.Ticket.Application.Interfaces;
using AN.Ticket.WebUI.ViewModels.Contact;
using Microsoft.AspNetCore.Mvc;

namespace AN.Ticket.WebUI.Components;

public class ContactDetailsViewComponent
    : ViewComponent
{
    private readonly IContactService _contactService;

    public ContactDetailsViewComponent(
        IContactService contactService
    )
        => _contactService = contactService;

    public async Task<IViewComponentResult> InvokeAsync(Guid contactId)
    {
        if (contactId == Guid.Empty)
        {
            return View(new ContactDetailsViewModel());
        }

        var contact = await _contactService.GetByIdAsync(contactId);
        var (totalTicketsAtribuied, totalTicketsonHold) = await _contactService.GetTotalAndOnholdTicketsAsyn(contact.PrimaryEmail);

        if (contact is null)
        {
            return View(new ContactDetailsViewModel());
        }

        var viewModel = new ContactDetailsViewModel
        {
            Id = contact.Id,
            Name = contact.GetFullName(),
            Email = contact.PrimaryEmail??"",
            Phone = contact.Phone??"",
            Mobile = contact.Mobile??"",
            Department=contact.Department??"",
            TotalTicketsonHold=totalTicketsonHold,
            TotalTicketsAtribuied=totalTicketsAtribuied,
        };

        return View(viewModel);
    }
}