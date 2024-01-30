using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace Iceshrimp.Backend.Core.Extensions;

public static class ModelBinderProviderExtensions {
	public static void AddHybridBindingProvider(this IList<IModelBinderProvider> providers) {
		if (providers.Single(provider => provider.GetType() == typeof(BodyModelBinderProvider)) is not
			    BodyModelBinderProvider bodyProvider ||
		    providers.Single(provider => provider.GetType() == typeof(ComplexObjectModelBinderProvider)) is not
			    ComplexObjectModelBinderProvider complexProvider)
			throw new Exception("Failed to set up hybrid model binding provider");

		var hybridProvider = new HybridModelBinderProvider(bodyProvider, complexProvider);

		providers.Insert(0, hybridProvider);
	}
}

public class HybridModelBinderProvider(
	IModelBinderProvider bodyProvider,
	IModelBinderProvider complexProvider) : IModelBinderProvider {
	public IModelBinder? GetBinder(ModelBinderProviderContext context) {
		if (context.BindingInfo.BindingSource == null) return null;
		if (!context.BindingInfo.BindingSource.CanAcceptDataFrom(HybridBindingSource.Hybrid)) return null;

		context.BindingInfo.BindingSource = BindingSource.Body;
		var bodyBinder = bodyProvider.GetBinder(context);
		context.BindingInfo.BindingSource = BindingSource.ModelBinding;
		var complexBinder = complexProvider.GetBinder(context);

		return new HybridModelBinder(bodyBinder, complexBinder);
	}
}

public class HybridModelBinder(
	IModelBinder? bodyBinder,
	IModelBinder? complexBinder
) : IModelBinder {
	public async Task BindModelAsync(ModelBindingContext bindingContext) {
		if (bodyBinder != null && bindingContext is
			    { IsTopLevelObject: true, HttpContext.Request: { HasFormContentType: false, ContentLength: > 0 } }) {
			bindingContext.BindingSource = BindingSource.Body;
			await bodyBinder.BindModelAsync(bindingContext);
		}

		if (complexBinder != null && !bindingContext.Result.IsModelSet) {
			bindingContext.BindingSource = BindingSource.ModelBinding;
			await complexBinder.BindModelAsync(bindingContext);
		}

		if (bindingContext.Result.IsModelSet) bindingContext.Model = bindingContext.Result.Model;
	}
}

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public class FromHybridAttribute : Attribute, IBindingSourceMetadata {
	public BindingSource BindingSource => HybridBindingSource.Hybrid;
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ConsumesHybridAttribute : Attribute;

public sealed class HybridBindingSource() : BindingSource("Hybrid", "Hybrid", true, true) {
	public static readonly HybridBindingSource Hybrid = new();

	public override bool CanAcceptDataFrom(BindingSource bindingSource) {
		return bindingSource == this;
	}
}