using FamilySync.Core.Helpers;
using FamilySync.Core.Persistence;
using FamilySync.Models.Authentication.Entities;
using FamilySync.Services.Authentication.Persistence;
using FamilySync.Services.Authentication.Services;
using Microsoft.AspNetCore.Identity;

namespace FamilySync.Services.Authentication;

public class Configuration : ServiceConfiguration
{
    public override void Configure(IApplicationBuilder app)
    {
        
    }

    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddMySqlContext<AuthContext>("auth", Configuration);

        services.AddIdentity<UserIdentity, IdentityRole<Guid>>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequiredLength = 9;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddEntityFrameworkStores<AuthContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IClaimService, ClaimService>();
        services.AddScoped<IUserIdentityService, UserIdentityService>();
    }
}