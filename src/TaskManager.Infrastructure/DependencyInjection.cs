using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Infrastructure.Jobs;
using TaskManager.Infrastructure.Persistence;
using TaskManager.Infrastructure.Services;

namespace TaskManager.Infrastructure
{
    public static class DependencyInjection
    {
        //In Program.cs -> builder.Services.AddInfrastructure(builder.Configuration);
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

            services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

            services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
            services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

            services.AddHttpContextAccessor();

            services.AddSingleton<IDateTime, DateTimeService>();
            // services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<INotificationDispatcher, NotificationDispatcher>();
            services.AddScoped<IPasswordHasher, PasswordHasher>();
            services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
            // Hangfire configuration
            services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(o =>
                    o.UseNpgsqlConnection(configuration.GetConnectionString("DefaultConnection"))));
            // Add Hangfire server
            services.AddHangfireServer();

            services.AddScoped<OverdueTaskScanJob>();
            services.AddScoped<OffsetNotificationScanJob>();    

            return services;
        }
    }
}
