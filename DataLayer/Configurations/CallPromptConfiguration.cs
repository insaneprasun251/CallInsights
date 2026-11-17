using Core.BusinessObject;
using Core.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataLayer.Configurations
{
    public class CallPromptConfiguration : IEntityTypeConfiguration<CallPrompt>
    {
        public void Configure(EntityTypeBuilder<CallPrompt> builder)
        {
            builder.ToTable(DbTables.CallPrompts);
            builder.HasKey(p => p.Id);

            builder.Property(p => p.TemplateKey).IsRequired().HasMaxLength(FieldLengths.TemplateKey);
            builder.Property(p => p.TemplateVersion).IsRequired().HasMaxLength(FieldLengths.TemplateVersion);
            builder.Property(p => p.ContextText).IsRequired();
            builder.Property(p => p.PromptText).IsRequired();
            builder.Property(p => p.ResponseModel).HasMaxLength(FieldLengths.ModelName);

            builder.HasOne(p => p.Call)
                   .WithMany(c => c.Prompts)
                   .HasForeignKey(p => p.CallId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(p => p.CallId);
        }
    }
}
