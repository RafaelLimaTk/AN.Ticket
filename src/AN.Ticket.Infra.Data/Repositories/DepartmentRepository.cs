using AN.Ticket.Domain.Entities;
using AN.Ticket.Domain.Interfaces;
using AN.Ticket.Infra.Data.Context;
using AN.Ticket.Infra.Data.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace AN.Ticket.Infra.Data.Repositories;
public class DepartmentRepository : Repository<Department>, IDepartmentRepository
{
    public DepartmentRepository(ApplicationDbContext context)
        : base(context)
    { }

    public async Task<(IEnumerable<Department> Items, int TotalCount)> GetPaginatedDepartmentsAsync(
    int pageNumber, int pageSize, string searchTerm = "")
    {
        var query = Entities.Include(d => d.Members).AsQueryable();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(d => d.Name.Contains(searchTerm) || d.Description.Contains(searchTerm));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(d => d.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
