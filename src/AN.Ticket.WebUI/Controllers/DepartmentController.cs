using AN.Ticket.Application.DTOs.Department;
using AN.Ticket.Application.Interfaces;
using AN.Ticket.Domain.Enums;
using AN.Ticket.WebUI.ViewModels.Asset;
using AN.Ticket.WebUI.ViewModels.Department;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AN.Ticket.WebUI.Controllers;

[Authorize(Roles = "Admin")]
public class DepartmentController : Controller
{
    private readonly IDepartmentService _departmentService;
    private readonly IUserService _userService;
    private readonly IContactService _contactService;

    public DepartmentController(
        IDepartmentService departmentService,
        IUserService userService,
        IContactService contactService
    )
    {
        _departmentService = departmentService;
        _userService = userService;
        _contactService = contactService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 10, string searchTerm = "", int? status = null, string memberOrder = "")
    {
        var paginatedDepartments = await _departmentService.GetPaginatedDepartmentsAsync(pageNumber, pageSize, searchTerm, status, memberOrder);

        ViewBag.StatusOptions = new SelectList(new List<SelectListItem>
        {
            new SelectListItem { Text = "Todos", Value = "", Selected = status == null },
            new SelectListItem { Text = "Ativo", Value = "1", Selected = status == 1 },
            new SelectListItem { Text = "Inativo", Value = "2", Selected = status == 2 }
        }, "Value", "Text", status?.ToString());

        ViewBag.MemberOrderOptions = new SelectList(new List<SelectListItem>
        {
            new SelectListItem { Text = "Nenhum", Value = "", Selected = string.IsNullOrEmpty(memberOrder) },
            new SelectListItem { Text = "Menor para Maior", Value = "asc", Selected = memberOrder == "asc" },
            new SelectListItem { Text = "Maior para Menor", Value = "desc", Selected = memberOrder == "desc" }
        }, "Value", "Text", memberOrder);


        return View(new DepartmentListViewModel
        {
            Departments = paginatedDepartments.Items,
            PageNumber = paginatedDepartments.PageNumber,
            PageSize = paginatedDepartments.PageSize,
            TotalItems = paginatedDepartments.TotalItems,
            SearchTerm = searchTerm
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new DepartmentDto
        {
            Status = DepartmentStatus.Active
        };

        ViewBag.UserContacts = await GetUserContactsAsync();

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(DepartmentDto model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.UserContacts = await GetUserContactsAsync();
            return View(model);
        }

        try
        {
            await _departmentService.CreateDepartmentAsync(model);
            TempData["SuccessMessage"] = "Departamento criado com sucesso!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Erro ao criar o departamento: {ex.Message}";
            ViewBag.UserContacts = await GetUserContactsAsync();
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        if (id == Guid.Empty)
        {
            TempData["ErrorMessage"] = "ID do departamento inválido.";
            return RedirectToAction(nameof(Index));
        }

        var department = await _departmentService.GetByIdAsync(id);

        if (department is null)
        {
            TempData["ErrorMessage"] = "Departamento não encontrado.";
            return RedirectToAction(nameof(Index));
        }

        var departmentMembers = await _departmentService.GetMembersByDepartmentIdAsync(id);
        var departmentDto = new DepartmentDto
        {
            Id = department.Id,
            Name = department.Name,
            Code = department.Code,
            Description = department.Description,
            Status = department.Status,
            Members = departmentMembers.Select(member => new DepartmentMemberDto
            {
                Id = member.Id,
                FullName = member.FullName,
                Type = member.Type
            }).ToList()
        };

        ViewBag.UserContacts = await GetUserContactsAsync();

        return View(nameof(Create), departmentDto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(DepartmentDto model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.UserContacts = await GetUserContactsAsync();
            return View(nameof(Create), model);
        }

        try
        {
            await _departmentService.UpdateDepartmentAsync(model);
            TempData["SuccessMessage"] = "Departamento atualizado com sucesso!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Erro ao atualizar o departamento: {ex.Message}";
            ViewBag.UserContacts = await GetUserContactsAsync();
            return View(nameof(Create), model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (id == Guid.Empty)
        {
            TempData["ErrorMessage"] = "ID do departamento inválido.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _departmentService.DeleteDepartmentAsync(id);
            TempData["SuccessMessage"] = "Departamento excluído com sucesso!";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Erro ao excluir o departamento: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<List<UserContactDto>> GetUserContactsAsync()
    {
        var users = await _userService.GetAllAsync();
        var contacts = await _contactService.GetAllAsync();

        var userContactList = new List<UserContactDto>();

        userContactList.AddRange(users.Select(user => new UserContactDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Type = UserContactType.User
        }));

        userContactList.AddRange(contacts.Select(contact => new UserContactDto
        {
            Id = contact.Id,
            FullName = contact.GetFullName(),
            Type = UserContactType.Contact
        }));

        return userContactList;
    }
}
