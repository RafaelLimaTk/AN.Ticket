using AN.Ticket.Application.DTOs.Asset;
using AN.Ticket.Application.Helpers.Pagination;
using AN.Ticket.Application.Interfaces.Base;
using AN.Ticket.Domain.Entities;

namespace AN.Ticket.Application.Interfaces;

public interface IAssetService : IService<AssetDto, Asset>
{
    Task<PagedResult<AssetDto>> GetPaginatedAssetsAsync(int pageNumber, int pageSize, string searchTerm = "");
    Task<bool> CreateAssetAsync(AssetDto assetDto);
    Task<bool> UpdateAssetAsync(AssetDto assetDto);
    Task<bool> DeleteAssetsAsync(List<Guid> ids);
    Task<bool> DeleteAssetAsync(Guid id);
}
