using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TaskManager.Application.Common.Behaviors;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Application.Users.Common.Interfaces;
using TaskManager.Application.Users.Common.Services;

namespace TaskManager.Application.Common
{
    public static class DependencyInjection
    {
        //In Program.cs -> builder.Services.AddApplication();
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // Get the assembly containing the current code
            var assembly = Assembly.GetExecutingAssembly();
            //Register MediatR handlers from the current assembly
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
            //Register FluentValidation validators from the current assembly
            services.AddValidatorsFromAssembly(assembly);
            //Register MediatR Pipeline to run ValidationBehavior
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            //Register ManagerHierarchyService to detect cycle in managers list
            services.AddScoped<IManagerHierarchyService, ManagerHierarchyService>();
            // services.AddScoped<INotificationDispatcher, NotificationDispatcher>();
            // services.AddSingleton<IDateTime,DateTimeService>();
            return services;
        }
    }
}