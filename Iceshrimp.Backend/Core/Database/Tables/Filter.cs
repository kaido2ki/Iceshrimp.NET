using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("filter")]
[Index("user_id")]
public class Filter
{
	[PgName("filter_action_enum")]
	public enum FilterAction
	{
		[PgName("warn")] Warn,
		[PgName("hide")] Hide
	}

	[PgName("filter_context_enum")]
	public enum FilterContext
	{
		[PgName("home")]          Home,
		[PgName("lists")]         Lists,
		[PgName("threads")]       Threads,
		[PgName("notifications")] Notifications,
		[PgName("accounts")]      Accounts,
		[PgName("public")]        Public
	}

	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	[Column("id")]
	public long Id { get; set; }

	[InverseProperty(nameof(User.Filters))]
	public User User { get; set; } = null!;

	[Column("name")]     public string              Name     { get; set; } = null!;
	[Column("expiry")]   public DateTime?           Expiry   { get; set; }
	[Column("keywords")] public List<string>        Keywords { get; set; } = [];
	[Column("contexts")] public List<FilterContext> Contexts { get; set; } = [];
	[Column("action")]   public FilterAction        Action   { get; set; }

	public Filter Clone(User? user = null)
	{
		return new Filter
		{
			Name     = Name,
			Action   = Action,
			Contexts = Contexts,
			Expiry   = Expiry,
			Keywords = Keywords,
			Id       = Id,
			User     = user!
		};
	}
}

public class FilterEntityTypeConfiguration : IEntityTypeConfiguration<Filter>
{
	public void Configure(EntityTypeBuilder<Filter> entity)
	{
		entity.HasOne(p => p.User)
		      .WithMany(p => p.Filters)
		      .OnDelete(DeleteBehavior.Cascade)
		      .HasForeignKey("user_id");

		entity.Property(p => p.Keywords).HasDefaultValueSql("'{}'::varchar[]");
		entity.Property(p => p.Contexts).HasDefaultValueSql("'{}'::public.filter_context_enum[]");
	}
}