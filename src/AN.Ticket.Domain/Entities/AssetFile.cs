using AN.Ticket.Domain.Entities.Base;

namespace AN.Ticket.Domain.Entities;
public class AssetFile : EntityBase
{
    public Guid AssetId { get; private set; }
    public string FileName { get; private set; }
    public byte[] FileContent { get; private set; }
    public Asset? Asset { get; set; }

    protected AssetFile() { }

    public AssetFile(Guid assetId, string fileName, byte[] fileContent)
    {
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("FileName é obrigatório.", nameof(fileName));
        if (fileContent == null || fileContent.Length == 0) throw new ArgumentException("FileContent é obrigatório.", nameof(fileContent));

        AssetId = assetId;
        FileName = fileName;
        FileContent = fileContent;
    }
}
