using Microsoft.EntityFrameworkCore;
using RevenueMetrics.Domain.Entities;

namespace RevenueMetrics.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
	public AppDbContext(DbContextOptions<AppDbContext> options)
		: base(options)
	{
	}

	public DbSet<Transaction> Transactions => Set<Transaction>();
	public DbSet<SyncState> SyncStates => Set<SyncState>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<Transaction>(entity =>
		{
			entity.ToTable("transactions");

			entity.HasKey(x => x.Id);

			entity.Property(x => x.Amount)
				.HasPrecision(18, 2);

			entity.Property(x => x.Source)
				.HasMaxLength(50)
				.IsRequired();

			entity.Property(x => x.SourceTransactionId)
				.HasMaxLength(200)
				.IsRequired();

			entity.Property(x => x.Currency)
				.HasMaxLength(10)
				.IsRequired();

			entity.Property(x => x.SourceStatus)
				.HasMaxLength(100)
				.IsRequired();

			entity.Property(x => x.CanonicalStatus)
				.HasMaxLength(50)
				.IsRequired();

			entity.Property(x => x.RawPayload)
				.HasColumnType("jsonb");

			entity.HasIndex(x => new
			{
				x.Source,
				x.SourceTransactionId
			})
			.IsUnique();
		});

		modelBuilder.Entity<SyncState>(entity =>
		{
			entity.ToTable("sync_states");

			entity.HasKey(x => x.Id);

			entity.Property(x => x.SourceName)
				.HasMaxLength(50)
				.IsRequired();

			entity.HasIndex(x => x.SourceName)
				.IsUnique();
		});
	}
}
