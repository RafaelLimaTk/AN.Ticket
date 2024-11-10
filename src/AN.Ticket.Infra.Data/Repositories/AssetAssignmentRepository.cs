using AN.Ticket.Domain.Entities;
using AN.Ticket.Domain.Interfaces;
using AN.Ticket.Infra.Data.Context;
using AN.Ticket.Infra.Data.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace AN.Ticket.Infra.Data.Repositories;

public class AssetAssignmentRepository : Repository<AssetAssignment>, IAssetAssignmentRepository
{
    public AssetAssignmentRepository(ApplicationDbContext context)
        : base(context)
    { }

    public async Task<Guid> GetAssignmentUserIdAsync(Guid assetId)
    {
        var assetAssignment = await Entities
            .Where(a => a.AssetId == assetId)
            .Select(a => a.UserId)
            .FirstOrDefaultAsync();

        return assetAssignment ?? Guid.Empty;
    }

    public async Task<AssetAssignment> GetByIdOrNullAsync(Guid assetId)
    {
        return await Entities
            .Where(a => a.AssetId == assetId)
            .FirstOrDefaultAsync();
    }
}
