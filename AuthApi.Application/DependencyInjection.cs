using AuthApi.Application.Features.Administrative;
using AuthApi.Application.Features.Companies;
using AuthApi.Application.Features.EcosystemApps;
using AuthApi.Application.Features.Security;
using AuthApi.Application.Features.Users;
using Microsoft.Extensions.DependencyInjection;

namespace AuthApi.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICompanyService, CompanyService>();
        services.AddScoped<IEcosystemAppService, EcosystemAppService>();
        services.AddScoped<IAdministrativeService, AdministrativeService>();
        services.AddScoped<ISecurityService, SecurityService>();

        return services;
    }
}
