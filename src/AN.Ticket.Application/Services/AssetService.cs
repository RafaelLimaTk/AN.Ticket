using AN.Ticket.Application.DTOs.Asset;
using AN.Ticket.Application.Helpers.Pagination;
using AN.Ticket.Application.Interfaces;
using AN.Ticket.Application.Services.Base;
using AN.Ticket.Domain.Entities;
using AN.Ticket.Domain.EntityValidations;
using AN.Ticket.Domain.Enums;
using AN.Ticket.Domain.Interfaces;
using AN.Ticket.Domain.Interfaces.Base;

namespace AN.Ticket.Application.Services;
public class AssetService : Service<AssetDto, Asset>, IAssetService
{
    private readonly IAssetRepository _assetRepository;
    private readonly IAssetAssignmentRepository _assetAssignmentRepository;
    private readonly IAssetFileRepository _assetFileRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AssetService(
        IRepository<Asset> repository,
        IAssetRepository assetRepository,
        IAssetAssignmentRepository assetAssignmentRepository,
        IAssetFileRepository assetFileRepository,
        IUnitOfWork unitOfWork
    )
        : base(repository)
    {
        _assetRepository = assetRepository;
        _assetAssignmentRepository = assetAssignmentRepository;
        _assetFileRepository = assetFileRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<AssetDto>> GetPaginatedAssetsAsync(int pageNumber, int pageSize, string searchTerm = "")
    {
        var (assets, totalItems) = await _assetRepository.GetPaginatedAssetsAsync(pageNumber, pageSize, searchTerm);

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

    public async Task<bool> CreateAssetAsync(AssetDto assetDto)
    {
        var asset = new Asset(
            assetDto.Name,
            assetDto.SerialNumber,
            assetDto.AssetType,
            assetDto.PurchaseDate,
            assetDto.Value,
            assetDto.Description
        );

        AssetAssignment? assetAssignment = null;

        if (assetDto.UserId.HasValue && assetDto.UserId != Guid.Empty)
        {
            assetAssignment = assetDto.Type switch
            {
                UserContactType.User => new AssetAssignment(asset.Id, assetDto.UserId, null),
                UserContactType.Contact => new AssetAssignment(asset.Id, null, assetDto.UserId),
                _ => throw new EntityValidationException("Tipo de usuário ou contato inválido.")
            };
        }

        if (assetDto.Files is not null && assetDto.Files.Any())
        {
            foreach (var file in assetDto.Files)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    var fileContent = memoryStream.ToArray();

                    var assetFile = new AssetFile(asset.Id, file.FileName, fileContent);
                    await _assetFileRepository.SaveAsync(assetFile);
                }
            }
        }

        await _assetRepository.SaveAsync(asset);

        await _unitOfWork.CommitAsync();

        if (assetAssignment is not null)
            await _assetAssignmentRepository.SaveAsync(assetAssignment);

        await _unitOfWork.CommitAsync();

        return true;
    }

    public async Task<bool> UpdateAssetAsync(AssetDto assetDto)
    {
        var asset = await _assetRepository.GetByIdAsync(assetDto.Id);

        if (asset is null)
            throw new EntityValidationException("Ativo não encontrado.");

        var assetFiles = await _assetFileRepository.GetAssetFilesAsync(asset.Id);

        asset.UpdateDetails(
            assetDto.Name,
            assetDto.SerialNumber,
            assetDto.AssetType,
            assetDto.PurchaseDate,
            assetDto.Value,
            assetDto.Description
        );

        if (assetDto.UserId.HasValue && assetDto.UserId != Guid.Empty)
        {
            var existingAssignment = await _assetAssignmentRepository.GetAssignmentUserIdAsync(asset.Id);

            if (existingAssignment != assetDto.UserId)
            {
                var assetAssignment = await _assetAssignmentRepository.GetByIdOrNullAsync(asset.Id);

                if (assetAssignment is null)
                {
                    assetAssignment = assetDto.Type switch
                    {
                        UserContactType.User => new AssetAssignment(asset.Id, assetDto.UserId, null),
                        UserContactType.Contact => new AssetAssignment(asset.Id, null, assetDto.UserId),
                        _ => throw new EntityValidationException("Tipo de usuário ou contato inválido.")
                    };

                    await _assetAssignmentRepository.SaveAsync(assetAssignment);
                }
                else
                {
                    switch (assetDto.Type)
                    {
                        case UserContactType.User:
                            assetAssignment.AssignToUser(assetDto.UserId.Value);
                            break;
                        case UserContactType.Contact:
                            assetAssignment.AssignToContact(assetDto.UserId.Value);
                            break;
                        default:
                            throw new EntityValidationException("Tipo de usuário ou contato inválido.");
                    }

                    _assetAssignmentRepository.Update(assetAssignment);
                }
            }
        }
        else
        {
            var assetAssignment = await _assetAssignmentRepository.GetByIdOrNullAsync(asset.Id);
            assetAssignment?.CancelAssignment();
        }

        if (assetDto.Files is not null && assetDto.Files.Any())
        {
            foreach (var file in assetDto.Files)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    var fileContent = memoryStream.ToArray();

                    var assetFile = new AssetFile(asset.Id, file.FileName, fileContent);
                    await _assetFileRepository.SaveAsync(assetFile);
                }
            }
        }

        foreach (var file in assetFiles)
        {
            if (!assetDto.ExistingFiles.Any(f => f.Id == file.Id))
                _assetFileRepository.Delete(file);
        }

        _assetRepository.Update(asset);
        await _unitOfWork.CommitAsync();

        return true;
    }


    public async Task<bool> DeleteAssetsAsync(List<Guid> ids)
    {
        var assets = await _assetRepository.GetAllAsync();
        var assetsToDelete = assets.Where(a => ids.Contains(a.Id)).ToList();

        if (!assetsToDelete.Any())
            return false;

        foreach (var asset in assetsToDelete)
            _assetRepository.Delete(asset);

        await _unitOfWork.CommitAsync();
        return true;
    }

    public async Task<bool> DeleteAssetAsync(Guid id)
    {
        var asset = await _assetRepository.GetByIdAsync(id);

        if (asset is null)
            throw new EntityValidationException("Ativo não encontrado.");

        _assetRepository.Delete(asset);
        await _unitOfWork.CommitAsync();

        return true;
    }

    public async Task<List<AssetFileDto>> GetAssetFilesAsync(Guid assetId)
    {
        var assetFiles = await _assetFileRepository.GetAssetFilesAsync(assetId);

        return assetFiles.Select(f => new AssetFileDto
        {
            Id = f.Id,
            FileName = f.FileName,
            FileContent = f.FileContent
        }).ToList();
    }
}
