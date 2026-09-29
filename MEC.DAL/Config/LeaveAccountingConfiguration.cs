using MEC.Domain.Entity.Leave;
using MEC.Domain.Entity.Employee;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MEC.DAL.Config;

public class LeaveAccountingConfiguration : IEntityTypeConfiguration<LeaveAccount>,
    IEntityTypeConfiguration<LeaveMovement>, IEntityTypeConfiguration<LeaveAccrual>,
    IEntityTypeConfiguration<LeaveCharge>, IEntityTypeConfiguration<LeaveCancellation>,
    IEntityTypeConfiguration<LeaveImportRow>, IEntityTypeConfiguration<LeaveCalendar>,
    IEntityTypeConfiguration<LeaveCalendarVersion>, IEntityTypeConfiguration<LeaveAgreementVersion>, IEntityTypeConfiguration<LeaveImportBatch>
{
    public void Configure(EntityTypeBuilder<LeaveImportBatch> b) => b.HasIndex(x=>x.BatchHash).IsUnique();
    public void Configure(EntityTypeBuilder<LeaveAccount> b) { b.HasOne<EmployeePortal>().WithMany().HasForeignKey(x=>x.EmployeeId).OnDelete(DeleteBehavior.Restrict); b.HasIndex(x => x.EmployeeId).IsUnique(); b.Property(x => x.Revision).IsConcurrencyToken(); }
    public void Configure(EntityTypeBuilder<LeaveMovement> b) { b.HasOne<EmployeePortal>().WithMany().HasForeignKey(x=>x.EmployeeId).OnDelete(DeleteBehavior.Restrict); b.HasOne<Leave>().WithMany().HasForeignKey(x=>x.LeaveId).OnDelete(DeleteBehavior.Restrict); b.HasIndex(x => new { x.EmployeeId, x.OperationKey }).IsUnique(); b.HasIndex(x => new { x.EmployeeId, x.EffectiveDate }); }
    public void Configure(EntityTypeBuilder<LeaveAccrual> b) => b.HasIndex(x => new { x.EmployeeId, x.Cycle, x.ServiceYear }).IsUnique();
    public void Configure(EntityTypeBuilder<LeaveCharge> b) { b.HasIndex(x => x.LeaveId).IsUnique(); b.HasOne<Leave>().WithMany().HasForeignKey(x=>x.LeaveId).OnDelete(DeleteBehavior.Cascade); }
    public void Configure(EntityTypeBuilder<LeaveCancellation> b) { b.HasIndex(x => x.LeaveId).IsUnique(); b.HasOne<Leave>().WithMany().HasForeignKey(x=>x.LeaveId).OnDelete(DeleteBehavior.Restrict); }
    public void Configure(EntityTypeBuilder<LeaveImportRow> b) => b.HasIndex(x => new { x.BatchHash, x.Email }).IsUnique();
    public void Configure(EntityTypeBuilder<LeaveCalendar> b) { b.HasIndex(x => x.Year).IsUnique(); b.Property(x => x.Revision).IsConcurrencyToken(); }
    public void Configure(EntityTypeBuilder<LeaveCalendarVersion> b) => b.HasIndex(x => new { x.Year, x.Revision }).IsUnique();
    public void Configure(EntityTypeBuilder<LeaveAgreementVersion> b) => b.HasIndex(x => new { x.AgreementId, x.Version }).IsUnique();
}
