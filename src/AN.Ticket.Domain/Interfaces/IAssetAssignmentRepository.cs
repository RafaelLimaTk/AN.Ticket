using AN.Ticket.Domain.Entities;
using AN.Ticket.Domain.Interfaces.Base;

namespace AN.Ticket.Domain.Interfaces;

public interface IAssetAssignmentRepository
    : IRepository<AssetAssignment>
{
    Task<Guid> GetAssignmentUserIdAsync(Guid assetId);
    Task<AssetAssignment> GetByIdOrNullAsync(Guid assetId);
    Task<IEnumerable<AssetAssignment>> GetByContactIdAsync(Guid contactId);
}
