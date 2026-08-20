using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.OpenApi;

namespace QueenZone.Web;

public static class QueenZoneJsonApiServiceCollectionExtensions
{
    public static IServiceCollection AddQueenZoneJsonApi(this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        });
        services.AddOpenApi(ApiV1.OpenApiDocumentName, options =>
        {
            options.ShouldInclude = static description =>
                string.Equals(description.GroupName, ApiV1.OpenApiDocumentName, StringComparison.OrdinalIgnoreCase);
            options.AddDocumentTransformer<ApiV1OpenApiDocumentTransformer>();
        });
        return services;
    }
}
