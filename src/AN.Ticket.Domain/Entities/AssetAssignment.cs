using AN.Ticket.Domain.Entities.Base;

namespace AN.Ticket.Domain.Entities;
public class AssetAssignment : EntityBase
{
    public Guid AssetId { get; private set; }
    public Asset Asset { get; set; }

    public Guid? UserId { get; private set; }
    public User? User { get; set; }

    public Guid? ContactId { get; private set; }
    public Contact? Contact { get; set; }

    public DateTime AssignedAt { get; private set; }
    public DateTime? ReturnedAt { get; private set; }

    protected AssetAssignment() { }

    public AssetAssignment(Guid assetId, Guid? userId = null, Guid? contactId = null)
    {
        if (assetId == Guid.Empty) throw new ArgumentException("AssetId não pode ser vazio.", nameof(assetId));
        if (userId == null && contactId == null) throw new ArgumentException("Deve ser fornecido UserId ou ContactId.");

        AssetId = assetId;
        UserId = userId;
        ContactId = contactId;
        AssignedAt = DateTime.UtcNow;
    }

    public void AssignToUser(Guid userId)
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId não pode ser vazio.", nameof(userId));

        UserId = userId;
        ContactId = null;
        AssignedAt = DateTime.UtcNow;
        ReturnedAt = null;
    }

    public void AssignToContact(Guid contactId)
    {
        if (contactId == Guid.Empty) throw new ArgumentException("ContactId não pode ser vazio.", nameof(contactId));

        ContactId = contactId;
        UserId = null;
        AssignedAt = DateTime.UtcNow;
        ReturnedAt = null;
    }

    public void ReturnAsset()
    {
        if (ReturnedAt != null) throw new InvalidOperationException("O ativo já foi devolvido.");
        ReturnedAt = DateTime.UtcNow;
    }

    public void CancelAssignment()
    {
        UserId = null;
        ContactId = null;
        ReturnedAt = null;
    }
}
