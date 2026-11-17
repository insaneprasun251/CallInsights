using Core.BusinessObject;
using Core.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataLayer.Configurations
{
    public class ProcessingEventConfiguration : IEntityTypeConfiguration<ProcessingEvent>
    {
        public void Configure(EntityTypeBuilder<ProcessingEvent> builder)
        {
            builder.ToTable(DbTables.ProcessingEvents);
            builder.HasKey(e => e.Id);

            builder.Property(e => e.Step).IsRequired().HasMaxLength(FieldLengths.EventStep);
            builder.Property(e => e.Message).HasMaxLength(FieldLengths.EventMessage);

            builder.HasOne(e => e.Call)
                   .WithMany(c => c.Events)
                   .HasForeignKey(e => e.CallId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(e => e.CallId);
        }
    }
}
