using System.Security.Claims;
using WhoSabi.Application.Users;

namespace WhoSabi.Api.Me;

public static class MeEndpoints
{
    public static IEndpointRouteBuilder MapMeEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/me", GetCurrentUserAsync)
            .RequireAuthorization()
            .WithTags("Me")
            .WithName("GetCurrentUser");

        return app;
    }

    private static async Task<IResult> GetCurrentUserAsync(
        ClaimsPrincipal principal,
        IGetCurrentUserUseCase useCase,
        CancellationToken cancellationToken)
    {
        var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Results.Unauthorized();
        }

        try
        {
            var response = await useCase.HandleAsync(userId, cancellationToken);
            return Results.Ok(response);
        }
        catch (CurrentUserException exception) when (exception.Code == "USER_NOT_FOUND")
        {
            return Results.NotFound(new
            {
                code = exception.Code,
                message = exception.Message
            });
        }
        catch (CurrentUserException exception)
        {
            return Results.BadRequest(new
            {
                code = exception.Code,
                message = exception.Message
            });
        }
    }
}
