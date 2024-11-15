using AN.Ticket.Application.DTOs.Ticket;
using AN.Ticket.Application.Interfaces;
using AN.Ticket.Domain.EntityValidations;
using AN.Ticket.Infra.Data.Identity;
using AN.Ticket.WebUI.Components;
using AN.Ticket.WebUI.ViewModels.Ticket;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AN.Ticket.WebUI.Controllers;

[Authorize(Roles = "Support")]
public class TicketController : Controller
{
    private readonly ITicketService _ticketService;
    private readonly IContactService _contactService;
    private readonly UserManager<ApplicationUser> _userManager;

    public TicketController(
        ITicketService ticketService,
        IContactService contactService,
        UserManager<ApplicationUser> userManager
    )
    {
        _ticketService = ticketService;
        _contactService = contactService;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        TicketFilterDto filters,
        int ticketPageNumber = 1,
        int ticketPageSize = 5,
        int activityPageNumber = 1,
        int activityPageSize = 5
    )
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var userId = await GetCurrentUserId();

        var dashboardData = await _ticketService.GetSupportDashboardAsync(
            userId,
            filters,
            ticketPageNumber,
            ticketPageSize,
            activityPageNumber,
            activityPageSize
        );

        var viewModel = new SupportDashboardViewModel
        {
            DashboardData = dashboardData,
            Filters = filters,
            TicketPageNumber = ticketPageNumber,
            TicketPageSize = ticketPageSize,
            ActivityPageNumber = activityPageNumber,
            ActivityPageSize = activityPageSize
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> UserTickets()
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
            return Unauthorized();

        var tickets = await _ticketService.GetTicketsByUserIdAsync(Guid.Parse(user.Id));
        return View(tickets.OrderByDescending(x => x.Priority));
    }

    [HttpGet]
    public async Task<IActionResult> UnassignedTickets()
    {
        var tickets = await _ticketService.GetTicketsNotAssignedAsync();
        return View(tickets.OrderByDescending(x => x.Priority));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TakeTicket(Guid ticketId)
    {
        if (ticketId == Guid.Empty)
        {
            TempData["ErrorMessage"] = "Ticket ID inválido.";
            return RedirectToAction(nameof(UserTickets));
        }

        try
        {
            var user = await GetCurrentUserAsync();
            if (user is null)
                return Unauthorized();

            var success = await _ticketService.AssignTicketToUserAsync(ticketId, Guid.Parse(user.Id), false);

            if (!success)
            {
                TempData["ErrorMessage"] = "Não foi possível atribuir o ticket.";
                return RedirectToAction(nameof(UserTickets));
            }

            TempData["SuccessMessage"] = "Ticket atribuído com sucesso!";
            return RedirectToAction(nameof(UserTickets));
        }
        catch (EntityValidationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(UserTickets));
        }
    }

