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
using Microsoft.OpenApi;
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
		public void Apply(IOpenApiSchema model, SchemaFilterContext context)
		{
			var additionalRequiredProps = model.Properties
			                                   ?.Where(x => x.Value.Type?.HasFlag(JsonSchemaType.Null) != true
			                                                && model.Required?.Contains(x.Key) != true)
			                                   .Select(x => x.Key);

			if (additionalRequiredProps is null) return;

			foreach (var propKey in additionalRequiredProps)
				model.Required?.Add(propKey);
		}
	}

	[SuppressMessage("ReSharper", "ClassNeverInstantiated.Local",
	                 Justification = "SwaggerGenOptions.SchemaFilter<T> instantiates this class at runtime")]
	private class SwaggerBodyExampleSchemaFilter : ISchemaFilter
	{
		public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
		{
			var att = context.ParameterInfo?.GetCustomAttribute<SwaggerBodyExampleAttribute>();
			if (att == null) return;
			schema.Examples?.Clear();
			schema.Examples?.Add(att.Value);
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

		private static readonly OpenApiResponseReference Ref401 = new("error-401");
		private static readonly OpenApiResponseReference Ref403 = new("error-403");

		private static readonly OpenApiResponse MastoRes401 = new()
		{
			Description = "Unauthorized",
			Content = new Dictionary<string, OpenApiMediaType> { ["application/json"] = new() { Example = Masto401 } }
		};

		private static readonly OpenApiResponse MastoRes403 = new()
		{
			Description = "Forbidden",
			Content     = new Dictionary<string, OpenApiMediaType> { ["application/json"] = new() { Example = Masto403 } }
		};

		private static readonly OpenApiResponse WebRes401 = new()
		{
			Description = "Unauthorized",
			Content     = new Dictionary<string, OpenApiMediaType> { ["application/json"] = new() { Example = Web401 } }
		};

		private static readonly OpenApiResponse WebRes403 = new()
		{
			Description = "Forbidden",
			Content     = new Dictionary<string, OpenApiMediaType> { ["application/json"] = new() { Example = Web403 } }
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

			var securitySchemaName = isMastodonController ? "mastodon" : "iceshrimp";
			var schema             = new OpenApiSecuritySchemeReference(securitySchemaName, context.Document);
			operation.Security = new List<OpenApiSecurityRequirement> { new() { [schema] = [] } };

			if (authorizeAttribute == null) return;

			operation.Responses?.Remove("401");
			operation.Responses ??= [];
			operation.Responses.Add("401", Ref401);

			if (authorizeAttribute is { AdminRole: false, ModeratorRole: false, Scopes.Length: 0 } &&
			    authenticateAttribute is { AdminRole: false, ModeratorRole: false, Scopes.Length: 0 })
				return;

			operation.Responses?.Remove("403");
			operation.Responses ??= [];
			operation.Responses.Add("403", Ref403);
		}

		public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
		{
			swaggerDoc.Components           ??= new OpenApiComponents();
			swaggerDoc.Components.Responses ??= new OpenApiResponses();

			if (swaggerDoc.Info.Title == "Mastodon")
			{
				swaggerDoc.Components.Responses.Add(Ref401.Reference.Id!, MastoRes401);
				swaggerDoc.Components.Responses.Add(Ref403.Reference.Id!, MastoRes403);
			}
			else
			{
				swaggerDoc.Components.Responses.Add(Ref401.Reference.Id!, WebRes401);
				swaggerDoc.Components.Responses.Add(Ref403.Reference.Id!, WebRes403);
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
					Content = new Dictionary<string, OpenApiMediaType>
					{
						["application/json"] = new() { Schema = schema }
					}
				};

				operation.Responses?.Remove(((int)status).ToString());
				operation.Responses ??= [];
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

				operation.Responses?.Remove(((int)status).ToString());
				operation.Responses ??= [];
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
			if (context.ApiDescription.ParameterDescriptions.All(p => p.Source != HybridBindingSource.Hybrid))
				return;

			operation.RequestBody =
				GenerateRequestBody(context.ApiDescription, context.SchemaRepository, context.SchemaGenerator);
			operation.Parameters?.Clear();
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

		private static IOpenApiSchema GenerateSchema(
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