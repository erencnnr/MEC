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
using MEC.Domain.Entity.Leave;
using MEC.Domain.Entity.Logging;
using MEC.Domain.Entity.Overtime;


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
        public DbSet<ServiceHistory> ServiceHistories { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<Loan> Loans { get; set; }
        public DbSet<LoanStatus> LoanStatuses { get; set; }
        public DbSet<SchoolClass> SchoolClasses { get; set; }
        
        public DbSet<Employee> Employees { get; set; }
        public DbSet<EmployeeType> EmployeeTypes { get; set; }
        public DbSet<EmployeePortal> EmployeePortals { get; set; }
        public DbSet<Announcement> Announcement { get; set; }
        public DbSet<AnnouncementAttachment> AnnouncementAttachments { get; set; }
        public DbSet<AnnouncementImage> AnnouncementImages { get; set; }
        public DbSet<LibraryFolder> LibraryFolders { get; set; }
        public DbSet<LibraryDocument> LibraryDocuments { get; set; }
        public DbSet<SliderImage> SliderImages { get; set; }
        public DbSet<ApiLog> ApiLogs { get; set; }
        public DbSet<UserActionLog> UserActionLogs { get; set; }
        public DbSet<LeaveType> LeaveTypes { get; set; }
        public DbSet<Holiday> Holidays { get; set; }
        public DbSet<LeaveAgreement> LeaveAgreements { get; set; }
        public DbSet<OvertimeRequest> OvertimeRequests { get; set; }

        // Çakışmayı önlemek için sınıfı tam adıyla (MEC.Domain.Entity.Leave.Leave) belirtiyoruz
        public DbSet<MEC.Domain.Entity.Leave.Leave> Leaves { get; set; }
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

            modelBuilder.Entity<LeaveType>()
                .HasIndex(x => x.Code)
                .IsUnique();

            modelBuilder.Entity<Holiday>()
                .Property(x => x.CreatedDate)
                .HasColumnName("created_date");

            modelBuilder.Entity<Holiday>()
                .Property(x => x.UpdateDate)
                .HasColumnName("update_date");

            modelBuilder.Entity<Holiday>()
                .HasIndex(x => new { x.StartDate, x.EndDate });

            modelBuilder.Entity<LeaveAgreement>()
                .Property(x => x.CreatedDate)
                .HasColumnName("created_date");

            modelBuilder.Entity<LeaveAgreement>()
                .Property(x => x.UpdateDate)
                .HasColumnName("update_date");

            modelBuilder.Entity<LeaveAgreement>()
                .HasIndex(x => x.EmployeePortalId)
                .IsUnique();

            modelBuilder.Entity<LeaveAgreement>()
                .HasOne(x => x.EmployeePortal)
                .WithMany()
                .HasForeignKey(x => x.EmployeePortalId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MEC.Domain.Entity.Leave.Leave>()
                .HasOne(x => x.LeaveType)
                .WithMany(x => x.Leaves)
                .HasForeignKey(x => x.LeaveTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OvertimeRequest>()
                .Property(x => x.CreatedDate)
                .HasColumnName("created_date");

            modelBuilder.Entity<OvertimeRequest>()
                .Property(x => x.UpdateDate)
                .HasColumnName("update_date");

            modelBuilder.Entity<OvertimeRequest>()
                .HasIndex(x => x.EmployeePortalId);

            modelBuilder.Entity<OvertimeRequest>()
                .HasIndex(x => x.Status);

            modelBuilder.Entity<OvertimeRequest>()
                .HasOne(x => x.EmployeePortal)
                .WithMany()
                .HasForeignKey(x => x.EmployeePortalId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AnnouncementAttachment>()
                .Property(x => x.CreatedDate)
                .HasColumnName("created_date");

            modelBuilder.Entity<AnnouncementAttachment>()
                .Property(x => x.UpdateDate)
                .HasColumnName("update_date");

            modelBuilder.Entity<AnnouncementAttachment>()
                .HasOne(x => x.Announcement)
                .WithMany(x => x.Attachments)
                .HasForeignKey(x => x.AnnouncementId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AnnouncementImage>()
                .Property(x => x.CreatedDate)
                .HasColumnName("created_date");

            modelBuilder.Entity<AnnouncementImage>()
                .Property(x => x.UpdateDate)
                .HasColumnName("update_date");

            modelBuilder.Entity<AnnouncementImage>()
                .HasOne(x => x.Announcement)
                .WithMany(x => x.Images)
                .HasForeignKey(x => x.AnnouncementId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LibraryFolder>()
                .Property(x => x.CreatedDate)
                .HasColumnName("created_date");

            modelBuilder.Entity<LibraryFolder>()
                .Property(x => x.UpdateDate)
                .HasColumnName("update_date");

            modelBuilder.Entity<LibraryFolder>()
                .HasOne(x => x.ParentFolder)
                .WithMany(x => x.ChildFolders)
                .HasForeignKey(x => x.ParentFolderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LibraryDocument>()
                .Property(x => x.CreatedDate)
                .HasColumnName("created_date");

            modelBuilder.Entity<LibraryDocument>()
                .Property(x => x.UpdateDate)
                .HasColumnName("update_date");

            modelBuilder.Entity<LibraryDocument>()
                .HasOne(x => x.Folder)
                .WithMany(x => x.Documents)
                .HasForeignKey(x => x.FolderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SliderImage>()
                .Property(x => x.CreatedDate)
                .HasColumnName("created_date");

            modelBuilder.Entity<SliderImage>()
                .Property(x => x.UpdateDate)
                .HasColumnName("update_date");

            base.OnModelCreating(modelBuilder);
        }
    }
}
