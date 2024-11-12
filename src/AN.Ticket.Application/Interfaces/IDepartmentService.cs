using AN.Ticket.Application.DTOs.Department;
using AN.Ticket.Application.Helpers.Pagination;
using AN.Ticket.Application.Interfaces.Base;
using AN.Ticket.Domain.Entities;

namespace AN.Ticket.Application.Interfaces;
public interface IDepartmentService : IService<DepartmentDto, Department>
{
    Task<bool> CreateDepartmentAsync(DepartmentDto dto);
    Task<PagedResult<DepartmentDto>> GetPaginatedDepartmentsAsync(int pageNumber, int pageSize, string searchTerm = "");
}
