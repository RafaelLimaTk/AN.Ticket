using AN.Ticket.Application.Interfaces;
using AN.Ticket.WebUI.ViewModels.Department;
using Microsoft.AspNetCore.Mvc;

namespace AN.Ticket.WebUI.Controllers;
public class DepartmentController : Controller
{
    private readonly IDepartmentService _departmentService;

    public DepartmentController(
        IDepartmentService departmentService
    )
    {
        _departmentService = departmentService;
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
}
