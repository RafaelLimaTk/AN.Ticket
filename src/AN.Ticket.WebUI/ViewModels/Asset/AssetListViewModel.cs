using AN.Ticket.Application.DTOs.Asset;

namespace AN.Ticket.WebUI.ViewModels.Asset;

public class AssetListViewModel
{
    public IEnumerable<AssetDto> Assets { get; set; } = new List<AssetDto>();
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public string SearchTerm { get; set; } = string.Empty;
    public string OrderBy { get; set; } = "PurchaseDate";

    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
}
