using AN.Ticket.Application.Interfaces;
using AN.Ticket.Domain.EntityValidations;
using AN.Ticket.Domain.Enums;
using AN.Ticket.Infra.Data.Identity;
using AN.Ticket.WebUI.ViewModels.Admin;
using AN.Ticket.WebUI.ViewModels.Asset;
using AN.Ticket.WebUI.ViewModels.Ticket;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AN.Ticket.WebUI.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly IAdminService _adminService;
    private readonly IUserService _userService;
    private readonly ITicketService _ticketService;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(
        IAdminService adminService,
        IUserService userService,
        ITicketService ticketService,
        UserManager<ApplicationUser> userManager
    )
    {
        _adminService = adminService;
        _userService = userService;
        _ticketService = ticketService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(
        int ticketPageNumber = 1,
        int ticketPageSize = 10,
        string ticketSearchTerm = "",
        string ticketOrderBy = "CreatedAt",
        TicketStatus? ticketStatusFilter = null,
        int assetPageNumber = 1,
        int assetPageSize = 10,
        DateTime? assetPurchaseDate = null,
        string assetOrderBy = "PurchaseDate"
    )
    {
        var assets = await _adminService.GetPaginatedAssetsAsync(
            assetPageNumber,
            assetPageSize,
            assetPurchaseDate,
            assetOrderBy
        );

        var tickets = await _adminService.GetPaginatedTicketsAsync(
            ticketPageNumber,
            ticketPageSize,
            ticketSearchTerm,
            ticketOrderBy,
            ticketStatusFilter
        );

        var ticketMetrics = await _adminService.GetTicketMetricsAsync();

        var users = await _userService.GetAllUsersAsync();
        ViewBag.UserAssigned = users;

        var currentUser = await _userManager.GetUserAsync(User);
        string firstName = "Convidado";

        if (currentUser != null && !string.IsNullOrEmpty(currentUser.FullName))
        {
            var names = currentUser.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (names.Length > 0)
            {
                firstName = names[0];
                ViewBag.FirstName = firstName;
            }
        }

        var viewModel = new AdminViewModel
        {
            TicketMetrics = ticketMetrics,
            Assets = new AssetListViewModel
            {
                Assets = assets.Items,
                PageNumber = assets.PageNumber,
                PageSize = assets.PageSize,
                TotalItems = assets.TotalItems,
                OrderBy = assetOrderBy
            },
            Tickets = new TicketListViewModel
            {
                Tickets = tickets.Items,
                PageNumber = tickets.PageNumber,
                PageSize = tickets.PageSize,
                TotalItems = tickets.TotalItems,
                SearchTerm = ticketSearchTerm,
                OrderBy = ticketOrderBy
            }
        };

        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> AssignTicket(Guid ticketId, Guid userId)
    {
        if (ticketId == Guid.Empty)
        {
            TempData["ErrorMessage"] = "Ticket ID inválido.";
            return RedirectToAction(nameof(Index));
        }

        if (userId == Guid.Empty)
        {
            TempData["ErrorMessage"] = "Usuário ID inválido.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var success = await _ticketService.AssignTicketToUserAsync(ticketId, userId, true);

            if (!success)
            {
                TempData["ErrorMessage"] = "Não foi possível atribuir o ticket.";
                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] = "Ticket atribuído com sucesso!";
            return RedirectToAction(nameof(Index));
        }
        catch (EntityValidationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }
}
