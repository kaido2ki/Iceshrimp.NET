using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("jobs")]
[Index("Queue")]
[Index("Status")]
[Index("FinishedAt")]
[Index("DelayedUntil")]
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
	[Column("data")]              public string    Data             { get; set; } = null!;

	[Column("worker_id")]
	[StringLength(64)]
	public string? WorkerId { get; set; }

	[NotMapped]
	public long Duration => (long)((FinishedAt ?? DateTime.UtcNow) - (StartedAt ?? QueuedAt)).TotalMilliseconds;

	[NotMapped] public long QueueDuration => (long)((StartedAt ?? DateTime.UtcNow) - QueuedAt).TotalMilliseconds;
}