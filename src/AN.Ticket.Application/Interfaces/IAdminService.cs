using AN.Ticket.Application.DTOs.Asset;
using AN.Ticket.Application.DTOs.Ticket;
using AN.Ticket.Application.Helpers.Pagination;
using AN.Ticket.Domain.Enums;

namespace AN.Ticket.Application.Interfaces;

public interface IAdminService
{
    Task<PagedResult<AssetDto>> GetPaginatedAssetsAsync(int pageNumber, int pageSize, DateTime? purchaseDate = null, string orderBy = "PurchaseDate");
    Task<PagedResult<TicketNotAssignedDto>> GetPaginatedTicketsAsync(int pageNumber, int pageSize, string searchTerm = "", string orderBy = "CreatedAt", TicketStatus? statusFilter = null);
    Task<TicketMetricsDto> GetTicketMetricsAsync();
}
