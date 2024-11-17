using AN.Ticket.Application.DTOs.Activity;
using AN.Ticket.Application.Interfaces;
using AN.Ticket.Domain.EntityValidations;
using AN.Ticket.Domain.Enums;
using AN.Ticket.WebUI.Components;
using AN.Ticket.WebUI.ViewModels.Activity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AN.Ticket.WebUI.Controllers;

[Authorize]
public class ActivityController : Controller
{
    private readonly IActivityService _activityService;
    private readonly IContactService _contactService;
    private readonly ITicketService _ticketService;

    public ActivityController(
        IActivityService activityService,
        IContactService contactService,
        ITicketService ticketService
    )
    {
        _activityService = activityService;
        _contactService = contactService;
        _ticketService = ticketService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 10, string searchTerm = "")
    {
        var paginatedActivities = await _activityService.GetPaginatedActivitiesAsync(pageNumber, pageSize, searchTerm);

        return View(new ActivityListViewModel
        {
            Activities = paginatedActivities.Items,
            PageNumber = paginatedActivities.PageNumber,
            PageSize = paginatedActivities.PageSize,
            TotalItems = paginatedActivities.TotalItems,
            SearchTerm = searchTerm
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create(Guid? ticketId, bool? isScreenTicket)
    {
        var model = new ActivityDto
        {
            ScheduledDate = DateTime.Now,
            TicketId = ticketId ?? Guid.Empty,
            IsScreenTicket = isScreenTicket ?? false
        };

        var tickets = await _ticketService.GetAllAsync();
        ViewBag.Tickets = tickets.Where(x => x.Status != TicketStatus.Closed);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ActivityDto model)
    {
        if (!ModelState.IsValid)
        {
            var tickets = await _ticketService.GetAllAsync();
            ViewBag.Tickets = tickets.Where(x => x.Status != TicketStatus.Closed);
            return View(model);
        }

        try
        {
            await _activityService.CreateActivityAsync(model);

            TempData["SuccessMessage"] = "Atividade criada com sucesso";
            if (model.IsScreenTicket)
                return RedirectToAction("Details", "Ticket", new { id = model.TicketId });

            return RedirectToAction(nameof(Index));
        }
        catch (EntityValidationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Create), new { model.TicketId });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, bool isTicketEdit = false)
    {
        var activity = await _activityService.GetByIdAsync(id);
        if (activity is null)
        {
            TempData["ErrorMessage"] = "Erro ao editar a atividade";
            return RedirectToAction(nameof(Index));
        }

        var activityDto = new ActivityDto
        {
            Id = activity.Id,
            Description = activity.Description,
            Duration = activity.Duration ?? TimeSpan.Zero,
            Priority = activity.Priority,
            ScheduledDate = activity.ScheduledDate,
            Subject = activity.Subject!,
            TicketId = activity.TicketId ?? Guid.Empty,
            Type = activity.Type,
            ContactId = activity.ContactId,
            IsEditTicket = isTicketEdit
        };

        var tickets = await _ticketService.GetAllAsync();
        ViewBag.Tickets = tickets.Where(x => x.Status != TicketStatus.Closed);
        ViewBag.isTicketEdit = isTicketEdit;

        return View(nameof(Create), activityDto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ActivityDto model)
    {
        if (!ModelState.IsValid)
        {
            var tickets = await _ticketService.GetAllAsync();
            ViewBag.Tickets = tickets.Where(x => x.Status != TicketStatus.Closed);
            return View(nameof(Create), model);
        }

        try
        {
            await _activityService.UpdateActivityAsync(model);

            if (model.IsEditTicket)
            {
                TempData["SuccessMessage"] = "Atividade atualizada com sucesso";
                return RedirectToAction("Details", "Ticket", new { id = model.TicketId });
            }

            TempData["SuccessMessage"] = "Atividade atualizada com sucesso";
            return RedirectToAction(nameof(Index));
        }
        catch (EntityValidationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Create), new { id = model.Id });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Delete(Guid id, bool isTicketEdit = false)
    {
        try
        {
            await _activityService.DeleteActivityAsync(id);
            TempData["SuccessMessage"] = "Atividade excluída com sucesso.";
        }
        catch (EntityValidationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        if (isTicketEdit)
        {
            return RedirectToAction("Details", "Ticket", new { id });
        }

        return RedirectToAction(nameof(Index));
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteActivities(List<Guid> ids)
    {
        if (ids == null || !ids.Any())
        {
            TempData["ErrorMessage"] = "Nenhuma atividade foi selecionada.";
            return RedirectToAction(nameof(Index));
        }

        var success = await _activityService.DeleteActivitiesAsync(ids);
        if (!success)
        {
            TempData["ErrorMessage"] = "Erro ao deletar as atividades selecionadas.";
            return RedirectToAction(nameof(Index));
        }

        TempData["SuccessMessage"] = "Atividades deletadas com sucesso!";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> GetTicketDetails(Guid ticketId)
    {
        var ticketDetailsViewComponent = new TicketDetailsViewComponent(_ticketService);
        var result = await ticketDetailsViewComponent.InvokeAsync(ticketId);
        return ViewComponent("TicketDetails", new { ticketId });
    }
}
