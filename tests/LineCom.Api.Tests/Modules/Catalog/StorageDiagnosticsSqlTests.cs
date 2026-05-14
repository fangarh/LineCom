using LineCom.Api.Modules.Catalog.Controllers;
using LineCom.Api.Modules.Catalog.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace LineCom.Api.Tests.Modules.Catalog;

public sealed class StorageDiagnosticsSqlTests
{
    [Theory]
    [InlineData("INSERT")]
    [InlineData("UPDATE")]
    [InlineData("DELETE")]
    public void StorageDiagnosticsSql_DoesNotContainWriteVerbs(string writeVerb)
    {
        Assert.DoesNotContain(
            writeVerb,
            StorageDiagnosticsSql.ListStoredFiles,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AdminStorageDiagnosticsController_ExposesOnlyReadOnlyGetAction()
    {
        var methods = typeof(AdminStorageDiagnosticsController)
            .GetMethods()
            .Where(method => method.DeclaringType == typeof(AdminStorageDiagnosticsController))
            .ToArray();

        var diagnosticsAction = Assert.Single(methods, method => method.Name == "GetDiagnostics");

        Assert.NotNull(diagnosticsAction.GetCustomAttributes(typeof(HttpGetAttribute), inherit: false).SingleOrDefault());
        Assert.Empty(diagnosticsAction.GetCustomAttributes(typeof(HttpPostAttribute), inherit: false));
        Assert.Empty(diagnosticsAction.GetCustomAttributes(typeof(HttpPutAttribute), inherit: false));
        Assert.Empty(diagnosticsAction.GetCustomAttributes(typeof(HttpPatchAttribute), inherit: false));
        Assert.Empty(diagnosticsAction.GetCustomAttributes(typeof(HttpDeleteAttribute), inherit: false));
    }
}
