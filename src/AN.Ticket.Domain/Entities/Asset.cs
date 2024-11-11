using AN.Ticket.Domain.Entities.Base;

namespace AN.Ticket.Domain.Entities;
public class Asset : EntityBase
{
    public string Name { get; private set; }
    public string SerialNumber { get; private set; }
    public string AssetType { get; private set; }
    public DateTime PurchaseDate { get; private set; }
    public decimal Value { get; private set; }
    public string? Description { get; private set; }

    public ICollection<AssetAssignment> AssetAssignments { get; private set; }
    public ICollection<AssetFile> AssetFiles { get; private set; }

    protected Asset()
    {
        AssetAssignments = new List<AssetAssignment>();
        AssetFiles = new List<AssetFile>();
    }

    public Asset(string name, string serialNumber, string assetType, DateTime purchaseDate, decimal value, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name é obrigatório.", nameof(name));
        if (string.IsNullOrWhiteSpace(serialNumber)) throw new ArgumentException("SerialNumber é obrigatório.", nameof(serialNumber));
        if (string.IsNullOrWhiteSpace(assetType)) throw new ArgumentException("AssetType é obrigatório.", nameof(assetType));
        if (purchaseDate == default) throw new ArgumentException("PurchaseDate deve ser uma data válida.", nameof(purchaseDate));
        if (value <= 0) throw new ArgumentException("Value deve ser maior que zero.", nameof(value));

        Name = name;
        SerialNumber = serialNumber;
        AssetType = assetType;
        PurchaseDate = purchaseDate;
        Value = value;
        Description = description;
        AssetAssignments = new List<AssetAssignment>();
        AssetFiles = new List<AssetFile>();
    }

    public void UpdateDetails(string name, string serialNumber, string assetType, DateTime purchaseDate, decimal value, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name é obrigatório.", nameof(name));
        if (string.IsNullOrWhiteSpace(serialNumber)) throw new ArgumentException("SerialNumber é obrigatório.", nameof(serialNumber));
        if (string.IsNullOrWhiteSpace(assetType)) throw new ArgumentException("AssetType é obrigatório.", nameof(assetType));
        if (purchaseDate == default) throw new ArgumentException("PurchaseDate deve ser uma data válida.", nameof(purchaseDate));
        if (value <= 0) throw new ArgumentException("Value deve ser maior que zero.", nameof(value));

        Name = name;
        SerialNumber = serialNumber;
        AssetType = assetType;
        PurchaseDate = purchaseDate;
        Value = value;
        Description = description;
    }
}
