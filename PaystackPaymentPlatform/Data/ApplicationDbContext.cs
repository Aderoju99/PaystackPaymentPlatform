using Microsoft.EntityFrameworkCore;
using PaystackPaymentPlatform.Models;

namespace PaystackPaymentPlatform.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Payment> Payments { get; set; }
    }
}
