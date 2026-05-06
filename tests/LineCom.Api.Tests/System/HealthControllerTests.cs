using LineCom.Api.Modules.System.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace LineCom.Api.Tests.System;

public sealed class HealthControllerTests
{
    [Fact]
    public void GetHealth_ReturnsOkWithServiceStatus()
    {
        var controller = new HealthController();

        var result = controller.GetHealth();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<HealthResponse>(okResult.Value);
        Assert.Equal("ok", response.Status);
        Assert.Equal("LineCom.Api", response.Service);
    }
}
