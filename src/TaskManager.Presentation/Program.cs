using System.Text;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TaskManager.Application.Common;          // adjust to the namespace of your AddApplication()
using TaskManager.Application.Common.Interfaces;
using TaskManager.Infrastructure;      // AddInfrastructure()
using TaskManager.Infrastructure.Jobs;  // RegisterRecurringJobs()
using TaskManager.Infrastructure.Persistence;
using TaskManager.Presentation.ExceptionHandling;
using TaskManager.Presentation.Services;

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

// Deny-by-default: every endpoint requires auth unless marked [AllowAnonymous].
builder.Services.AddControllers(options =>
    options.Filters.Add(new AuthorizeFilter()));
builder.Services.AddOpenApi(); // built-in in .NET 9+/10, no Swashbuckle needed
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
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

app.Run();