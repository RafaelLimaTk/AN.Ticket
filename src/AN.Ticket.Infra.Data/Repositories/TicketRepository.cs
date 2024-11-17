using AN.Ticket.Application.DTOs.Ticket;
using AN.Ticket.Domain.DTOs;
using AN.Ticket.Domain.Enums;
using AN.Ticket.Domain.Interfaces;
using AN.Ticket.Infra.Data.Context;
using AN.Ticket.Infra.Data.Repositories.Base;
using Microsoft.EntityFrameworkCore;
using DomainEntity = AN.Ticket.Domain.Entities;

namespace AN.Ticket.Infra.Data.Repositories;

public class TicketRepository
    : Repository<DomainEntity.Ticket>, ITicketRepository
{
    public TicketRepository(
        ApplicationDbContext context
    )
        : base(context)
    {
    }

    public async Task<DomainEntity.Ticket> GetByEmailAndSubjectAsync(string email, string subject)
    {
        return await Entities
            .AsNoTracking()
            .Include(x => x.Messages)
            .FirstOrDefaultAsync(
                x => x.Email == email &&
                x.Subject == subject
            );
    }

    public async Task<IEnumerable<DomainEntity.Ticket>> GetTicketsByUserIdAsync(Guid userId)
    {
        return await Entities
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToListAsync();
    }

    public async Task<IEnumerable<DomainEntity.Ticket>> GetTicketsNotAssignedAsync()
    {
        return await Entities
            .AsNoTracking()
            .Where(x => x.UserId == null)
            .ToListAsync();
    }

    public async Task<DomainEntity.Ticket> GetTicketWithDetailsAsync(Guid ticketId)
    {
        return await Entities
            .AsNoTracking()
            .Include(x => x.Messages).ThenInclude(x => x.User)
            .Include(x => x.Activities)
            .Include(x => x.InteractionHistories).ThenInclude(x => x.User)
            .Include(x => x.SatisfactionRating)
            .Include(x => x.User)
            .Include(x => x.Attachments)
            .FirstOrDefaultAsync(x => x.Id == ticketId);
    }

    public async Task<List<DomainEntity.Ticket>> GetTicketWithDetailsByUserAsync(Guid userId)
    {
        return await Entities
            .AsNoTracking()
            .Include(x => x.Messages).ThenInclude(x => x.User)
            .Include(x => x.Activities)
            .Include(x => x.InteractionHistories).ThenInclude(x => x.User)
            .Include(x => x.SatisfactionRating)
            .Include(x => x.User)
            .Include(x => x.Attachments)
            .Where(x => x.UserId == userId).ToListAsync();
    }

    public async Task<int> GetTicketCodeByIdAsync(Guid ticketId)
    {
        return await Entities
            .AsNoTracking()
            .Where(x => x.Id == ticketId)
            .Select(x => x.TicketCode)
            .SingleAsync();
    }

    public async Task<DomainEntity.Ticket> GetByTicketCodeAsync(int ticketCode)
    {
        return await Entities
            .AsNoTracking()
            .Include(x => x.Messages)
            .SingleAsync(x => x.TicketCode == ticketCode);
    }

    public async Task<bool> IsTicketClosedAsync(Guid ticketId)
    {
        return await Entities
            .AsNoTracking()
            .Where(x => x.Id == ticketId && x.Status == TicketStatus.Closed)
            .AnyAsync();
    }

    public async Task<IEnumerable<DomainEntity.Ticket>> GetByContactEmailAsync(List<string> emails)
    {
        return await Entities
            .AsNoTracking()
            .Where(x => emails.Contains(x.Email))
            .Include(x => x.User)
            .Include(x => x.Messages)
            .ToListAsync();
    }

    public async Task<(int Total, int Onhold)> GetTicketAssocieteContactTotalAndOnhold(string contactEmail)
    {
        var totalTickets = await Entities
           .AsNoTracking()
           .Where(x => x.Email.Equals(contactEmail))
           .CountAsync();
        var onHoldTickets = await Entities
            .AsNoTracking()
            .Where(x => x.Email.Equals(contactEmail) && x.Status == TicketStatus.Onhold)
            .CountAsync();
        return (totalTickets, onHoldTickets);
    }

    public async Task<IEnumerable<ITicketCountUserDto>> GetUserTicketCountsAsync()
    {
        return await Entities
            .AsNoTracking()
            .Where(t =>
                t.Status == TicketStatus.Onhold ||
                t.Status == TicketStatus.Open
            )
            .GroupBy(t => t.UserId)
            .Select(g => new TicketCountUserDto
            {
                UserId = g.Key.Value,
                TicketCount = g.Count()
            })
            .ToListAsync();
    }

    public async Task<(IEnumerable<DomainEntity.Ticket> Items, int TotalCount)> GetPaginatedTicketsAsync(
        int pageNumber,
        int pageSize,
        string searchTerm = "",
        string orderBy = "CreatedAt",
        TicketStatus? statusFilter = null
    )
    {
        var query = Entities.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(t =>
                t.Subject.Contains(searchTerm) ||
                t.ContactName.Contains(searchTerm) ||
                t.Email.Contains(searchTerm)
            );
        }

        if (statusFilter.HasValue)
        {
            query = query.Where(t => t.Status == statusFilter.Value);
        }

        var orderedQuery = query.OrderBy(t => t.UserId.HasValue);

        orderedQuery = orderBy switch
        {
            "Subject" => orderedQuery.ThenBy(t => t.Subject),
            "ContactName" => orderedQuery.ThenBy(t => t.ContactName),
            "Email" => orderedQuery.ThenBy(t => t.Email),
            "Priority" => orderedQuery.ThenBy(t => t.Priority),
            "DueDate" => orderedQuery.ThenBy(t => t.DueDate),
            _ => orderedQuery.ThenBy(t => t.CreatedAt)
        };

        var totalCount = await orderedQuery.CountAsync();

        var items = await orderedQuery
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Include(t => t.User)
            .Include(t => t.Messages)
            .Include(t => t.Attachments)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<(int TicketsConcluidos, int TicketsEmAndamento, double tempoEconomizadoHoras)> GetTicketMetricsAsync()
    {
        var metrics = await Entities
            .AsNoTracking()
            .Where(t => t.Status == TicketStatus.Closed || t.Status == TicketStatus.Open || t.Status == TicketStatus.InProgress)
            .Select(t => new
            {
                t.Status,
                t.ClosedAt,
                TempoEconomizado = t.ClosedAt.HasValue ?
                    EF.Functions.DateDiffHour(t.ClosedAt.Value, t.DueDate) : 0
            })
            .ToListAsync();

        var ticketsConcluidos = metrics.Count(t => t.Status == TicketStatus.Closed);
        var ticketsEmAndamento = metrics.Count(t => t.Status == TicketStatus.Open || t.Status == TicketStatus.InProgress);

        var tempoEconomizadoHoras = metrics
            .Where(t => t.Status == TicketStatus.Closed && t.ClosedAt.HasValue)
            .Sum(t => t.TempoEconomizado);

        return (ticketsConcluidos, ticketsEmAndamento, tempoEconomizadoHoras);
    }
}
