using LineCom.Api.Modules.Catalog.DTOs;
using Microsoft.AspNetCore.Http;

namespace LineCom.Api.Modules.Catalog.Services;

public interface IStorageDiagnosticsService
{
    Task<AdminStorageDiagnosticsResponse> GetDiagnosticsAsync(
        HttpContext httpContext,
        int? maxItems = null,
        CancellationToken cancellationToken = default);
}
