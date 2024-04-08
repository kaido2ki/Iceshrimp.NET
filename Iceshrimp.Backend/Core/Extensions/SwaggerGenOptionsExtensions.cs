using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Iceshrimp.Backend.Controllers.Federation.Attributes;
using Iceshrimp.Backend.Controllers.Mastodon.Attributes;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Iceshrimp.Backend.Core.Extensions;

public static class SwaggerGenOptionsExtensions
{
	public static void AddFilters(this SwaggerGenOptions options)
	{
		options.SchemaFilter<RequireNonNullablePropertiesSchemaFilter>();
		options.OperationFilter<AuthorizeCheckOperationFilter>();
		options.OperationFilter<HybridRequestOperationFilter>();
		options.DocInclusionPredicate(DocInclusionPredicate);
	}

	[SuppressMessage("ReSharper", "ClassNeverInstantiated.Local",
	                 Justification = "SwaggerGenOptions.SchemaFilter<T> instantiates this class at runtime")]
	private class RequireNonNullablePropertiesSchemaFilter : ISchemaFilter
	{
		public void Apply(OpenApiSchema model, SchemaFilterContext context)
		{
			var additionalRequiredProps = model.Properties
			                                   .Where(x => !x.Value.Nullable && !model.Required.Contains(x.Key))
			                                   .Select(x => x.Key);
			foreach (var propKey in additionalRequiredProps)
			{
				model.Required.Add(propKey);
			}
		}
	}

	private static bool DocInclusionPredicate(string docName, ApiDescription apiDesc)
	{
		if (!apiDesc.TryGetMethodInfo(out var methodInfo)) return false;
		if (methodInfo.DeclaringType is null) return false;

		var isMastodonController = methodInfo.DeclaringType.GetCustomAttributes(true)
		                                     .OfType<MastodonApiControllerAttribute>()
		                                     .Any();

		var isFederationController = methodInfo.DeclaringType.GetCustomAttributes(true)
		                                       .OfType<FederationApiControllerAttribute>()
		                                       .Any();

		return docName switch
		{
			"mastodon" when isMastodonController                              => true,
			"federation" when isFederationController                          => true,
			"iceshrimp" when !isMastodonController && !isFederationController => true,
			_                                                                 => false
		};
	}

	[SuppressMessage("ReSharper", "ClassNeverInstantiated.Local",
	                 Justification = "SwaggerGenOptions.OperationFilter<T> instantiates this class at runtime")]
	private class AuthorizeCheckOperationFilter : IOperationFilter
	{
		public void Apply(OpenApiOperation operation, OperationFilterContext context)
		{
			if (context.MethodInfo.DeclaringType is null)
				return;

			var authenticateAttribute = context.MethodInfo.GetCustomAttributes(true)
			                                   .OfType<AuthenticateAttribute>()
			                                   .FirstOrDefault() ??
			                            context.MethodInfo.DeclaringType.GetCustomAttributes(true)
			                                   .OfType<AuthenticateAttribute>()
			                                   .FirstOrDefault();

			if (authenticateAttribute == null) return;

			var isMastodonController = context.MethodInfo.DeclaringType.GetCustomAttributes(true)
			                                  .OfType<MastodonApiControllerAttribute>()
			                                  .Any();

			var authorizeAttribute = context.MethodInfo.GetCustomAttributes(true)
			                                .OfType<AuthorizeAttribute>()
			                                .FirstOrDefault() ??
			                         context.MethodInfo.DeclaringType.GetCustomAttributes(true)
			                                .OfType<AuthorizeAttribute>()
			                                .FirstOrDefault();

			var schema = new OpenApiSecurityScheme
			{
				Reference = new OpenApiReference
				{
					Type = ReferenceType.SecurityScheme, Id = isMastodonController ? "mastodon" : "iceshrimp"
				}
			};

			operation.Security = new List<OpenApiSecurityRequirement> { new() { [schema] = Array.Empty<string>() } };

			if (authorizeAttribute == null) return;

			const string web401 =
				"""
				{
				  "statusCode": 401,
				  "error": "Unauthorized",
				  "message": "This method requires an authenticated user"
				}
				""";

			const string web403 =
				"""
				{
				  "statusCode": 403,
				  "error": "Forbidden",
				  "message": "This action is outside the authorized scopes"
				}
				""";

			const string masto401 =
				"""
				{
				  "error": "This method requires an authenticated user"
				}
				""";

			const string masto403 =
				"""
				{
				  "message": "This action is outside the authorized scopes"
				}
				""";

			var example401 = new OpenApiString(isMastodonController ? masto401 : web401);

			var res401 = new OpenApiResponse
			{
				Description = "Unauthorized",
				Content = new Dictionary<string, OpenApiMediaType>
				{
					{ "application/json", new OpenApiMediaType { Example = example401 } }
				}
			};

			operation.Responses.Remove("401");
			operation.Responses.Add("401", res401);

			if (authorizeAttribute is { AdminRole: false, ModeratorRole: false, Scopes.Length: 0 } &&
			    authenticateAttribute is { AdminRole: false, ModeratorRole: false, Scopes.Length: 0 })
				return;

			operation.Responses.Remove("403");

			var example403 = new OpenApiString(isMastodonController ? masto403 : web403);

			var res403 = new OpenApiResponse
			{
				Description = "Forbidden",
				Content = new Dictionary<string, OpenApiMediaType>
				{
					{ "application/json", new OpenApiMediaType { Example = example403 } }
				}
			};
			operation.Responses.Add("403", res403);
		}
	}

	[SuppressMessage("ReSharper", "ClassNeverInstantiated.Local",
	                 Justification = "SwaggerGenOptions.OperationFilter<T> instantiates this class at runtime")]
	private class HybridRequestOperationFilter : IOperationFilter
	{
		public void Apply(OpenApiOperation operation, OperationFilterContext context)
		{
			if (context.MethodInfo.DeclaringType is null)
				return;

			var consumesHybrid = context.MethodInfo.DeclaringType.GetCustomAttributes(true)
			                            .OfType<ConsumesHybridAttribute>()
			                            .Any() ||
			                     context.MethodInfo.GetCustomAttributes(true)
			                            .OfType<ConsumesHybridAttribute>()
			                            .Any();

			if (!consumesHybrid) return;

			operation.RequestBody =
				GenerateRequestBody(context.ApiDescription, context.SchemaRepository, context.SchemaGenerator);
			operation.Parameters.Clear();
		}

		private static OpenApiRequestBody? GenerateRequestBody(
			ApiDescription apiDescription,
			SchemaRepository schemaRepository,
			ISchemaGenerator schemaGenerator
		)
		{
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
		)
		{
			List<string> contentTypes =
			[
				"application/json", "application/x-www-form-urlencoded", "multipart/form-data"
			];

			var isRequired = bodyParameter.IsRequiredParameter();

			var schema = GenerateSchema(bodyParameter.ModelMetadata.ModelType,
			                            schemaRepository,
			                            schemaGenerator,
			                            bodyParameter.PropertyInfo(),
			                            bodyParameter.ParameterInfo());

			return new OpenApiRequestBody
			{
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
		)
		{
			try
			{
				return schemaGenerator.GenerateSchema(type, schemaRepository, propertyInfo, parameterInfo, routeInfo);
			}
			catch (Exception ex)
			{
				throw new
					SwaggerGeneratorException($"Failed to generate schema for type - {type}. See inner exception",
					                          ex);
			}
		}
	}
}