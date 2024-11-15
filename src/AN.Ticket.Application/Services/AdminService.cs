using AN.Ticket.Application.DTOs.Asset;
using AN.Ticket.Application.DTOs.Ticket;
using AN.Ticket.Application.DTOs.User;
using AN.Ticket.Application.Helpers.Pagination;
using AN.Ticket.Application.Interfaces;
using AN.Ticket.Domain.Enums;
using AN.Ticket.Domain.Interfaces;

namespace AN.Ticket.Application.Services;
public class AdminService : IAdminService
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IAssetRepository _assetRepository;

    public AdminService(ITicketRepository ticketRepository, IAssetRepository assetRepository)
    {
        _ticketRepository = ticketRepository;
        _assetRepository = assetRepository;
    }

    public async Task<PagedResult<AssetDto>> GetPaginatedAssetsAsync(
        int pageNumber,
        int pageSize,
        DateTime? purchaseDate = null,
        string orderBy = "PurchaseDate"
    )
    {
        var (assets, totalItems) = await _assetRepository.GetPaginatedAssetsAsync(pageNumber, pageSize, purchaseDate, orderBy);

        var assetDTOs = assets.Select(a => new AssetDto
        {
            Id = a.Id,
            Name = a.Name,
            SerialNumber = a.SerialNumber,
            AssetType = a.AssetType,
            PurchaseDate = a.PurchaseDate,
            Value = a.Value,
            Description = a.Description,
            CreatedAt = a.CreatedAt,
            UpdatedAt = a.UpdatedAt
        }).ToList();

        return new PagedResult<AssetDto>
        {
            Items = assetDTOs,
            TotalItems = totalItems,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<TicketNotAssignedDto>> GetPaginatedTicketsAsync(
        int pageNumber,
        int pageSize,
        string searchTerm = "",
        string orderBy = "CreatedAt",
        TicketStatus? statusFilter = null
    )
    {
        var (tickets, totalItems) = await _ticketRepository.GetPaginatedTicketsAsync(
            pageNumber,
            pageSize,
            searchTerm,
            orderBy,
            statusFilter
        );

        var ticketDTOs = tickets.Select(t => new TicketNotAssignedDto
        {
            Id = t.Id,
            Subject = t.Subject,
            Priority = t.Priority,
            DueDate = t.DueDate,
            ContactName = t.ContactName,
            Status = t.Status,
            CreatedAt = t.CreatedAt,
            MessagesCount = t.Messages?.Count() ?? 0,
            AttachmentsCount = t.Attachments?.Count() ?? 0,
            User = t.User == null ? null : new UserDto
            {
                Id = t.User.Id,
                FullName = t.User.FullName,
                Email = t.User.Email,
                ProfilePicture = t.User.ProfilePicture
            }
        }).ToList();

        return new PagedResult<TicketNotAssignedDto>
        {
            Items = ticketDTOs,
            TotalItems = totalItems,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<TicketMetricsDto> GetTicketMetricsAsync()
    {
        var tupleTicketMetrics = await _ticketRepository.GetTicketMetricsAsync();

        return new TicketMetricsDto
        {
            TicketsConcluidos = tupleTicketMetrics.TicketsConcluidos,
            TicketsEmAndamento = tupleTicketMetrics.TicketsEmAndamento,
            TempoEconomizadoHoras = tupleTicketMetrics.tempoEconomizadoHoras,
        };
    }
}
