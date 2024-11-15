using AN.Ticket.Application.DTOs.Team;
using AN.Ticket.Application.DTOs.User;
using AN.Ticket.Application.Interfaces;
using AN.Ticket.Domain.EntityValidations;
using AN.Ticket.Domain.Enums;
using AN.Ticket.WebUI.ViewModels.Team;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AN.Ticket.WebUI.Controllers;

[Authorize]
public class TeamController : Controller
{
    private readonly ITeamService _teamService;
    private readonly IUserService _userService;

    public TeamController(
        ITeamService teamService,
        IUserService userService
    )
    {
        _teamService = teamService;
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPagedTeamMembers(Guid teamId, int pageNumber = 1, int pageSize = 10, string searchTerm = "")
    {
        var pagedMembers = await _teamService.GetPagedTeamMembersAsync(teamId, pageNumber, pageSize, searchTerm);

        var viewModel = new TeamMemberListViewModel
        {
            TeamId = teamId,
            Members = pagedMembers.Items,
            PageNumber = pagedMembers.PageNumber,
            PageSize = pagedMembers.PageSize,
            TotalItems = pagedMembers.TotalItems,
            SearchTerm = searchTerm
        };

        return PartialView("~/Views/Shared/Partials/Team/_TeamMembersTable.cshtml", viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> CreateTeamModal()
    {
        var users = await _userService.GetAllUsersAsync();
        var viewModel = new TeamCreationViewModel
        {
            AvailableUsers = users.Where(us => us.Role != UserRole.Admin).ToList()
        };
        return PartialView("~/Views/Shared/Partials/Team/_CreateTeamModal.cshtml", viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTeam(TeamCreationViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        try
        {
            var teamDto = new TeamDto
            {
                Name = model.TeamName,
                Members = model.SelectedUserIds.Select(id => new UserDto { Id = id }).ToList()
            };

            await _teamService.CreateTeamAsync(teamDto);
            TempData["SuccessMessage"] = "Time criado com sucesso!";
            TempData["SuccessRedirect"] = true;
            return Json(new { success = true });
        }
        catch (EntityValidationException ex)
        {
            return Json(new { success = false, errors = new[] { ex.Message } });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, errors = new[] { "Erro interno do servidor." } });
        }
    }

    [HttpGet]
    public async Task<IActionResult> ShowAddMemberModal(Guid teamId)
    {
        var users = await _userService.GetAllUsersAsync();
        var usersNotAdmin = users.Where(us => us.Role != UserRole.Admin);
        ViewBag.AvailableUsers = usersNotAdmin;
        return PartialView("_AddMemberModal", teamId);
    }

    [HttpPost]
    public async Task<IActionResult> AddMember(Guid teamId, List<Guid> selectedUserIds)
    {
        if (teamId == Guid.Empty || selectedUserIds == null || !selectedUserIds.Any())
        {
            return Json(new { success = false, error = "Selecione pelo menos um membro para adicionar." });
        }

        try
        {
            await _teamService.AddMembersToTeamAsync(teamId, selectedUserIds);
            TempData["SuccessMessage"] = "Membros adicionados com sucesso!";
            TempData["SuccessRedirect"] = true;
            return Json(new { success = true });
        }
        catch (EntityValidationException ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = "Erro ao adicionar membro. Tente novamente." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMembers(Guid teamId, List<Guid> memberIds)
    {
        if (memberIds == null || !memberIds.Any())
        {
            TempData["ErrorMessage"] = "Nenhum membro foi selecionado.";
            TempData["SuccessRedirect"] = true;
            return RedirectToAction("Index", "Setting");
        }

        try
        {
            await _teamService.RemoveMembersFromTeamAsync(teamId, memberIds);
            TempData["SuccessMessage"] = "Membros deletados com sucesso!";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Erro ao deletar os membros.";
        }

        TempData["SuccessRedirect"] = true;
        return RedirectToAction("Index", "Setting");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMember(Guid teamId, Guid memberId)
    {
        try
        {
            await _teamService.RemoveMemberFromTeamAsync(teamId, memberId);
            TempData["SuccessMessage"] = "Membro deletado com sucesso!";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Erro ao deletar o membro.";
        }

        TempData["SuccessRedirect"] = true;
        return RedirectToAction("Index", "Setting");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTeam(Guid teamId)
    {
        try
        {
            await _teamService.DeleteTeamAsync(teamId);
            TempData["SuccessMessage"] = "Time deletado com sucesso!";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Erro ao deletar o time.";
        }

        TempData["SuccessRedirect"] = true;
        return RedirectToAction("Index", "Setting");
    }
}
