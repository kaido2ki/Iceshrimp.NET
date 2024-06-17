using System.Linq.Expressions;

namespace Iceshrimp.Backend.Core.Extensions;

public static class ExpressionExtensions
{
	public static Expression<Func<T, bool>> True<T>()  => f => true;
	public static Expression<Func<T, bool>> False<T>() => f => false;

	public static Expression<Func<T, bool>> Or<T>(
		this Expression<Func<T, bool>> expr1, Expression<Func<T, bool>> expr2
	)
	{
		var invokedExpr = Expression.Invoke(expr2, expr1.Parameters);
		return Expression.Lambda<Func<T, bool>>(Expression.OrElse(expr1.Body, invokedExpr), expr1.Parameters);
	}

	public static Expression<Func<T, bool>> And<T>(
		this Expression<Func<T, bool>> expr1, Expression<Func<T, bool>> expr2
	)
	{
		var invokedExpr = Expression.Invoke(expr2, expr1.Parameters);
		return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(expr1.Body, invokedExpr), expr1.Parameters);
	}

	public static Expression<Func<TFirstParam, TResult>> Compose<TFirstParam, TIntermediate, TResult>(
		this Expression<Func<TFirstParam, TIntermediate>> first,
		Expression<Func<TIntermediate, TResult>> second
	)
	{
		var param = Expression.Parameter(typeof(TFirstParam), "param");

		var newFirst  = first.Body.Replace(first.Parameters[0], param);
		var newSecond = second.Body.Replace(second.Parameters[0], newFirst);

		return Expression.Lambda<Func<TFirstParam, TResult>>(newSecond, param);
	}

	private static Expression Replace(this Expression expression, Expression searchEx, Expression replaceEx)
	{
		return new ReplaceVisitor(searchEx, replaceEx).Visit(expression) ?? throw new NullReferenceException();
	}

	private class ReplaceVisitor(Expression from, Expression to) : ExpressionVisitor
	{
		public override Expression? Visit(Expression? node)
		{
			return node == from ? to : base.Visit(node);
		}
	}
}