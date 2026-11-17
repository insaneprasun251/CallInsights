using Core.BusinessObject;
using Core.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataLayer.Configurations
{
    public class CallConfiguration : IEntityTypeConfiguration<Call>
    {
        public void Configure(EntityTypeBuilder<Call> builder)
        {
            builder.ToTable(DbTables.Calls);
            builder.HasKey(c => c.Id);

            builder.Property(c => c.FileName).IsRequired().HasMaxLength(FieldLengths.FileName);
            builder.Property(c => c.FilePath).IsRequired().HasMaxLength(FieldLengths.FilePath);
            builder.Property(c => c.Language).HasMaxLength(FieldLengths.Language);
            builder.Property(c => c.ErrorMessage).HasMaxLength(FieldLengths.ErrorMessage);

            builder.HasIndex(c => c.FilePath).IsUnique();
            builder.HasIndex(c => c.Status);
        }
    }
}
