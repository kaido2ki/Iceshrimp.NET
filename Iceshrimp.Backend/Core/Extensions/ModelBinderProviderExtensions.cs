using System.Collections;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace Iceshrimp.Backend.Core.Extensions;

public static class ModelBinderProviderExtensions
{
	public static void AddHybridBindingProvider(this IList<IModelBinderProvider> providers)
	{
		if (providers.Single(provider => provider.GetType() == typeof(BodyModelBinderProvider)) is not
			    BodyModelBinderProvider bodyProvider ||
		    providers.Single(provider => provider.GetType() == typeof(ComplexObjectModelBinderProvider)) is not
			    ComplexObjectModelBinderProvider complexProvider)
			throw new Exception("Failed to set up hybrid model binding provider");

		if (providers.Single(provider => provider.GetType() == typeof(CollectionModelBinderProvider)) is not
		    CollectionModelBinderProvider collectionProvider)
			throw new Exception("Failed to set up query collection model binding provider");

		if (providers.Single(provider => provider.GetType() == typeof(DictionaryModelBinderProvider)) is not
		    DictionaryModelBinderProvider dictionaryProvider)
			throw new Exception("Failed to set up dictionary model binding provider");

		var hybridProvider           = new HybridModelBinderProvider(bodyProvider, complexProvider, dictionaryProvider);
		var customCollectionProvider = new CustomCollectionModelBinderProvider(collectionProvider);
		var intToBoolProvider        = new IntToBoolModelBinderProvider();

		providers.Insert(0, hybridProvider);
		providers.Insert(1, customCollectionProvider);
		providers.Insert(2, intToBoolProvider);
	}
}

public class HybridModelBinderProvider(
	IModelBinderProvider bodyProvider,
	IModelBinderProvider complexProvider,
	IModelBinderProvider dictionaryProvider
) : IModelBinderProvider
{
	public IModelBinder? GetBinder(ModelBinderProviderContext context)
	{
		if (context.BindingInfo.BindingSource == null) return null;
		if (!context.BindingInfo.BindingSource.CanAcceptDataFrom(HybridBindingSource.Hybrid)) return null;

		context.BindingInfo.BindingSource = BindingSource.Body;
		var bodyBinder = bodyProvider.GetBinder(context);
		context.BindingInfo.BindingSource = BindingSource.ModelBinding;
		var complexBinder = complexProvider.GetBinder(context);
		context.BindingInfo.BindingSource = BindingSource.Query;
		var dictionaryBinder = dictionaryProvider.GetBinder(context);

		return new HybridModelBinder(bodyBinder, complexBinder, dictionaryBinder);
	}
}

public class IntToBoolModelBinderProvider : IModelBinderProvider
{
	public IModelBinder? GetBinder(ModelBinderProviderContext context)
	{
		var isBool         = context.Metadata.ModelType.IsAssignableFrom(typeof(bool));
		var isNullableBool = context.Metadata.ModelType.IsAssignableFrom(typeof(bool?));
		if (!isBool && !isNullableBool) return null;

		return new IntToBoolModelBinder();
	}
}

public class IntToBoolModelBinder : IModelBinder
{
	public Task BindModelAsync(ModelBindingContext bindingContext)
	{
		var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
		var value               = valueProviderResult.FirstValue;
		var error               = $"{bindingContext.ModelName} must be one of: [true, false, 1, 0].";

		if (bool.TryParse(value, out var boolValue))
		{
			bindingContext.Result = ModelBindingResult.Success(boolValue);
		}
		else if (int.TryParse(value, out var intValue))
		{
			if (intValue is 0 or 1) bindingContext.Result = ModelBindingResult.Success(intValue != 0);
			else bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, error);
		}
		else if (bindingContext.ModelMetadata.IsNullableValueType && value != null && string.IsNullOrWhiteSpace(value))
		{
			bindingContext.Result = ModelBindingResult.Success(null);
		}

		return Task.CompletedTask;
	}
}

public class CustomCollectionModelBinderProvider(IModelBinderProvider provider) : IModelBinderProvider
{
	public IModelBinder? GetBinder(ModelBinderProviderContext context)
	{
		if (!context.Metadata.IsCollectionType) return null;

		var binder = provider.GetBinder(context);
		return new CustomCollectionModelBinder(binder);
	}
}

public class HybridModelBinder(
	IModelBinder? bodyBinder,
	IModelBinder? complexBinder,
	IModelBinder? dictionaryBinder
) : IModelBinder
{
	public async Task BindModelAsync(ModelBindingContext bindingContext)
	{
		if (bindingContext.IsTopLevelObject)
		{
			if (bindingContext is { HttpContext.Request.HasFormContentType: false } &&
			    bindingContext.HttpContext.Request.HasBody())
			{
				if (bodyBinder != null)
				{
					bindingContext.BindingSource = BindingSource.Body;
					await bodyBinder.BindModelAsync(bindingContext);
				}
			}
			else if (!bindingContext.HttpContext.Request.HasBody() && dictionaryBinder != null)
			{
				bindingContext.BindingSource = BindingSource.Query;
				await dictionaryBinder.BindModelAsync(bindingContext);
			}
		}

		if (complexBinder != null && !bindingContext.Result.IsModelSet)
		{
			bindingContext.BindingSource = BindingSource.ModelBinding;
			await complexBinder.BindModelAsync(bindingContext);
		}

		if (bindingContext.Result.IsModelSet) bindingContext.Model = bindingContext.Result.Model;
	}
}

public class CustomCollectionModelBinder(IModelBinder? binder) : IModelBinder
{
	public async Task BindModelAsync(ModelBindingContext bindingContext)
	{
		if (binder != null && !bindingContext.Result.IsModelSet)
		{
			await binder.BindModelAsync(bindingContext);

			if (!bindingContext.Result.IsModelSet || (bindingContext.Result.Model as IList) is not { Count: > 0 })
			{
				var pre = bindingContext.ModelName;
				bindingContext.ModelName = bindingContext.ModelName.EndsWith("[]")
					? bindingContext.ModelName[..^2]
					: bindingContext.ModelName + "[]";

				await binder.BindModelAsync(bindingContext);

				if (bindingContext.Result.IsModelSet &&
				    bindingContext.ModelState.TryGetValue(bindingContext.ModelName, out var state))
				{
					bindingContext.ModelState.SetModelValue(pre, state.RawValue, state.AttemptedValue);
					bindingContext.ModelState.Remove(bindingContext.ModelName);
				}

				bindingContext.ModelName       = pre;
				bindingContext.BinderModelName = pre;
			}
		}

		if (bindingContext.Result.IsModelSet)
		{
			bindingContext.Model         = bindingContext.Result.Model;
			bindingContext.BindingSource = BindingSource.ModelBinding;
		}
	}
}

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public class FromHybridAttribute(string? name = null) : Attribute, IBindingSourceMetadata, IModelNameProvider
{
	public BindingSource BindingSource => HybridBindingSource.Hybrid;
	public string?       Name          => name;
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ConsumesHybridAttribute : Attribute;

public sealed class HybridBindingSource() : BindingSource("Hybrid", "Hybrid", true, true)
{
	public static readonly HybridBindingSource Hybrid = new();

	public override bool CanAcceptDataFrom(BindingSource bindingSource)
	{
		return bindingSource == this;
	}
}