using AN.Ticket.Application.DTOs.Department;

namespace AN.Ticket.WebUI.ViewModels.Department;

public class DepartmentListViewModel
{
    public IEnumerable<DepartmentDto> Departments { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public string SearchTerm { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
}