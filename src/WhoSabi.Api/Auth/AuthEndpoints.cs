using WhoSabi.Application.Auth;
using WhoSabi.Contracts.Auth;
using WhoSabi.Api.Platform;

namespace WhoSabi.Api.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Auth").RequireRateLimiting("auth");

        group.MapPost("/register/initiate", InitiateRegistrationAsync)
            .WithName("InitiateRegistration");

        group.MapPost("/login", LoginAsync)
            .WithName("Login");

        return app;
    }

    private static async Task<IResult> InitiateRegistrationAsync(
        InitiateRegistrationRequest request,
        IInitiateRegistrationUseCase useCase,
        MonnifyClient provider,
        PaymentService payments,
        CancellationToken cancellationToken)
    {
        provider.RequireConfigured();
        try
        {
            var response = await useCase.HandleAsync(request, cancellationToken);
            try
            {
                var checkout = await payments.Checkout(response.PaymentReference, null, cancellationToken);
                var session = System.Text.Json.JsonSerializer.SerializeToElement(checkout);
                return Results.Ok(response with { PaymentUrl = session.GetProperty("paymentUrl").GetString() });
            }
            catch (ApiFault e) when (e.Status == 502)
            {
                return Results.Json(new { code = e.Code, message = e.Message, response.SignupId, response.PaymentReference, status = "payment_session_pending" }, statusCode: 502);
            }
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
