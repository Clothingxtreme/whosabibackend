using WhoSabi.Application.Auth;
using WhoSabi.Contracts.Auth;

namespace WhoSabi.Api.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Auth");

        group.MapPost("/register/initiate", InitiateRegistrationAsync)
            .WithName("InitiateRegistration");

        group.MapPost("/login", LoginAsync)
            .WithName("Login");

        return app;
    }

    private static async Task<IResult> InitiateRegistrationAsync(
        InitiateRegistrationRequest request,
        IInitiateRegistrationUseCase useCase,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await useCase.HandleAsync(request, cancellationToken);
            return Results.Ok(response);
        }
        catch (RegistrationInitiationException exception)
        {
            return Results.BadRequest(new
            {
                code = exception.Code,
                message = exception.Message
            });
        }
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        ILoginUseCase useCase,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await useCase.HandleAsync(request, cancellationToken);
            return Results.Ok(response);
        }
        catch (LoginException exception)
        {
            return Results.BadRequest(new
            {
                code = exception.Code,
                message = exception.Message
            });
        }
    }
}
