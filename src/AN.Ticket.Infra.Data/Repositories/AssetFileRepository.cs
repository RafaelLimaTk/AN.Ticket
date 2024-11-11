using AN.Ticket.Domain.Entities;
using AN.Ticket.Domain.Interfaces;
using AN.Ticket.Infra.Data.Context;
using AN.Ticket.Infra.Data.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace AN.Ticket.Infra.Data.Repositories;
public class AssetFileRepository
    : Repository<AssetFile>, IAssetFileRepository
{
    public AssetFileRepository(ApplicationDbContext context)
        : base(context)
    { }

    public async Task<List<AssetFile>> GetAssetFilesAsync(Guid assetId)
    {
        return await Entities
            .Where(af => af.AssetId == assetId)
            .ToListAsync();
    }
}
