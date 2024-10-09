using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Iceshrimp.Backend.Controllers.Federation.Attributes;
using Iceshrimp.Backend.Controllers.Mastodon.Attributes;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Iceshrimp.Backend.Core.Extensions;

public static class SwaggerGenOptionsExtensions
{
	public static void AddFilters(this SwaggerGenOptions options)
	{
		options.SchemaFilter<RequireNonNullablePropertiesSchemaFilter>();
		options.SchemaFilter<SwaggerBodyExampleSchemaFilter>();
		options.SupportNonNullableReferenceTypes(); // Sets Nullable flags appropriately.              
		options.UseAllOfToExtendReferenceSchemas(); // Allows $ref enums to be nullable
		options.UseAllOfForInheritance();           // Allows $ref objects to be nullable
		options.OperationFilter<AuthorizeCheckOperationDocumentFilter>();
		options.OperationFilter<HybridRequestOperationFilter>();
		options.OperationFilter<PossibleErrorsOperationFilter>();
		options.OperationFilter<PossibleResultsOperationFilter>();
		options.DocumentFilter<AuthorizeCheckOperationDocumentFilter>();
		options.DocInclusionPredicate(DocInclusionPredicate);
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

	[SuppressMessage("ReSharper", "ClassNeverInstantiated.Local",
	                 Justification = "SwaggerGenOptions.SchemaFilter<T> instantiates this class at runtime")]
	private class SwaggerBodyExampleSchemaFilter : ISchemaFilter
	{
		public void Apply(OpenApiSchema schema, SchemaFilterContext context)
		{
			var att = context.ParameterInfo?.GetCustomAttribute<SwaggerBodyExampleAttribute>();
			if (att != null)
				schema.Example = new OpenApiString(att.Value);
		}
	}

	[SuppressMessage("ReSharper", "ClassNeverInstantiated.Local",
	                 Justification = "SwaggerGenOptions.OperationFilter<T> instantiates this class at runtime")]
	private class AuthorizeCheckOperationDocumentFilter : IOperationFilter, IDocumentFilter
	{
		private const string Web401 =
			"""
			{
			  "statusCode": 401,
			  "error": "Unauthorized",
			  "message": "This method requires an authenticated user"
			}
			""";

		private const string Web403 =
			"""
			{
			  "statusCode": 403,
			  "error": "Forbidden",
			  "message": "This action is outside the authorized scopes"
			}
			""";

		private const string Masto401 =
			"""
			{
			  "error": "This method requires an authenticated user"
			}
			""";

		private const string Masto403 =
			"""
			{
			  "message": "This action is outside the authorized scopes"
			}
			""";

		private static readonly OpenApiString MastoExample401 = new(Masto401);
		private static readonly OpenApiString MastoExample403 = new(Masto403);
		private static readonly OpenApiString WebExample401   = new(Web401);
		private static readonly OpenApiString WebExample403   = new(Web403);

		private static readonly OpenApiReference Ref401 =
			new() { Type = ReferenceType.Response, Id = "error-401" };

		private static readonly OpenApiReference Ref403 =
			new() { Type = ReferenceType.Response, Id = "error-403" };

		private static readonly OpenApiResponse MastoRes401 = new()
		{
			Reference   = Ref401,
			Description = "Unauthorized",
			Content     = { ["application/json"] = new OpenApiMediaType { Example = MastoExample401 } }
		};

		private static readonly OpenApiResponse MastoRes403 = new()
		{
			Reference   = Ref403,
			Description = "Forbidden",
			Content     = { ["application/json"] = new OpenApiMediaType { Example = MastoExample403 } }
		};

		private static readonly OpenApiResponse WebRes401 = new()
		{
			Reference   = Ref401,
			Description = "Unauthorized",
			Content     = { ["application/json"] = new OpenApiMediaType { Example = WebExample401 } }
		};

		private static readonly OpenApiResponse WebRes403 = new()
		{
			Reference   = Ref403,
			Description = "Forbidden",
			Content     = { ["application/json"] = new OpenApiMediaType { Example = WebExample403 } }
		};

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

			operation.Responses.Remove("401");
			operation.Responses.Add("401", new OpenApiResponse { Reference = Ref401 });

			if (authorizeAttribute is { AdminRole: false, ModeratorRole: false, Scopes.Length: 0 } &&
			    authenticateAttribute is { AdminRole: false, ModeratorRole: false, Scopes.Length: 0 })
				return;

			operation.Responses.Remove("403");
			operation.Responses.Add("403", new OpenApiResponse { Reference = Ref403 });
		}

		public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
		{
			if (swaggerDoc.Info.Title == "Mastodon")
			{
				swaggerDoc.Components.Responses.Add(Ref401.Id, MastoRes401);
				swaggerDoc.Components.Responses.Add(Ref403.Id, MastoRes403);
			}
			else
			{
				swaggerDoc.Components.Responses.Add(Ref401.Id, WebRes401);
				swaggerDoc.Components.Responses.Add(Ref403.Id, WebRes403);
			}
		}
	}

