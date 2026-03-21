using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudioB2B.Domain.Entities;

namespace StudioB2B.Infrastructure.Persistence.Tenant.Configurations;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
    }
}

public class PermissionPageConfiguration : IEntityTypeConfiguration<PermissionPage>
{
    public void Configure(EntityTypeBuilder<PermissionPage> builder)
    {
        builder.ToTable("PermissionPages");
        builder.HasKey(pp => new { pp.PermissionId, pp.PageId });

        builder.HasOne(pp => pp.Permission)
            .WithMany(p => p.Pages)
            .HasForeignKey(pp => pp.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pp => pp.Page)
            .WithMany(p => p.PermissionPages)
            .HasForeignKey(pp => pp.PageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PermissionPageColumnConfiguration : IEntityTypeConfiguration<PermissionPageColumn>
{
    public void Configure(EntityTypeBuilder<PermissionPageColumn> builder)
    {
        builder.ToTable("PermissionPageColumns");
        builder.HasKey(pc => new { pc.PermissionId, pc.PageColumnId });

        builder.HasOne(pc => pc.Permission)
            .WithMany(p => p.PageColumns)
            .HasForeignKey(pc => pc.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pc => pc.PageColumn)
            .WithMany(c => c.PermissionPageColumns)
            .HasForeignKey(pc => pc.PageColumnId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PermissionFunctionConfiguration : IEntityTypeConfiguration<PermissionFunction>
{
    public void Configure(EntityTypeBuilder<PermissionFunction> builder)
    {
        builder.ToTable("PermissionFunctions");
        builder.HasKey(pf => new { pf.PermissionId, pf.FunctionId });

        builder.HasOne(pf => pf.Permission)
            .WithMany(p => p.Functions)
            .HasForeignKey(pf => pf.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pf => pf.Function)
            .WithMany(f => f.PermissionFunctions)
            .HasForeignKey(pf => pf.FunctionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

