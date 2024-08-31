﻿using Hangfire.Dashboard;

namespace AN.Ticket.Hangfire.Configuration;
public class HangfireAuthorizationFilter
    : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return !httpContext.User.Identity.IsAuthenticated;
    }
}
