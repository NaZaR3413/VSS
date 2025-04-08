using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.EntityFrameworkCore.Modeling;
using web_backend.Livestreams;

namespace web_backend.Configurations
{
    public class LivestreamConfiguration : IEntityTypeConfiguration<Livestream>
    {
        public void Configure(EntityTypeBuilder<Livestream> builder)
        {
            builder.ToTable(nameof(Livestream), web_backendConsts.DbSchema);
            builder.ConfigureByConvention();

            builder.HasKey(c => c.Id);

            builder.Property(cs => cs.HlsUrl).IsRequired();
            builder.Property(cs => cs.HomeTeamId).IsRequired();
            builder.Property(cs => cs.AwayTeamId).IsRequired();
            builder.Property(cs => cs.HomeScore).IsRequired();
            builder.Property(cs => cs.AwayScore).IsRequired();
            builder.Property(cs => cs.EventType).IsRequired();
            builder.Property(cs => cs.StreamStatus).IsRequired();
            builder.Property(cs => cs.EventDate).IsRequired();
        }

    }
}
