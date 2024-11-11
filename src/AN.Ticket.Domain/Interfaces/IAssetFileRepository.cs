using AN.Ticket.Domain.Entities;
using AN.Ticket.Domain.Interfaces.Base;
using System.Threading.Tasks;

namespace AN.Ticket.Domain.Interfaces;
public interface IAssetFileRepository : IRepository<AssetFile>
{
    Task<List<AssetFile>> GetAssetFilesAsync(Guid assetId);
}
