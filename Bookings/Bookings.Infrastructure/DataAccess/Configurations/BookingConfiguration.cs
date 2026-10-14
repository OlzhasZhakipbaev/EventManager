using Bookings.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bookings.Infrastructure.DataAccess.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<BookingModel>
{
    public void Configure(EntityTypeBuilder<BookingModel> builder)
    {
        builder.ToTable("bookings");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(b => b.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(b => b.EventId)
            .HasColumnName("event_id")
            .IsRequired();

        builder.Property(b => b.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(b => b.CreatedAt).IsRequired();
        builder.Property(b => b.ProcessedAt);

        builder.HasIndex(b => b.UserId);
        builder.HasIndex(b => b.EventId);
    }
}
