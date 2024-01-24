using System.Buffers;
using Iceshrimp.Backend.Controllers.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Extensions;

public static class MvcBuilderExtensions {
	public static IMvcBuilder AddMultiFormatter(this IMvcBuilder builder) {
		builder.Services.AddOptions<MvcOptions>()
		       .PostConfigure<IOptions<JsonOptions>, IOptions<MvcNewtonsoftJsonOptions>, ArrayPool<char>,
			       ObjectPoolProvider, ILoggerFactory>((opts, jsonOpts, _, _, _, loggerFactory) => {
			       // We need to re-add these one since .AddNewtonsoftJson() removes them
			       if (!opts.InputFormatters.OfType<SystemTextJsonInputFormatter>().Any()) {
				       var systemInputLogger = loggerFactory.CreateLogger<SystemTextJsonInputFormatter>();
				       opts.InputFormatters.Add(new SystemTextJsonInputFormatter(jsonOpts.Value, systemInputLogger));
			       }

			       if (!opts.OutputFormatters.OfType<SystemTextJsonOutputFormatter>().Any())
				       opts.OutputFormatters.Add(new SystemTextJsonOutputFormatter(jsonOpts.Value
					                                 .JsonSerializerOptions));

			       opts.InputFormatters.Insert(0, new JsonInputMultiFormatter());
			       opts.OutputFormatters.Insert(0, new JsonOutputMultiFormatter());
		       });

		return builder;
	}
}