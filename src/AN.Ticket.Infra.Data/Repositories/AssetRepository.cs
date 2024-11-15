using AN.Ticket.Domain.Entities;
using AN.Ticket.Domain.Interfaces;
using AN.Ticket.Infra.Data.Context;
using AN.Ticket.Infra.Data.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace AN.Ticket.Infra.Data.Repositories;
public class AssetRepository : Repository<Asset>, IAssetRepository
{
    public AssetRepository(ApplicationDbContext context)
        : base(context)
    { }

    public async Task<(IEnumerable<Asset> Items, int TotalCount)> GetPaginatedAssetsAsync(
        int pageNumber,
        int pageSize,
        string searchTerm = ""
    )
    {
        var query = Entities.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(a =>
                a.Name.Contains(searchTerm) ||
                a.SerialNumber.Contains(searchTerm) ||
                a.AssetType.Contains(searchTerm)
            );
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(a => a.PurchaseDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<(IEnumerable<Asset> Items, int TotalCount)> GetPaginatedAssetsAsync(
        int pageNumber,
        int pageSize,
        DateTime? purchaseDate = null,
        string orderBy = "PurchaseDate"
    )
    {
        var query = Entities.AsQueryable();

        if (purchaseDate.HasValue)
        {
            query = query.Where(a => a.PurchaseDate.Date == purchaseDate.Value.Date);
        }

        query = orderBy switch
        {
            "Name" => query.OrderBy(a => a.Name),
            "PurchaseDate" => query.OrderByDescending(a => a.PurchaseDate),
            _ => query.OrderByDescending(a => a.PurchaseDate)
        };

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
