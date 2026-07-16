using System.Security.Claims;
using WhoSabi.Application.Wallets;

namespace WhoSabi.Api.Wallets;

public static class WalletEndpoints
{
    public static IEndpointRouteBuilder MapWalletEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/wallet", GetWalletAsync)
            .RequireAuthorization()
            .WithTags("Wallet")
            .WithName("GetWallet");

        app.MapGet("/api/wallet/transactions", GetWalletTransactionsAsync)
            .RequireAuthorization()
            .WithTags("Wallet")
            .WithName("GetWalletTransactions");

        return app;
    }

    private static async Task<IResult> GetWalletAsync(
        ClaimsPrincipal principal,
        IGetWalletUseCase useCase,
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
        catch (WalletException exception) when (exception.Code == "WALLET_NOT_FOUND")
        {
            return Results.NotFound(new
            {
                code = exception.Code,
                message = exception.Message
            });
        }
        catch (WalletException exception)
        {
            return Results.BadRequest(new
            {
                code = exception.Code,
                message = exception.Message
            });
        }
    }

    private static async Task<IResult> GetWalletTransactionsAsync(
        ClaimsPrincipal principal,
        IGetWalletTransactionsUseCase useCase,
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
        catch (WalletException exception) when (exception.Code == "WALLET_NOT_FOUND")
        {
            return Results.NotFound(new
            {
                code = exception.Code,
                message = exception.Message
            });
        }
        catch (WalletException exception)
        {
            return Results.BadRequest(new
            {
                code = exception.Code,
                message = exception.Message
            });
        }
    }
}
