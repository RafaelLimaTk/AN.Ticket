﻿using AN.Ticket.Domain.Interfaces.Base;
using DomainEntity = AN.Ticket.Domain.Entities;

namespace AN.Ticket.Domain.Interfaces;
public interface ITicketRepository
    : IRepository<DomainEntity.Ticket>
{
    Task<DomainEntity.Ticket> GetByEmailAndSubjectAsync(string email, string subject);
    Task<IEnumerable<DomainEntity.Ticket>> GetTicketsByUserIdAsync(Guid userId);
    Task<IEnumerable<DomainEntity.Ticket>> GetTicketsNotAssignedAsync();
}