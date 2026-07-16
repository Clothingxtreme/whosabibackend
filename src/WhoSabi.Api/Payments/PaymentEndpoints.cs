using WhoSabi.Application.Payments;
using WhoSabi.Contracts.Payments;

namespace WhoSabi.Api.Payments;

public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/payments")
            .WithTags("Payments");

        group.MapPost("/signup/confirm", ConfirmSignupPaymentAsync)
            .WithName("ConfirmSignupPayment");

        return app;
    }

    private static async Task<IResult> ConfirmSignupPaymentAsync(
        ConfirmSignupPaymentRequest request,
        IConfirmSignupPaymentUseCase useCase,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await useCase.HandleAsync(request, cancellationToken);
            return Results.Ok(response);
        }
        catch (ConfirmSignupPaymentException exception)
        {
            return Results.BadRequest(new
            {
                code = exception.Code,
                message = exception.Message
            });
        }
    }
}
