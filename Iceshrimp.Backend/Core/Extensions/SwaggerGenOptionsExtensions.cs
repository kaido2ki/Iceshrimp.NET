using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Iceshrimp.Backend.Controllers.Mastodon.Attributes;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Iceshrimp.Backend.Core.Extensions;

public static class SwaggerGenOptionsExtensions {
	public static void AddOperationFilters(this SwaggerGenOptions options) {
		options.OperationFilter<AuthorizeCheckOperationFilter>();
		options.OperationFilter<HybridRequestOperationFilter>();
		options.OperationFilter<MastodonApiControllerOperationFilter>();
	}

	[SuppressMessage("ReSharper", "ClassNeverInstantiated.Local",
	                 Justification = "SwaggerGenOptions.OperationFilter<T> instantiates this class at runtime")]
	private class MastodonApiControllerOperationFilter : IOperationFilter {
		public void Apply(OpenApiOperation operation, OperationFilterContext context) {
			if (context.MethodInfo.DeclaringType is null)
				return;

			var isMastodonController = context.MethodInfo.DeclaringType.GetCustomAttributes(true)
			                                  .OfType<MastodonApiControllerAttribute>().Any();

			if (!isMastodonController) return;

			operation.Tags = [new OpenApiTag { Name = "Mastodon" }];
		}
	}

	[SuppressMessage("ReSharper", "ClassNeverInstantiated.Local",
	                 Justification = "SwaggerGenOptions.OperationFilter<T> instantiates this class at runtime")]
	private class AuthorizeCheckOperationFilter : IOperationFilter {
		public void Apply(OpenApiOperation operation, OperationFilterContext context) {
			if (context.MethodInfo.DeclaringType is null)
				return;

			//TODO: separate admin & user authorize attributes
			var hasAuthenticate = context.MethodInfo.DeclaringType.GetCustomAttributes(true)
			                             .OfType<AuthenticateAttribute>().Any() ||
			                      context.MethodInfo.GetCustomAttributes(true)
			                             .OfType<AuthenticateAttribute>().Any();

			var isMastodonController = context.MethodInfo.DeclaringType.GetCustomAttributes(true)
			                                  .OfType<MastodonApiControllerAttribute>().Any();

			if (!hasAuthenticate) return;

			var schema = new OpenApiSecurityScheme {
				Reference = new OpenApiReference {
					Type = ReferenceType.SecurityScheme,
					Id   = isMastodonController ? "mastodon" : "user"
				}
			};

			operation.Security = new List<OpenApiSecurityRequirement> {
				new() {
					[schema] = Array.Empty<string>()
				}
			};
		}
	}

	[SuppressMessage("ReSharper", "ClassNeverInstantiated.Local",
	                 Justification = "SwaggerGenOptions.OperationFilter<T> instantiates this class at runtime")]
	private class HybridRequestOperationFilter : IOperationFilter {
		public void Apply(OpenApiOperation operation, OperationFilterContext context) {
			if (context.MethodInfo.DeclaringType is null)
				return;

			var consumesHybrid = context.MethodInfo.DeclaringType.GetCustomAttributes(true)
			                            .OfType<ConsumesHybridAttribute>().Any() ||
			                     context.MethodInfo.GetCustomAttributes(true)
			                            .OfType<ConsumesHybridAttribute>().Any();

			if (!consumesHybrid) return;

			operation.RequestBody =
				GenerateRequestBody(context.ApiDescription, context.SchemaRepository, context.SchemaGenerator);
			operation.Parameters.Clear();
		}

		private static OpenApiRequestBody? GenerateRequestBody(
			ApiDescription apiDescription,
			SchemaRepository schemaRepository,
			ISchemaGenerator schemaGenerator
		) {
			OpenApiRequestBody? requestBody = null;

			var hybridParameter = apiDescription.ParameterDescriptions.FirstOrDefault(paramDesc =>
						 paramDesc.Source == HybridBindingSource.Hybrid);

			if (hybridParameter != null)
				requestBody =
					GenerateRequestBodyFromHybridParameter(schemaRepository, schemaGenerator, hybridParameter);

			return requestBody;
		}

		private static OpenApiRequestBody GenerateRequestBodyFromHybridParameter(
			SchemaRepository schemaRepository,
			ISchemaGenerator schemaGenerator,
			ApiParameterDescription bodyParameter
		) {
			List<string> contentTypes =
				["application/json", "application/x-www-form-urlencoded", "multipart/form-data"];

			var isRequired = bodyParameter.IsRequiredParameter();

			var schema = GenerateSchema(bodyParameter.ModelMetadata.ModelType,
			                            schemaRepository,
			                            schemaGenerator,
			                            bodyParameter.PropertyInfo(),
			                            bodyParameter.ParameterInfo());

			return new OpenApiRequestBody {
				Content = contentTypes
					.ToDictionary(contentType => contentType, _ => new OpenApiMediaType { Schema = schema }),
				Required = isRequired
			};
		}

		private static OpenApiSchema GenerateSchema(
			Type type,
			SchemaRepository schemaRepository,
			ISchemaGenerator schemaGenerator,
			MemberInfo? propertyInfo = null,
			ParameterInfo? parameterInfo = null,
			ApiParameterRouteInfo? routeInfo = null
		) {
			try {
				return schemaGenerator.GenerateSchema(type, schemaRepository, propertyInfo, parameterInfo, routeInfo);
			}
			catch (Exception ex) {
				throw new
					SwaggerGeneratorException($"Failed to generate schema for type - {type}. See inner exception",
					                          ex);
			}
		}
	}
}