    [HttpGet]
    public IActionResult CreateTicket() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTicket(CreateTicketDto model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            model.UserId = await GetCurrentUserId();
            var success = await _ticketService.CreateTicketAsync(model);

            if (!success)
            {
                TempData["ErrorMessage"] = "Não foi possível criar o ticket.";
                return View(model);
            }

            TempData["SuccessMessage"] = "Ticket criado com sucesso!";
            return RedirectToAction(nameof(UserTickets));
        }
        catch (EntityValidationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return View(model);
        }
    }

    [HttpGet]
    [Route("Ticket/Details/{id:guid}")]
    public async Task<IActionResult> Details(Guid id)
    {
        if (id == Guid.Empty)
        {
            TempData["ErrorMessage"] = "ID do ticket inválido.";
            return RedirectToAction(nameof(UserTickets));
        }

        var ticket = await _ticketService.GetTicketDetailsAsync(id);
        if (ticket is null)
        {
            TempData["ErrorMessage"] = "Ticket não encontrado.";
            return RedirectToAction(nameof(UserTickets));
        }

        return View(ticket);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResolveTicket(TicketResolutionDto resolution)
    {
        if (!ModelState.IsValid)
        {
            var ticketDetails = await _ticketService.GetTicketDetailsAsync(resolution.TicketId);
            if (ticketDetails == null)
            {
                TempData["ErrorMessage"] = "Ticket não encontrado.";
                return RedirectToAction(nameof(UserTickets));
            }

            TempData["ErrorMessage"] = "Ops! Ocorreu um erro ao tentar salvar a resolução do Ticket. Verifique a aba resolução.";
            ticketDetails.Resolution = resolution;
            return View("Details", ticketDetails);
        }

        try
        {
            var success = await _ticketService.ResolveTicketAsync(resolution);

            if (!success)
            {
                TempData["ErrorMessage"] = "Não foi possível resolver o ticket. Por favor, tente novamente mais tarde.";
                return RedirectToAction(nameof(Details), new { id = resolution.TicketId });
            }

            TempData["SuccessMessage"] = "Ticket resolvido com sucesso!";
            return RedirectToAction(nameof(UserTickets));
        }
        catch (EntityValidationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id = resolution.TicketId });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReplyToTicket(Guid ticketId, string responseText, List<IFormFile> attachments)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(responseText))
        {
            TempData["ErrorMessage"] = "A mensagem de resposta não pode ser vazia.";
            return RedirectToAction(nameof(Details), new { id = ticketId });
        }

        try
        {
            var success = await _ticketService.ReplyToTicketAsync(ticketId, responseText, Guid.Parse(user.Id), attachments);

            if (!success)
            {
                TempData["ErrorMessage"] = "Não foi possível enviar a resposta. Por favor, tente novamente.";
                return RedirectToAction(nameof(Details), new { id = ticketId });
            }

            TempData["SuccessMessage"] = "Resposta enviada com sucesso!";
            return RedirectToAction(nameof(Details), new { id = ticketId });
        }
        catch (EntityValidationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id = ticketId });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid ticketId)
    {
        if (ticketId == Guid.Empty)
        {
            TempData["ErrorMessage"] = "ID do ticket inválido.";
            return RedirectToAction(nameof(UserTickets));
        }

        try
        {
            var success = await _ticketService.DeleteTicketAsync(ticketId);
            if (!success)
            {
                TempData["ErrorMessage"] = "Não foi possível excluir o ticket.";
                return RedirectToAction(nameof(UserTickets));
            }

            TempData["SuccessMessage"] = "Ticket excluído com sucesso.";
            return RedirectToAction(nameof(UserTickets));
        }
        catch (EntityValidationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(UserTickets));
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        if (id == Guid.Empty)
        {
            TempData["ErrorMessage"] = "ID do ticket inválido.";
            return RedirectToAction(nameof(UserTickets));
        }

        var ticketDetails = await _ticketService.GetTicketDetailsAsync(id);
        if (ticketDetails == null)
        {
            TempData["ErrorMessage"] = "Ticket não encontrado.";
            return RedirectToAction(nameof(UserTickets));
        }

        var editTicketDto = new EditTicketDto
        {
            Id = ticketDetails.Ticket.Id,
            Subject = ticketDetails.Ticket.Subject,
            Status = ticketDetails.Ticket.Status,
            Priority = ticketDetails.Ticket.Priority,
            DueDate = ticketDetails.Ticket.DueDate,
            ContactName = ticketDetails.Ticket.ContactName,
            AccountName = ticketDetails.Ticket.AccountName,
            Email = ticketDetails.Ticket.Email,
            Phone = ticketDetails.Ticket.Phone,

            TicketCode = ticketDetails.Ticket.TicketCode,
            TotalMessages = ticketDetails.Ticket.Messages?.Count() ?? 0,
            TotalActivities = ticketDetails.Ticket.Activities?.Count() ?? 0,
            TotalAttachments = ticketDetails.Ticket.Attachments?.Count() ?? 0,
            SatisfactionStars = (int?)ticketDetails.Ticket.SatisfactionRating?.Rating ?? 0
        };

        return View(editTicketDto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditTicket(EditTicketDto model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Erro ao editar o ticket.";
            return View(model);
        }

        try
        {
            var success = await _ticketService.UpdateTicketAsync(model);
            if (!success)
            {
                TempData["ErrorMessage"] = "Não foi possível editar o ticket.";
                return RedirectToAction(nameof(Edit), new { id = model.Id });
            }

            TempData["SuccessMessage"] = "Ticket editado com sucesso!";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }
        catch (EntityValidationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Edit), new { id = model.Id });
        }
    }


    [HttpGet]
    public async Task<IActionResult> GetContactDetails(Guid contactId)
    {
        var contactDetailsViewComponent = new ContactDetailsViewComponent(_contactService);
        var result = await contactDetailsViewComponent.InvokeAsync(contactId);
        return ViewComponent("ContactDetails", new { contactId });
    }

    [HttpGet]
    public async Task<IActionResult> ShowAllTickets(List<string> emails)
    {
        var ticketDetailsViewComponent = new TicketsDetailsContactViewComponent(_ticketService);
        var result = await ticketDetailsViewComponent.InvokeAsync(emails, true);
        return ViewComponent("TicketsDetailsContact", new { emails = emails, showAll = true });
    }

    private async Task<ApplicationUser?> GetCurrentUserAsync()
        => await _userManager.GetUserAsync(User);

    private async Task<Guid> GetCurrentUserId()
    {
        var user = await GetCurrentUserAsync();
        return user != null ? Guid.Parse(user.Id) : Guid.Empty;
    }
}
