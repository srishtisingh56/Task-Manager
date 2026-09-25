using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskManager.Application.Common.Interfaces;
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

            services.AddSingleton<IDateTime, DateTimeService>();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<INotificationDispatcher, NotificationDispatcher>();

            return services;
        }
    }
}
