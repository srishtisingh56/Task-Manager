using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace TaskManager.Presentation.Startup
{
    // Presentation/Startup/DbSeeder.cs
public static class DbSeeder
{
    public static async Task SeedUsersAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        if (await db.Users.AnyAsync())
            return; // already seeded — don't duplicate on every restart

        var admin = User.Create(
            name: "Admin1",
            email: "singhsrishti019@gmail.com",
            phoneNumber: "1234567890",
            passwordHash: hasher.Hash("Admin@123"));
        admin.PromoteToSystemAdmin();

        var emp1 = User.Create(
            name: "Emp1",
            email: "srishti.singh@matrixcomsec.com",
            phoneNumber: "1234567891",
            passwordHash: hasher.Hash("Emp1@123"));

        var emp2 = User.Create(
            name: "Emp2",
            email: "ayush.sarvaiya@matrixcomsec.com",
            phoneNumber: "1234567892",
            passwordHash: hasher.Hash("Emp2@123"));

        db.Users.AddRange(admin, emp1, emp2);
        await db.SaveChangesAsync(CancellationToken.None);

        // Assign manager relationships after the initial save, so both sides have real Ids.
        emp1.AssignManager(admin);
        emp2.AssignManager(admin);
        await db.SaveChangesAsync(CancellationToken.None);
    }
}
}