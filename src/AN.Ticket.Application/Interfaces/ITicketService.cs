using AN.Ticket.Application.DTOs.Ticket;
using AN.Ticket.Application.Interfaces.Base;
using AN.Ticket.Application.Services;
using Microsoft.AspNetCore.Http;
using DomainEntity = AN.Ticket.Domain.Entities;

namespace AN.Ticket.Application.Interfaces;
public interface ITicketService
    : IService<TicketDto, DomainEntity.Ticket>
{
    Task<bool> AssignTicketToUserAsync(Guid ticketId, Guid userId);
    Task<bool> CreateTicketAsync(CreateTicketDto createTicket);
    Task<bool> ResolveTicketAsync(TicketResolutionDto ticketResolutionDto);
    Task<TicketDetailsDto> GetTicketDetailsAsync(Guid ticketId);
    Task<IEnumerable<TicketDto>> GetTicketsByUserIdAsync(Guid userId);
    Task<IEnumerable<TicketDto>> GetTicketsNotAssignedAsync();
    Task<bool> ReplyToTicketAsync(Guid ticketId, string messageText, Guid userId, List<IFormFile> attachments);
    Task<bool> DeleteTicketAsync(Guid ticketId);
    Task<bool> UpdateTicketAsync(EditTicketDto editTicketDto);
    Task<List<TicketDto>> GetTicketWithDetailsByUserAsync(Guid userId);
    Task<SupportDashboardDto> GetSupportDashboardAsync(Guid userId, TicketFilterDto filters, int pageNumber, int pageSize, int activityPageNumber, int activityPageSize);
    Task<IEnumerable<TicketContactDetailsDto>> GetTicketsByContactIdAsync(List<string> emails, bool showAll);
    
}
