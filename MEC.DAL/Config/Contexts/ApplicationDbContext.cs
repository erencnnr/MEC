using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MEC.Domain.Entity.School;
using MEC.Domain.Entity.Asset;
using MEC.Domain.Entity.Invoice;
using MEC.Domain.Entity.Loan;
using MEC.Domain.Entity.Employee;


namespace MEC.DAL.Config.Contexts
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<School> Schools { get; set; }
        public DbSet<Asset> Assets { get; set; }
        public DbSet<AssetType> AssetTypes { get; set; }
        public DbSet<AssetStatus> AssetStatuses { get; set; }
        public DbSet<AssetImage> AssetImages { get; set; }
        public DbSet<AssetAttachment> AssetAttachments { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<Loan> Loans { get; set; }
        public DbSet<LoanStatus> LoanStatuses { get; set; }
        public DbSet<SchoolClass> SchoolClasses { get; set; }
        
        public DbSet<Employee> Employees { get; set; }
        public DbSet<EmployeeType> EmployeeTypes { get; set; }
        


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
            // 23.12.2025 tablo isimleri entitylerde tanımlandı bu blok kaldırıldı
            //foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            //{
            //    // Entity'nin class adını (örn: Asset) alıp tablo adı yapar.
            //    entityType.SetTableName(entityType.DisplayName().ToLower());
            //}

            base.OnModelCreating(modelBuilder);
        }
    }
}
