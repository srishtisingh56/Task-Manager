using System.Text;
using System.Text.Json.Serialization;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TaskManager.Application.Common;          // adjust to the namespace of your AddApplication()
using TaskManager.Application.Common.Interfaces;
using TaskManager.Infrastructure;      // AddInfrastructure()
using TaskManager.Infrastructure.Jobs;  // RegisterRecurringJobs()
using TaskManager.Infrastructure.Persistence;
using TaskManager.Presentation.ExceptionHandling;
using TaskManager.Presentation.Services;
using TaskManager.Presentation.Startup;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSection = builder.Configuration.GetSection("JwtSettings");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSection["Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Secret"]!))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Deny-by-default: every endpoint requires auth unless marked [AllowAnonymous].
builder.Services.AddControllers(options =>
    options.Filters.Add(new AuthorizeFilter()))
    .AddJsonOptions(options =>
     {
         options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
         options.JsonSerializerOptions.Converters.Add(new OptionalJsonConverterFactory());
     });

builder.Services.AddOpenApi(); // built-in in .NET 9+/10, no Swashbuckle needed

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseHangfireDashboard("/hangfire"); // dev only; add an   admin auth filter before deploying
}

app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Services.RegisterRecurringJobs();
await app.SeedUsersAsync();

app.Run();