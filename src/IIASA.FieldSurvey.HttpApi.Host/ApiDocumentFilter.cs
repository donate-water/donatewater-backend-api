using System.Collections.Generic;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace IIASA.FieldSurvey;

public class ApiDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var paths = new Dictionary<string, OpenApiPathItem>(swaggerDoc.Paths);
        foreach (var path in paths)
        {
            if (path.Key.Contains("role"))
            {
                continue;
            }
            if (path.Key.StartsWith("/api/account") && !path.Key.Contains("dummy"))
            {
                continue;
            }
            if (path.Key.StartsWith("/api/") || path.Key.Contains("dummy"))
            {
                swaggerDoc.Paths.Remove(path.Key);
            }
        }
    }
}