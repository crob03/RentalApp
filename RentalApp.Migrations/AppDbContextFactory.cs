using Microsoft.EntityFrameworkCore.Design;
using RentalApp.Database.Data;

namespace RentalApp.Migrations;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args) => new AppDbContext();
}
