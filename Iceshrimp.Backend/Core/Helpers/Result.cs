using System.Diagnostics.CodeAnalysis;

namespace Iceshrimp.Backend.Core.Helpers;

public abstract record Result<TResult, TError>
	where TResult : class
	where TError : class
{
	private Result() { }

	public sealed record Success(TResult Result) : Result<TResult, TError>;

	public sealed record Failure(TError Error) : Result<TResult, TError>;

	public static implicit operator Result<TResult, TError>(TResult result) => new Success(result);
	public static implicit operator Result<TResult, TError>(TError error)   => new Failure(error);

	public bool TryGetResult([NotNullWhen(true)] out TResult? result)
	{
		if (this is Success s)
		{
			result = s.Result;
			return true;
		}

		result = null;
		return false;
	}

	public bool TryGetError([NotNullWhen(true)] out TError? error)
	{
		if (this is Failure f)
		{
			error = f.Error;
			return true;
		}

		error = null;
		return false;
	}

	public bool IsSuccess => this is Success;
	public bool IsFailure => this is Failure;
}

public abstract record Result<TResult> where TResult : class
{
	private Result() { }

	public sealed record Success(TResult Result) : Result<TResult>;

	public sealed record Failure(Exception Error) : Result<TResult>;

	public static implicit operator Result<TResult>(TResult result)  => new Success(result);
	public static implicit operator Result<TResult>(Exception error) => new Failure(error);

	public bool TryGetResult([NotNullWhen(true)] out TResult? result)
	{
		if (this is Success s)
		{
			result = s.Result;
			return true;
		}

		result = null;
		return false;
	}

	public bool TryGetError([NotNullWhen(true)] out Exception? error)
	{
		if (this is Failure f)
		{
			error = f.Error;
			return true;
		}

		error = null;
		return false;
	}

	public bool IsSuccess => this is Success;
	public bool IsFailure => this is Failure;
}