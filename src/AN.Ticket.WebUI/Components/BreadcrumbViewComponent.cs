using Microsoft.AspNetCore.Mvc;

namespace AN.Ticket.WebUI.Components;

[ViewComponent]
public class BreadcrumbViewComponent : ViewComponent
{
    private readonly Dictionary<string, string> _controllerFriendlyNames = new Dictionary<string, string>
    {
        { "Home", "Início" },
        { "Ticket", "Tickets" },
        { "Contact", "Contatos" },
        { "Activity", "Atividades" },
        { "Asset", "Ativos" },
        { "Department", "Departamentos" },
        { "User", "Usuários" }
    };

    private readonly Dictionary<string, Dictionary<string, string>> _actionFriendlyNames = new Dictionary<string, Dictionary<string, string>>
    {
        {
            "Ticket", new Dictionary<string, string>
            {
                { "Index", "Dashboard Tickets" },
                { "UnassignedTickets", "Tickets Não Atribuídos" },
                { "UserTickets", "Meus Tickets" },
                { "CreateTicket", "Criar Ticket" }
            }
        },
        {
            "Contact", new Dictionary<string, string>
            {
                { "Index", "Contatos" },
                { "CreateContact", "Criar Contato" }
            }
        },
        {
            "Activity", new Dictionary<string, string>
            {
                { "Index", "Atividades" },
                { "Create", "Criar Atividade" }
            }
        },
        {
            "Asset", new Dictionary<string, string>
            {
                { "Index", "Ativos" },
                { "Create", "Criar Ativo" }
            }
        },
        {
            "Department", new Dictionary<string, string>
            {
                { "Index", "Departamentos" },
                { "Create", "Criar Departamento" }
            }
        },
        {
            "User", new Dictionary<string, string>
            {
                { "Index", "Usuários" },
                { "Create", "Criar Usuário" }
            }
        }
    };

    public IViewComponentResult Invoke()
    {
        var breadcrumbItems = GenerateBreadcrumb();
        return View(breadcrumbItems);
    }

    private List<BreadcrumbItem> GenerateBreadcrumb()
    {
        var breadcrumbItems = new List<BreadcrumbItem>();

        breadcrumbItems.Add(new BreadcrumbItem
        {
            Name = "",
            Url = Url.Action("Index", "Home"),
            Icon = "bi bi-house-door-fill"
        });

        var routeData = HttpContext.Request.RouteValues;

        if (routeData.ContainsKey("controller"))
        {
            string controller = routeData["controller"].ToString();
            string controllerFriendlyName = _controllerFriendlyNames.ContainsKey(controller) ? _controllerFriendlyNames[controller] : controller;

            breadcrumbItems.Add(new BreadcrumbItem { Name = controllerFriendlyName, Url = Url.Action("Index", controller) });

            if (routeData.ContainsKey("action"))
            {
                string action = routeData["action"].ToString();

                if (_actionFriendlyNames.ContainsKey(controller))
                {
                    var actions = _actionFriendlyNames[controller];
                    foreach (var act in actions)
                    {
                        if (act.Key == "Index") continue;
                        breadcrumbItems.Add(new BreadcrumbItem { Name = act.Value, Url = Url.Action(act.Key, controller) });

                        if (act.Key == action)
                            break;
                    }
                }
            }
        }

        return breadcrumbItems;
    }
}

public class BreadcrumbItem
{
    public string? Name { get; set; }
    public string? Url { get; set; }
    public string? Icon { get; set; }
}
