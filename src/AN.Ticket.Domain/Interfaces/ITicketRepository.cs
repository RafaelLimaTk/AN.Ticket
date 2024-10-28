using AN.Ticket.Domain.Entities;
using AN.Ticket.Domain.Interfaces.Base;
using DomainEntity = AN.Ticket.Domain.Entities;

namespace AN.Ticket.Domain.Interfaces;
public interface ITicketRepository : IRepository<DomainEntity.Ticket>
{
    Task<DomainEntity.Ticket> GetByEmailAndSubjectAsync(string email, string subject);
    Task<IEnumerable<DomainEntity.Ticket>> GetTicketsByUserIdAsync(Guid userId);
    Task<IEnumerable<DomainEntity.Ticket>> GetTicketsNotAssignedAsync();
    Task<DomainEntity.Ticket> GetTicketWithDetailsAsync(Guid ticketId);
    Task<int> GetTicketCodeByIdAsync(Guid ticketId);
    Task<DomainEntity.Ticket> GetByTicketCodeAsync(int ticketCode);
    Task<bool> IsTicketClosedAsync(Guid ticketId);
    Task<List<DomainEntity.Ticket>> GetTicketWithDetailsByUserAsync(Guid userId);
    Task<IEnumerable<DomainEntity.Ticket>> GetByContactEmailAsync(List<string> emails);
    Task<(int Total, int Onhold)> GetTicketAssocieteContactTotalAndOnhold(string contactEmail);

    

}
