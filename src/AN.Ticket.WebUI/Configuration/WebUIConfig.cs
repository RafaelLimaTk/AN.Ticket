using AN.Ticket.Hangfire.Configuration;
using AN.Ticket.Infra.Data.Context;
using AN.Ticket.WebUI.Filters;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace AN.Ticket.WebUI.Configuration;

public static class WebUIConfig
{
    public static IServiceCollection AddWebUI(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("AtlasBd");
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseMySql(
                connectionString,
                ServerVersion.AutoDetect(connectionString)
            )
        );

        services.AddControllersWithViews(options =>
        {
            options.Filters.Add<CustomExceptionFilter>();
        });

        services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = 104857600;
        });

        services.AddHangfireConfiguration(configuration);
        services.AddCustomAuthentication();
        services.AddRegister(configuration);

        var supportedCultures = new[] { "pt-BR", "en-US" };

        services.Configure<RequestLocalizationOptions>(options =>
        {
            var supportedCulturesInfo = supportedCultures.Select(c => new CultureInfo(c)).ToList();
            options.DefaultRequestCulture = new RequestCulture("pt-BR");
            options.SupportedCultures = supportedCulturesInfo;
            options.SupportedUICultures = supportedCulturesInfo;
        });

        return services;
    }

    public static IApplicationBuilder UseWebUI(
        this IApplicationBuilder app,
        IWebHostEnvironment env,
        IConfiguration configuration
    )
    {
        if (!env.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        var localizationOptions = app.ApplicationServices.GetService<IOptions<RequestLocalizationOptions>>().Value;
        app.UseRequestLocalization(localizationOptions);

        app.UseAuthentication();
        app.UseAuthorization();
        app.UseHangfireConfiguration(configuration);
        app.UseStaticFiles(); // Para servir arquivos estáticos

        //HangfireJobsConfig.ConfigureRecurringJobs(configuration);

        return app;
    }
}