	[SuppressMessage("ReSharper", "ClassNeverInstantiated.Local",
	                 Justification = "SwaggerGenOptions.OperationFilter<T> instantiates this class at runtime")]
	private class PossibleErrorsOperationFilter : IOperationFilter
	{
		public void Apply(OpenApiOperation operation, OperationFilterContext context)
		{
			if (context.MethodInfo.DeclaringType is null)
				return;

			var attribute = context.MethodInfo.GetCustomAttributes(true)
			                       .OfType<ProducesErrorsAttribute>()
			                       .FirstOrDefault() ??
			                context.MethodInfo.DeclaringType.GetCustomAttributes(true)
			                       .OfType<ProducesErrorsAttribute>()
			                       .FirstOrDefault();

			if (attribute == null) return;

			var isMastodonController = context.MethodInfo.DeclaringType.GetCustomAttributes(true)
			                                  .OfType<MastodonApiControllerAttribute>()
			                                  .Any();

			var type   = isMastodonController ? typeof(MastodonErrorResponse) : typeof(ErrorResponse);
			var schema = context.SchemaGenerator.GenerateSchema(type, context.SchemaRepository);

			foreach (var status in attribute.StatusCodes.Distinct())
			{
				var res = new OpenApiResponse
				{
					Description = ReasonPhrases.GetReasonPhrase((int)status),
					Content     = { ["application/json"] = new OpenApiMediaType { Schema = schema } }
				};

				operation.Responses.Remove(((int)status).ToString());
				operation.Responses.Add(((int)status).ToString(), res);
			}
		}
	}

	[SuppressMessage("ReSharper", "ClassNeverInstantiated.Local",
	                 Justification = "SwaggerGenOptions.OperationFilter<T> instantiates this class at runtime")]
	private class PossibleResultsOperationFilter : IOperationFilter
	{
		public void Apply(OpenApiOperation operation, OperationFilterContext context)
		{
			if (context.MethodInfo.DeclaringType is null)
				return;

			var attribute = context.MethodInfo.GetCustomAttributes(true)
			                       .OfType<ProducesResultsAttribute>()
			                       .FirstOrDefault() ??
			                context.MethodInfo.DeclaringType.GetCustomAttributes(true)
			                       .OfType<ProducesResultsAttribute>()
			                       .FirstOrDefault();

			if (attribute == null) return;

			var overrideType = context.MethodInfo.GetCustomAttributes(true)
			                          .OfType<OverrideResultTypeAttribute>()
			                          .FirstOrDefault() ??
			                   context.MethodInfo.DeclaringType.GetCustomAttributes(true)
			                          .OfType<OverrideResultTypeAttribute>()
			                          .FirstOrDefault();

			var type = overrideType?.Type ??
			           context.ApiDescription.SupportedResponseTypes.FirstOrDefault(p => p.Type != typeof(void))?.Type;

			var schema = type != null
				? context.SchemaGenerator.GenerateSchema(type, context.SchemaRepository)
				: null;

			var openApiMediaType = new OpenApiMediaType { Schema = schema };
			foreach (var status in attribute.StatusCodes.Distinct())
			{
				var content = schema != null
					? context.ApiDescription.SupportedResponseTypes
					         .Where(p => p.StatusCode == (int)status)
					         .SelectMany(p => p.ApiResponseFormats.Select(i => i.MediaType))
					         .Distinct()
					         .ToDictionary(contentType => contentType, _ => openApiMediaType)
					: null;

				var res = new OpenApiResponse
				{
					Description = ReasonPhrases.GetReasonPhrase((int)status), Content = content
				};

				operation.Responses.Remove(((int)status).ToString());
				operation.Responses.Add(((int)status).ToString(), res);
			}
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

	public class SwaggerBodyExampleAttribute(string value) : Attribute
	{
		public string Value => value;
	}
}