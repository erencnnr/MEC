using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MEC.Domain.Entity.School;

namespace MEC.DAL.Config.Contexts
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<School> Schools { get; set; }
        // ... diğerleri

        // 1. GLOBAL AYAR: String uzunlukları (EF Core 6.0 ve sonrası için en modern yol)
        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            // Projedeki (aksi belirtilmeyen) TÜM string property'ler veritabanında varchar(255) olsun.
            // Böylece "Key too long" hatası almazsın.
            configurationBuilder.Properties<string>().HaveMaxLength(255);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 2. CONFIGURATION YÜKLEME:
            // Bu satır, "Configurations" klasörüne yazdığın AssetConfiguration vb. sınıfları
            // otomatik bulur ve uygular. Tek tek eklemene gerek kalmaz.
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            // 3. GLOBAL AYAR: Tablo İsimlerini Tekil Yapma (Singular Table Names)
            // Eski koddaki "Remove Pluralizing" yerine modern yöntem budur.
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                // Entity'nin class adını (örn: Asset) alıp tablo adı yapar.
                entityType.SetTableName(entityType.DisplayName());
            }

            base.OnModelCreating(modelBuilder);
        }
    }
}
