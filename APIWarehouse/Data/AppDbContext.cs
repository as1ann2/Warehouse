using Microsoft.EntityFrameworkCore;
using APIWarehouse.Models;

namespace APIWarehouse
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Warehouse> Warehouses { get; set; }
    }
}