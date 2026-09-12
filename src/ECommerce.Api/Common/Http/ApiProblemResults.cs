namespace ECommerce.Api.Common.Http;

public static class ApiProblemResults
{
    public static IResult Validation(
        string field,
        string message)
    {
        return Results.ValidationProblem(
            new Dictionary<string, string[]>
            {
                [field] = new[] { message }
            });
    }

    public static IResult NotFound(string detail)
    {
        return Results.Problem(
            detail: detail,
            statusCode: StatusCodes.Status404NotFound);
    }

    public static IResult Conflict(string detail)
    {
        return Results.Problem(
            detail: detail,
            statusCode: StatusCodes.Status409Conflict);
    }

    public static IResult InternalServerError()
    {
        return Results.Problem(
            detail: "The server could not complete the request.",
            statusCode: StatusCodes.Status500InternalServerError);
    }
}
