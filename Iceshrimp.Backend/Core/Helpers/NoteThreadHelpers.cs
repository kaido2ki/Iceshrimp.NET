using System.Linq.Expressions;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Shared.Schemas;

namespace Iceshrimp.Backend.Core.Helpers;

public static class NoteThreadHelpers
{
	public class TreeNode<T>(T self)
	{
		public readonly T                 Self        = self;
		public          List<TreeNode<T>> Descendants = [];
		public          TreeNode<T>?      Parent;
	}

	public static List<NoteResponse> OrderDescendants(this List<NoteResponse> notes)
	{
		foreach (var note in notes)
		{
			var parent = notes.FirstOrDefault(p => p.Id == note.ReplyId);
			if (parent == null) continue;
			parent.Descendants ??= [];
			parent.Descendants.Add(note);
			note.Parent = parent;
		}

		foreach (var note in notes.Where(p => p.Descendants?.Count > 0))
		{
			note.Descendants = note.Descendants?
			                       .OrderBy(p => p.Id)
			                       .ToList()
			                       .PromoteBy(p => p.Reply != null && p.Reply.User.Id == p.User.Id);
		}

		notes.RemoveAll(p => p.Parent != null);
		return notes;
	}

	public static List<StatusEntity> OrderDescendants(this List<StatusEntity> notes)
	{
		var nodes = notes.Select(p => new TreeNode<StatusEntity>(p))
		                 .OrderBy(p => p.Self.Id)
		                 .ToList();

		foreach (var node in nodes)
		{
			var parent = nodes.FirstOrDefault(p => p.Self.Id == node.Self.ReplyId);
			if (parent == null) continue;
			parent.Descendants.Add(node);
			node.Parent = parent;
			if (parent.Self.Account.Id == node.Self.Account.Id)
				node.Self.ReplyUserId = node.Self.MastoReplyUserId ?? parent.Self.ReplyUserId ?? parent.Self.Account.Id;
		}

		foreach (var note in nodes.Where(p => p.Descendants.Count > 0))
		{
			note.Descendants = note.Descendants
			                       .OrderBy(p => p.Self.Id)
			                       .ToList()
			                       .PromoteBy(p => p.Self.ReplyUserId == p.Self.Account.Id);
		}

		nodes.RemoveAll(p => p.Parent != null);
		nodes.PromoteBy(p => p.Self.ReplyUserId == p.Self.Account.Id);
		List<StatusEntity> res = [];

		foreach (var node in nodes)
			Walk(node);

		return res;

		void Walk(TreeNode<StatusEntity> node)
		{
			res.Add(node.Self);
			foreach (var descendant in node.Descendants)
			{
				Walk(descendant);
			}
		}
	}

	private static List<T> PromoteBy<T>(this List<T> nodes, Expression<Func<T, bool>> predicate)
	{
		var compiled = predicate.Compile();
		var match    = nodes.FirstOrDefault(compiled);
		if (match == null) return nodes;

		var insertAt = 0;
		for (var index = 0; index < nodes.Count; index++)
		{
			var note = nodes[index];
			if (index <= insertAt || !compiled.Invoke(note))
				continue;
			nodes.RemoveAt(index);
			nodes.Insert(insertAt++, note);
		}

		return nodes;
	}
}