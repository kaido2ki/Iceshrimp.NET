using System.Buffers;
using System.Net;
using System.Text.Encodings.Web;
using System.Xml;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Iceshrimp.Backend.Core.Extensions;

public static class MvcBuilderExtensions
{
	public static IMvcBuilder AddMultiFormatter(this IMvcBuilder builder)
	{
		builder.Services.AddOptions<MvcOptions>()
		       .PostConfigure<IOptions<JsonOptions>, IOptions<MvcNewtonsoftJsonOptions>, ArrayPool<char>,
			       ObjectPoolProvider, ILoggerFactory>((opts, jsonOpts, _, _, _, loggerFactory) =>
		       {
			       // We need to re-add these one since .AddNewtonsoftJson() removes them
			       if (!opts.InputFormatters.OfType<SystemTextJsonInputFormatter>().Any())
			       {
				       var systemInputLogger = loggerFactory.CreateLogger<SystemTextJsonInputFormatter>();
				       // We need to set this, otherwise characters like '+' will be escaped in responses
				       jsonOpts.Value.JsonSerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
				       opts.InputFormatters.Add(new SystemTextJsonInputFormatter(jsonOpts.Value, systemInputLogger));
			       }

			       if (!opts.OutputFormatters.OfType<SystemTextJsonOutputFormatter>().Any())
				       opts.OutputFormatters.Add(new SystemTextJsonOutputFormatter(jsonOpts.Value
					                                 .JsonSerializerOptions));

			       opts.InputFormatters.Insert(0, new JsonInputMultiFormatter());
			       opts.OutputFormatters.Insert(0, new JsonOutputMultiFormatter());
			       opts.OutputFormatters.Add(new XmlSerializerOutputFormatter());
		       });

		return builder;
	}

	public static IMvcBuilder ConfigureNewtonsoftJson(this IMvcBuilder builder)
	{
		JsonConvert.DefaultSettings = () => new JsonSerializerSettings
		{
			DateTimeZoneHandling = DateTimeZoneHandling.Utc
		};

		return builder;
	}

	public static IMvcBuilder AddModelBindingProviders(this IMvcBuilder builder)
	{
		builder.Services.AddOptions<MvcOptions>()
		       .PostConfigure(options => { options.ModelBinderProviders.AddHybridBindingProvider(); });

		return builder;
	}

	public static IMvcBuilder AddValueProviderFactories(this IMvcBuilder builder)
	{
		builder.Services.AddOptions<MvcOptions>()
		       .PostConfigure(options =>
		       {
			       options.ValueProviderFactories.Add(new JQueryQueryStringValueProviderFactory());
		       });

		return builder;
	}

	public static IMvcBuilder AddApiBehaviorOptions(this IMvcBuilder builder)
	{
		builder.ConfigureApiBehaviorOptions(o =>
		{
			o.InvalidModelStateResponseFactory = actionContext =>
			{
				var details = new ValidationProblemDetails(actionContext.ModelState);

				var status  = (HttpStatusCode?)details.Status ?? HttpStatusCode.BadRequest;
				var message = details.Title ?? "One or more validation errors occurred.";
				if (details.Detail != null)
					message += $" - {details.Detail}";

				throw new ValidationException(status, status.ToString(), message, details.Errors);
			};
		});

		return builder;
	}
}