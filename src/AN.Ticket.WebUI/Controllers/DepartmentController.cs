using AN.Ticket.Application.DTOs.Department;
using AN.Ticket.Application.Interfaces;
using AN.Ticket.Domain.Enums;
using AN.Ticket.WebUI.ViewModels.Asset;
using AN.Ticket.WebUI.ViewModels.Department;
using Microsoft.AspNetCore.Mvc;

namespace AN.Ticket.WebUI.Controllers;
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
    public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 10, string searchTerm = "")
    {
        var paginatedDepartments = await _departmentService.GetPaginatedDepartmentsAsync(pageNumber, pageSize, searchTerm);

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
