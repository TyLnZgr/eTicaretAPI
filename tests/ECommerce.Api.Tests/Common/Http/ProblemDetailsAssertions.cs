using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Tests.Common.Http;

internal static class ProblemDetailsAssertions
{
    public static async Task AssertValidationErrorsAsync(
        HttpResponseMessage response,
        IReadOnlyDictionary<string, string> expectedErrors)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content
            .ReadFromJsonAsync<HttpValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Equal(
            "One or more validation errors occurred.",
            problem.Title);

        foreach (var expectedError in expectedErrors)
        {
            Assert.True(
                problem.Errors.TryGetValue(
                    expectedError.Key,
                    out var errors));

            Assert.Contains(expectedError.Value, errors);
        }
    }

    public static async Task AssertValidationAsync(
        HttpResponseMessage response,
        string expectedField,
        string expectedMessage)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content
            .ReadFromJsonAsync<HttpValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Equal(
            "One or more validation errors occurred.",
            problem.Title);

        Assert.True(
            problem.Errors.TryGetValue(
                expectedField,
                out var errors));

        Assert.Contains(expectedMessage, errors);
    }

    public static async Task AssertProblemAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatusCode,
        string expectedTitle,
        string expectedDetail)
    {
        Assert.Equal(expectedStatusCode, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content
            .ReadFromJsonAsync<ProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal((int)expectedStatusCode, problem.Status);
        Assert.Equal(expectedTitle, problem.Title);
        Assert.Equal(expectedDetail, problem.Detail);
        Assert.False(string.IsNullOrWhiteSpace(problem.Type));
    }
}
