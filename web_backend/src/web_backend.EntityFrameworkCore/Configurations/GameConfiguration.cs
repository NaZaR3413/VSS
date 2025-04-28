using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.EntityFrameworkCore.Modeling;
using web_backend.Games;

namespace web_backend.Configurations
{
    public class GameConfiguration : IEntityTypeConfiguration<Game>
    {
        public void Configure(EntityTypeBuilder<Game> builder)
        {
            builder.ToTable(nameof(Game), web_backendConsts.DbSchema);
            builder.ConfigureByConvention();

            builder.HasKey(c => c.Id);

            builder.Property(c => c.GameUrl).IsRequired().HasMaxLength(256);
            builder.Property(c => c.HomeTeam).IsRequired().HasMaxLength(128);
            builder.Property(c => c.AwayTeam).IsRequired().HasMaxLength(128);
            builder.Property(c => c.HomeScore).IsRequired();
            builder.Property(c => c.AwayScore).IsRequired();
            builder.Property(c => c.Broadcasters).HasMaxLength(256);
            builder.Property(c => c.Description).HasMaxLength(512);
            builder.Property(c => c.EventDate).IsRequired();
            builder.Property(c => c.EventType).IsRequired().HasConversion<int>();

        }
    }
}
