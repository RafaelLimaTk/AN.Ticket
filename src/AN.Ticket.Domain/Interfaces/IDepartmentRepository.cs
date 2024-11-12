using AN.Ticket.Domain.Entities;
using AN.Ticket.Domain.Interfaces.Base;

namespace AN.Ticket.Domain.Interfaces;
public interface IDepartmentRepository : IRepository<Department>
{
    Task<(IEnumerable<Department> Items, int TotalCount)> GetPaginatedDepartmentsAsync(int pageNumber, int pageSize, string searchTerm = "");
}
