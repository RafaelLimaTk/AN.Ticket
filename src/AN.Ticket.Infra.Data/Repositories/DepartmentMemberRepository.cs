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
                Id = dm.Id,
                FullName = dm.User != null ? dm.User.FullName : dm.Contact != null ? dm.Contact.GetFullName() : string.Empty,
                ProfilePictureUrl = dm.User != null ? dm.User.ProfilePicture : string.Empty,
            })
            .ToListAsync();
    }
}
