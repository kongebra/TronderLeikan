using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TronderLeikan.Domain.Departments;
using TronderLeikan.Domain.Persons;

namespace TronderLeikan.Infrastructure.Persistence.Configurations;

internal sealed class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.FirstName)
            .IsRequired()
            .HasMaxLength(200);
        builder.Property(p => p.LastName)
            .IsRequired()
            .HasMaxLength(200);
        builder.Property(p => p.DepartmentId);
        builder.Property(p => p.HasProfileImage);

        // Avdeling er valgfri — null betyr ingen avdeling satt ennå
        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(p => p.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.ToTable("Persons");
    }
}
