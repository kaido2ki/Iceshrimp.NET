using System.Diagnostics.CodeAnalysis;

namespace Iceshrimp.Parsing;

public interface ISearchQueryFilter;

public record WordFilter(bool Negated, string Value) : ISearchQueryFilter;

public record CwFilter(bool Negated, string Value) : ISearchQueryFilter;

public record MultiWordFilter(bool Negated, string[] Values) : ISearchQueryFilter;

public record FromFilter(bool Negated, string Value) : ISearchQueryFilter;

public record MentionFilter(bool Negated, string Value) : ISearchQueryFilter;

public record ReplyFilter(bool Negated, string Value) : ISearchQueryFilter;

public record InstanceFilter(bool Negated, string Value) : ISearchQueryFilter;

public enum MiscFilterType
{
	Followers,
	Following,
	Replies,
	Renotes
}

public record MiscFilter(bool Negated, MiscFilterType Value) : ISearchQueryFilter
{
	public static bool TryParse(bool negated, ReadOnlySpan<char> value, [NotNullWhen(true)] out MiscFilter? result)
	{
		MiscFilterType? type = value switch
		{
			"followers" => MiscFilterType.Followers,
			"following" => MiscFilterType.Following,
			"replies"   => MiscFilterType.Replies,
			"reply"     => MiscFilterType.Replies,
			"renote"    => MiscFilterType.Renotes,
			"renotes"   => MiscFilterType.Renotes,
			"boosts"    => MiscFilterType.Renotes,
			"boost"     => MiscFilterType.Renotes,
			_           => null
		};

		if (!type.HasValue)
		{
			result = null;
			return false;
		}

		result = new MiscFilter(negated, type.Value);
		return true;
	}
}

public enum InFilterType
{
	Bookmarks,
	Likes,
	Reactions,
	Interactions
}

public record InFilter(bool Negated, InFilterType Value) : ISearchQueryFilter
{
	public static bool TryParse(bool negated, ReadOnlySpan<char> value, [NotNullWhen(true)] out InFilter? result)
	{
		InFilterType? type = value switch
		{
			"bookmarks"    => InFilterType.Bookmarks,
			"likes"        => InFilterType.Likes,
			"favorites"    => InFilterType.Likes,
			"favourites"   => InFilterType.Likes,
			"reactions"    => InFilterType.Reactions,
			"interactions" => InFilterType.Interactions,
			_              => null
		};

		if (!type.HasValue)
		{
			result = null;
			return false;
		}

		result = new InFilter(negated, type.Value);
		return true;
	}
}

public enum AttachmentFilterType
{
	Media,
	Image,
	Video,
	Audio,
	File,
	Poll
}

public record AttachmentFilter(bool Negated, AttachmentFilterType Value) : ISearchQueryFilter
{
	public static bool TryParse(
		bool negated, ReadOnlySpan<char> value, [NotNullWhen(true)] out AttachmentFilter? result
	)
	{
		AttachmentFilterType? type = value switch
		{
			"any"   => AttachmentFilterType.Media,
			"media" => AttachmentFilterType.Media,
			"image" => AttachmentFilterType.Image,
			"video" => AttachmentFilterType.Video,
			"audio" => AttachmentFilterType.Audio,
			"file"  => AttachmentFilterType.File,
			"poll"  => AttachmentFilterType.Poll,
			_       => null
		};

		if (!type.HasValue)
		{
			result = null;
			return false;
		}

		result = new AttachmentFilter(negated, type.Value);
		return true;
	}
}

public record AfterFilter(DateOnly Value) : ISearchQueryFilter;

public record BeforeFilter(DateOnly Value) : ISearchQueryFilter;

public enum CaseFilterType
{
	Sensitive,
	Insensitive
}

public record CaseFilter(CaseFilterType Value) : ISearchQueryFilter
{
	public static bool TryParse(ReadOnlySpan<char> value, [NotNullWhen(true)] out CaseFilter? result)
	{
		CaseFilterType? type = value switch
		{
			"sensitive"   => CaseFilterType.Sensitive,
			"insensitive" => CaseFilterType.Insensitive,
			_             => null
		};

		if (!type.HasValue)
		{
			result = null;
			return false;
		}

		result = new CaseFilter(type.Value);
		return true;
	}
}

public enum MatchFilterType
{
	Words,
	Substring
}

public record MatchFilter(MatchFilterType Value) : ISearchQueryFilter
{
	public static bool TryParse(ReadOnlySpan<char> value, [NotNullWhen(true)] out MatchFilter? result)
	{
		MatchFilterType? type = value switch
		{
			"words"     => MatchFilterType.Words,
			"word"      => MatchFilterType.Words,
			"substring" => MatchFilterType.Substring,
			"substr"    => MatchFilterType.Substring,
			_           => null
		};

		if (!type.HasValue)
		{
			result = null;
			return false;
		}

		result = new MatchFilter(type.Value);
		return true;
	}
}
