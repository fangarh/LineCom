using LineCom.Api.Modules.Requests.Services;
using LineCom.Api.Shared.Errors;
using Microsoft.AspNetCore.Http;

namespace LineCom.Api.Tests.Modules.Requests;

public sealed class RequestReferenceDataTests
{
    [Theory]
    [InlineData("new", "Новая")]
    [InlineData("in_progress", "В работе")]
    [InlineData("completed", "Завершена")]
    [InlineData("cancelled", "Отменена")]
    public void GetStatus_ReturnsReleaseStatusLabels(string code, string label)
    {
        var referenceData = new RequestReferenceData();

        var status = referenceData.GetStatus(code);

        Assert.Equal(code, status.Code);
        Assert.Equal(label, status.Label);
    }

    [Fact]
    public void GetStatus_RejectsQuotedStatus()
    {
        var referenceData = new RequestReferenceData();

        var exception = Assert.Throws<ApiException>(() => referenceData.GetStatus("quoted"));

        Assert.Equal("request.invalid_status", exception.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
    }
}
