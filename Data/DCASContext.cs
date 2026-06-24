using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DCAS.Models;

namespace DCAS.Data
{
    public class DCASContext : DbContext
    {
        public DCASContext(DbContextOptions<DCASContext> options)
            : base(options)
        {
        }

        public DbSet<DCAS.Models.TodaySchedule> TodaySchedule { get; set; } = default!;
        public DbSet<DCAS.Models.PersonInfo> PersonInfo { get; set; } = default!;
        public DbSet<Services> Services { get; set; }
        public DbSet<DCAS.Models.MedicineInventory> MedicineInventory { get; set; } = default!;
        public DbSet<DCAS.Models.Users> Users { get; set; } = default!;
        public DbSet<DCAS.Models.Payment> Payments { get; set; } = default!;
        public DbSet<DCAS.Models.PaymentMedicine> PaymentMedicine { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var properties = entityType.GetProperties()
                    .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?));

                foreach (var property in properties)
                {
                    property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
                        v => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc),
                        v => v));
                }
            }
        }
    }
}
