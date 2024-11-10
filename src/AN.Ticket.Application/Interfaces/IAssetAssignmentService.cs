using AN.Ticket.Application.DTOs.AssetAssignment;
using AN.Ticket.Application.Interfaces.Base;
using AN.Ticket.Domain.Entities;

namespace AN.Ticket.Application.Interfaces;

public interface IAssetAssignmentService
    : IService<AssetAssignmentDto, AssetAssignment>
{
    Task<Guid> GetAssignmentUserIdAsync(Guid assetId);
}