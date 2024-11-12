using AN.Ticket.Domain.DTOs;
using AN.Ticket.Domain.Entities;
using AN.Ticket.Domain.Interfaces.Base;

namespace AN.Ticket.Domain.Interfaces;
public interface IDepartmentMemberRepository : IRepository<DepartmentMember>
{
    Task<IEnumerable<IDepartmentMemberDto>> GetMembersByDepartmentIdAsync(Guid departmentId);
}
