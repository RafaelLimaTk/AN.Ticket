using AN.Ticket.Application.DTOs.Contact;
using AN.Ticket.Application.Interfaces;
using AN.Ticket.Infra.Data.Identity;
using AN.Ticket.WebUI.ViewModels.Contact;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AN.Ticket.WebUI.Controllers;

[Authorize(Roles = "Admin")]
public class ContactController : Controller
{
    private readonly ILogger<ContactController> _logger;
    private readonly IContactService _contactService;
    private readonly IPaymantPlanService _paymantPlanService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ContactController(
        ILogger<ContactController> logger,
        IContactService contactService,
        IPaymantPlanService paymantPlanService,
        UserManager<ApplicationUser> userManager
    )
    {
        _logger = logger;
        _contactService = contactService;
        _paymantPlanService = paymantPlanService;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> CreateContact()
    {
        var paymentPlans = await _paymantPlanService.GetAllAsync();
        var viewModel = new ContactCreateViewModel
        {
            Contact = new ContactCreateDto(),
            PaymentPlans = paymentPlans.ToList()
        };
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateContact(ContactCreateViewModel model)
    {
        //if (model.Contact.PaymentPlanId == Guid.Empty)
        //{
        //    TempData["ErrorMessage"] = "Selecione um plano de pagamento";
        //    var paymentPlans = await _paymantPlanService.GetAllAsync();
        //    model.PaymentPlans = paymentPlans.ToList();
        //    return View(model);
        //}

        if (!ModelState.IsValid)
        {
            var paymentPlans = await _paymantPlanService.GetAllAsync();
            model.PaymentPlans = paymentPlans.ToList();
            return View(model);
        }

        var user = await GetCurrentUserAsync();
        model.Contact.UserId = Guid.Parse(user!.Id);

        var success = await _contactService.CreateContactAsync(model.Contact);
        if (!success)
        {
            TempData["ErrorMessage"] = "Ocorreu um erro ao criar o contato. Verifique os dados e tente novamente";
            View(model);
        }

        TempData["SuccessMessage"] = "Cliente criado com sucesso!";
        return Redirect(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 10, string searchTerm = "")
    {
        var paginatedContacts = await _contactService.GetPaginatedContactsAsync(pageNumber, pageSize, searchTerm);

        var contactDTOs = paginatedContacts.Items.Select(contact => new ContactDto
        {
            Id = contact.Id,
            FullName = contact.FullName,
            PrimaryEmail = contact.PrimaryEmail,
            SecondaryEmail = contact.SecondaryEmail,
            Phone = contact.Phone,
            Mobile = contact.Mobile,
            Department = contact.Department,
            Title = contact.Title,
            UserId = contact.UserId,
            User = contact.User,
            CreatedAt = contact.CreatedAt
        }).ToList();

        var viewModel = new ContactListViewModel
        {
            Contacts = contactDTOs,
            PageNumber = paginatedContacts.PageNumber,
            PageSize = paginatedContacts.PageSize,
            TotalItems = paginatedContacts.TotalItems,
            SearchTerm = searchTerm
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteContacts(List<Guid> ids)
    {
        if (ids == null || !ids.Any())
        {
            TempData["ErrorMessage"] = "Nenhum contato foi selecionado.";
            return RedirectToAction(nameof(Index));
        }

        var success = await _contactService.DeleteContactsAsync(ids);
        if (!success)
        {
            TempData["ErrorMessage"] = "Erro ao deletar os contatos selecionados.";
            return RedirectToAction(nameof(Index));
        }

        TempData["SuccessMessage"] = "Contatos deletados com sucesso!";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> DeleteContact(Guid id)
    {
        if (id == Guid.Empty)
        {
            TempData["ErrorMessage"] = "Nenhum contato foi selecionado.";
            return RedirectToAction(nameof(Index));
        }

        var ids = new List<Guid> { id };

        var success = await _contactService.DeleteContactsAsync(ids);
        if (!success)
        {
            TempData["ErrorMessage"] = "Erro ao deletar o contato selecionado.";
            return RedirectToAction(nameof(Index));
        }

        TempData["SuccessMessage"] = "Contato deletado com sucesso!";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> EditContact(Guid? id)
    {
        if (id is null)
        {
            return RedirectToAction(nameof(Index));
        }

        var contact = await _contactService.GetByIdAsync(id.Value);
        if (contact is null)
        {
            return RedirectToAction(nameof(Index));
        }

        var paymentPlans = await _paymantPlanService.GetAllAsync();
        var paymentPlanContactId = await _contactService.GetContactPaymentPlanIdAsync(contact.Id);
        var viewModel = new ContactCreateViewModel
        {
            Contact = new ContactCreateDto
            {
                Id = contact.Id,
                FirstName = contact.FirstName,
                LastName = contact.LastName,
                Cpf = contact.Cpf,
                PrimaryEmail = contact.PrimaryEmail,
                SecondaryEmail = contact.SecondaryEmail,
                Phone = contact.Phone,
                Mobile = contact.Mobile,
                PaymentPlanId = paymentPlanContactId ?? Guid.Empty
            },
            PaymentPlans = paymentPlans.ToList(),
            IsEditedContact = true
        };

        return View(nameof(CreateContact), viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditContact(ContactCreateViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            var paymentPlans = await _paymantPlanService.GetAllAsync();
            viewModel.PaymentPlans = paymentPlans.ToList();
            return View("CreateContact", viewModel);
        }

        var success = await _contactService.UpdateContactAsync(viewModel.Contact);
        if (!success)
        {
            TempData["ErrorMessage"] = "Erro ao atualizar o contato.";
            return RedirectToAction(nameof(EditContact), new { id = viewModel.Contact.Id });
        }

        TempData["SuccessMessage"] = "Contato atualizado com sucesso!";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var contactDetails = await _contactService.GetContactDetailsAsync(id);
        return View(contactDetails);
    }

    private async Task<ApplicationUser?> GetCurrentUserAsync()
    => await _userManager.GetUserAsync(User);

    private async Task<Guid> GetCurrentUserId()
    {
        var user = await GetCurrentUserAsync();
        return user != null ? Guid.Parse(user.Id) : Guid.Empty;
    }
}
