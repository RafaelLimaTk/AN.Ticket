using AN.Ticket.WebUI.ViewModels.Setting;
using Microsoft.AspNetCore.Mvc;

namespace AN.Ticket.WebUI.Components;

[ViewComponent]
public class ApiKeyViewComponent : ViewComponent
{
    private readonly IConfiguration _configuration;

    public ApiKeyViewComponent(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var viewModel = new ApiKeyViewModel
        {
            ApiKey = _configuration["OpenAI:ApiKey"]
        };
        return View(viewModel);
    }
}
