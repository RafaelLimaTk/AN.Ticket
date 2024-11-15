using AN.Ticket.Domain.Entities;
using AN.Ticket.Domain.Interfaces.Base;

namespace AN.Ticket.Domain.Interfaces;
public interface IAssetRepository : IRepository<Asset>
{
    Task<(IEnumerable<Asset> Items, int TotalCount)> GetPaginatedAssetsAsync(int pageNumber, int pageSize, string searchTerm = "");
    Task<(IEnumerable<Asset> Items, int TotalCount)> GetPaginatedAssetsAsync(int pageNumber, int pageSize, DateTime? purchaseDate = null, string orderBy = "PurchaseDate");
}
