using AN.Ticket.Application.DTOs.AssetAssignment;
using AN.Ticket.Application.Interfaces;
using AN.Ticket.Application.Services.Base;
using AN.Ticket.Domain.Entities;
using AN.Ticket.Domain.Interfaces;
using AN.Ticket.Domain.Interfaces.Base;

namespace AN.Ticket.Application.Services;
public class AssetAssignmentService
    : Service<AssetAssignmentDto, AssetAssignment>, IAssetAssignmentService
{
    private readonly IAssetAssignmentRepository _assetAssignmentRepository;

    public AssetAssignmentService(
        IRepository<AssetAssignment> repository,
        IAssetAssignmentRepository assetAssignmentRepository
    )
        : base(repository)
    {
        _assetAssignmentRepository = assetAssignmentRepository;
    }

    public async Task<Guid> GetAssignmentUserIdAsync(Guid assetId)
        => await _assetAssignmentRepository.GetAssignmentUserIdAsync(assetId);
}
