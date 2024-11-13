using AN.Ticket.Application.DTOs.Department;
using AN.Ticket.Domain.DTOs;
using AN.Ticket.Domain.Entities;
using AN.Ticket.Domain.Interfaces;
using AN.Ticket.Infra.Data.Context;
using AN.Ticket.Infra.Data.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace AN.Ticket.Infra.Data.Repositories;
public class DepartmentMemberRepository : Repository<DepartmentMember>, IDepartmentMemberRepository
{
    public DepartmentMemberRepository(ApplicationDbContext context)
        : base(context)
    { }

    public async Task<IEnumerable<IDepartmentMemberDto>> GetMembersByDepartmentIdAsync(Guid departmentId)
    {
        return await Entities
            .Where(dm => dm.DepartmentId == departmentId)
            .Select(dm => new DepartmentMemberDto
            {
                Id = dm.UserId ?? dm.ContactId ?? Guid.Empty,
                FullName = dm.User != null ? dm.User.FullName : dm.Contact != null ? dm.Contact.GetFullName() : string.Empty,
                ProfilePictureUrl = dm.User != null ? dm.User.ProfilePicture : string.Empty,
            })
            .ToListAsync();
    }

    public async Task<List<DepartmentMember>> GetByDepartmentIdAsync(Guid departmentId)
    {
        return await Entities
            .Where(dm => dm.DepartmentId == departmentId)
            .ToListAsync();
    }
}
