using AN.Ticket.Domain.Entities;
using AN.Ticket.Domain.Enums;
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
    int pageNumber, int pageSize, string searchTerm = "", int? status = null, string memberOrder = "")
    {
        var query = Entities.Include(d => d.Members).AsQueryable();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(d => d.Name.Contains(searchTerm) || d.Description.Contains(searchTerm));
        }

        if (status.HasValue)
        {
            query = query.Where(d => d.Status == (DepartmentStatus)status.Value);
        }

        query = memberOrder switch
        {
            "asc" => query.OrderBy(d => d.Members.Count),
            "desc" => query.OrderByDescending(d => d.Members.Count),
            _ => query.OrderBy(d => d.Name)
        };

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(d => d.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
