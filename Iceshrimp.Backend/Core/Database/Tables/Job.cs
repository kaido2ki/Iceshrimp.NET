﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EntityFrameworkCore.Projectables;
using Iceshrimp.Backend.Core.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("jobs")]
[Index(nameof(Queue))]
[Index(nameof(Status))]
[Index(nameof(FinishedAt))]
[Index(nameof(DelayedUntil))]
[Index(nameof(Mutex), IsUnique = true)]
public class Job
{
	public enum JobStatus
	{
		Queued,
		Delayed,
		Running,
		Completed,
		Failed
	}

	[Key] [Column("id")] public Guid Id { get; set; }

	[Column("queue")]             public string    Queue            { get; set; } = null!;
	[Column("status")]            public JobStatus Status           { get; set; }
	[Column("queued_at")]         public DateTime  QueuedAt         { get; set; }
	[Column("started_at")]        public DateTime? StartedAt        { get; set; }
	[Column("finished_at")]       public DateTime? FinishedAt       { get; set; }
	[Column("delayed_until")]     public DateTime? DelayedUntil     { get; set; }
	[Column("retry_count")]       public int       RetryCount       { get; set; }
	[Column("exception_message")] public string?   ExceptionMessage { get; set; }
	[Column("exception_source")]  public string?   ExceptionSource  { get; set; }
	[Column("stack_trace")]       public string?   StackTrace       { get; set; }
	[Column("exception")]         public string?   Exception        { get; set; }
	[Column("data")]              public string    Data             { get; set; } = null!;
	[Column("mutex")]             public string?   Mutex            { get; set; }

	[NotMapped]
	public long Duration => ((FinishedAt ?? DateTime.UtcNow) - (StartedAt ?? QueuedAt)).GetTotalMilliseconds();

	[NotMapped] public long QueueDuration => ((StartedAt ?? DateTime.UtcNow) - QueuedAt).GetTotalMilliseconds();

	[NotMapped] [Projectable] public DateTime LastUpdatedAt => FinishedAt ?? StartedAt ?? QueuedAt;

	private class EntityTypeConfiguration : IEntityTypeConfiguration<Job>
	{
		public void Configure(EntityTypeBuilder<Job> entity)
		{
			entity.Property(e => e.Id).ValueGeneratedNever();
			entity.Property(e => e.Status).HasDefaultValue(JobStatus.Queued);
			entity.Property(e => e.QueuedAt).HasDefaultValueSql("now()");
		}
	}
}