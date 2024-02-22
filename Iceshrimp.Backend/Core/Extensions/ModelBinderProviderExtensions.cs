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

		var hybridProvider           = new HybridModelBinderProvider(bodyProvider, complexProvider);
		var customCollectionProvider = new CustomCollectionModelBinderProvider(collectionProvider);

		providers.Insert(0, hybridProvider);
		providers.Insert(1, customCollectionProvider);
	}
}

//TODO: this doesn't work with QueryCollectionModelBinderProvider yet
public class HybridModelBinderProvider(
	IModelBinderProvider bodyProvider,
	IModelBinderProvider complexProvider
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

		return new HybridModelBinder(bodyBinder, complexBinder);
	}
}

public class CustomCollectionModelBinderProvider(IModelBinderProvider provider) : IModelBinderProvider
{
	public IModelBinder? GetBinder(ModelBinderProviderContext context)
	{
		if (context.BindingInfo.BindingSource == null) return null;
		if (!context.BindingInfo.BindingSource.CanAcceptDataFrom(BindingSource.Query) &&
		    !context.BindingInfo.BindingSource.CanAcceptDataFrom(BindingSource.Form) &&
		    !context.BindingInfo.BindingSource.CanAcceptDataFrom(BindingSource.ModelBinding)) return null;
		if (!context.Metadata.IsCollectionType) return null;

		var binder = provider.GetBinder(context);
		return new CustomCollectionModelBinder(binder);
	}
}

public class HybridModelBinder(IModelBinder? bodyBinder, IModelBinder? complexBinder) : IModelBinder
{
	public async Task BindModelAsync(ModelBindingContext bindingContext)
	{
		if (bodyBinder != null &&
		    bindingContext is
		    {
			    IsTopLevelObject: true, HttpContext.Request: { HasFormContentType: false, ContentLength: > 0 }
		    })
		{
			bindingContext.BindingSource = BindingSource.Body;
			await bodyBinder.BindModelAsync(bindingContext);
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
				bindingContext.ModelName = bindingContext.ModelName.EndsWith("[]")
					? bindingContext.ModelName[..^2]
					: bindingContext.ModelName + "[]";

				await binder.BindModelAsync(bindingContext);
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
	public string?       Name          => name;
	public BindingSource BindingSource => HybridBindingSource.Hybrid;
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