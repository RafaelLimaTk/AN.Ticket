using AN.Ticket.Application.Helpers.EmailSender;
using AN.Ticket.Application.Helpers.TokenProvider;
using AN.Ticket.Application.Interfaces;
using AN.Ticket.Application.Interfaces.Base;
using AN.Ticket.Application.Mappings;
using AN.Ticket.Application.Services;
using AN.Ticket.Application.Services.Base;
using AN.Ticket.Domain.Accounts;
using AN.Ticket.Domain.Interfaces;
using AN.Ticket.Domain.Interfaces.Base;
using AN.Ticket.Infra.Data.Identity;
using AN.Ticket.Infra.Data.Repositories;
using AN.Ticket.Infra.Data.Repositories.Base;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using OpenAI_API;

namespace AN.Ticket.WebUI.Configuration;

public static class DependencyInjectionConfig
{
    public static void AddRegister(this IServiceCollection services, IConfiguration configuration)
    {
        #region Base
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped(typeof(IService<,>), typeof(Service<,>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        #endregion

        #region Identity
        services.AddScoped<ISeedUserRoleInitial, SeedUserRoleInitial>();
        #endregion

        #region Services
        services.AddScoped<IChatGptService, ChatGptService>();
        services.AddScoped<IEmailMonitoringService, EmailMonitoringService>();
        services.AddScoped<ITicketService, TicketService>();
        services.AddScoped<IContactService, ContactService>();
        services.AddScoped<IAttachmentService, AttachmentService>();
        services.AddScoped<IActivityService, ActivityService>();
        services.AddScoped<IPaymantPlanService, PaymantPlanService>();
        services.AddScoped<IHomeService, HomeService>();
        services.AddScoped<ISatisfactionRatingService, SatisfactionRatingService>();
        services.AddScoped<ITeamService, TeamService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IFileService, FileService>();
        services.AddScoped<IAssetService, AssetService>();
        services.AddScoped<IAssetAssignmentService, AssetAssignmentService>();
        #endregion

        #region Repositories
        services.AddSingleton<ITempDataDictionaryFactory, TempDataDictionaryFactory>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<IContactRepository, ContactRepository>();
        services.AddScoped<ITicketMessageRepository, TicketMessageRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IPaymentPlanRepository, PaymentPlanRepository>();
        services.AddScoped<ISatisfactionRatingRepository, SatisfactionRatingRepository>();
        services.AddScoped<IInteractionHistoryRepository, InteractionHistoryRepository>();
        services.AddScoped<IActivityRepository, ActivityRepository>();
        services.AddScoped<IAttachmentRepository, AttachmentRepository>();
        services.AddScoped<ITeamRepository, TeamRepository>();
        services.AddScoped<IAssetRepository, AssetRepository>();
        services.AddScoped<IAssetAssignmentRepository, AssetAssignmentRepository>();
        #endregion

        #region SMTP
        services.Configure<SmtpSettings>(configuration.GetSection("SmtpSettings"));
        services.Configure<BaseUrlSettings>(configuration.GetSection("BaseUrlSettings"));
        services.AddSingleton<CancellationTokenProvider>();
        services.AddScoped<IEmailSenderService, EmailSenderService>();
        #endregion

        #region AutoMapper Profiles
        services.AddAutoMapper(typeof(DomainToDTOMappingProfile));
        #endregion

        #region OpenAI
        var token = configuration.GetValue<string>("OpenAI:ApiKey");
        var authentication = new APIAuthentication(token);
        services.AddScoped<IOpenAIAPI>(x => new OpenAIAPI(authentication));
        #endregion
    }
}