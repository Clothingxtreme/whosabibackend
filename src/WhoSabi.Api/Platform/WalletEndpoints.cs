using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using WhoSabi.Domain.Payments;
using WhoSabi.Domain.Platform;
using WhoSabi.Infrastructure.Persistence;

namespace WhoSabi.Api.Platform;

public sealed record DepositRequest(long AmountKobo, string IdempotencyKey);
public sealed record BankRequest(string BankCode, string AccountNumber);

public static class PlatformWalletEndpoints
{
    public static void MapPlatformWallet(this WebApplication app)
    {
        var wallet = app.MapGroup("/api/wallet").RequireAuthorization();
        wallet.MapPost("/deposits", async (DepositRequest input, ClaimsPrincipal principal, WhoSabiDbContext db, MonnifyClient provider, PaymentService payments, IConfiguration config, CancellationToken ct) => {
            provider.RequireConfigured(); var uid = principal.UserId();
            if (await db.Users.AnyAsync(x => x.Id == uid && x.IsTestAccount, ct)) throw new ApiFault(403, "TEST_ACCOUNT_RESTRICTED", "Test accounts cannot deposit.");
            var max = config.GetValue<long?>("Wallet:MaxDepositKobo") ?? 10_000_000;
            if (input.AmountKobo < 10_000 || input.AmountKobo > max) throw new ApiFault(400, "INVALID_AMOUNT", $"Deposit must be between 10000 and {max} kobo.");
            var key = ApiSupport.Required(input.IdempotencyKey, 80, "IdempotencyKey");
            var reference = "WSB-DEP-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{uid}:{key}")))[..48];
            var payment = await db.PaymentTransactions.SingleOrDefaultAsync(x => x.InternalReference == reference, ct);
            if (payment is null)
            {
                payment = new PaymentTransaction { UserId = uid, InternalReference = reference, AmountKobo = input.AmountKobo, Purpose = PaymentPurpose.WalletTopUp, Provider = PaymentProvider.Monnify, PaymentMethod = "checkout", ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30) };
                db.Add(payment); await db.SaveChangesAsync(ct);
            }
            else if (payment.AmountKobo != input.AmountKobo) throw new ApiFault(409, "IDEMPOTENCY_CONFLICT", "Key already used for a different amount.");
            return Results.Ok(await payments.Checkout(reference, uid, ct));
        });
        wallet.MapPost("/deposits/{reference}/verify", async (string reference, ClaimsPrincipal user, PaymentService service, CancellationToken ct) => Results.Ok(await service.VerifyAndApply(reference, user.UserId(), ct)));
        wallet.MapGet("/payments/{reference}", async (string reference, ClaimsPrincipal user, WhoSabiDbContext db, CancellationToken ct) => {
            var uid = user.UserId();
            var payment = await db.PaymentTransactions.AsNoTracking().Where(x => x.InternalReference == reference && x.UserId == uid)
                .Select(x => new { paymentReference = x.InternalReference, x.AmountKobo, x.Currency, status = x.Status.ToString().ToLowerInvariant(), x.CreatedAt, x.PaidAt }).SingleOrDefaultAsync(ct);
            return payment is null ? Results.NotFound() : Results.Ok(payment);
        });
        wallet.MapPost("/transfers", async (TransferRequest input, ClaimsPrincipal user, WalletService service, CancellationToken ct) => Results.Ok(await service.Transfer(user.UserId(), input.Username, input.AmountKobo, input.IdempotencyKey, "transfer", ct)));
        app.MapGet("/api/gifts", () => Results.Ok(WalletService.Gifts)).RequireAuthorization();
        app.MapPost("/api/gifts/send", async (SendGiftRequest input, ClaimsPrincipal user, WalletService service, CancellationToken ct) => {
            var gift = WalletService.Gifts.SingleOrDefault(x => x.Id == input.GiftId) ?? throw new ApiFault(404, "GIFT_NOT_FOUND", "Gift not found.");
            return Results.Ok(await service.Transfer(user.UserId(), input.Username, gift.PriceKobo, input.IdempotencyKey, "gift:" + gift.Id, ct));
        }).RequireAuthorization();
        wallet.MapGet("/operations/{id:guid}", async (Guid id, ClaimsPrincipal user, WhoSabiDbContext db, CancellationToken ct) => {
            var uid = user.UserId();
            var operation = await db.Set<WalletOperation>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.UserId == uid, ct);
            return operation is null ? Results.NotFound() : Results.Ok(operation);
        });
        var banks = app.MapGroup("/api/me/bank-accounts").RequireAuthorization();
        banks.MapGet("", async (ClaimsPrincipal user, WhoSabiDbContext db, CancellationToken ct) => {
            var uid = user.UserId();
            var accounts = await db.Set<BankAccount>().Where(x => x.UserId == uid).ToListAsync(ct);
            return Results.Ok(accounts.Select(x => new { x.Id, x.BankCode, accountNumber = "******" + x.AccountNumber[^4..], x.AccountName, x.VerifiedAt }));
        });
        banks.MapPost("", async (BankRequest input, ClaimsPrincipal user, WhoSabiDbContext db, MonnifyClient provider, CancellationToken ct) => {
            if (!Regex.IsMatch(input.AccountNumber ?? "", "^[0-9]{10}$") || !Regex.IsMatch(input.BankCode ?? "", "^[0-9]{3,10}$")) throw new ApiFault(400, "INVALID_BANK_ACCOUNT", "Enter a bank code and 10 digit account number.");
            var uid = user.UserId();
            var account = await db.Set<BankAccount>().SingleOrDefaultAsync(x => x.UserId == uid && x.BankCode == input.BankCode && x.AccountNumber == input.AccountNumber, ct);
            if (account is null)
            {
                var name = await provider.ResolveBank(input.BankCode!, input.AccountNumber!, ct);
                account = new BankAccount { UserId = uid, BankCode = input.BankCode!, AccountNumber = input.AccountNumber!, AccountName = name, VerifiedAt = DateTimeOffset.UtcNow };
                db.Add(account); await db.SaveChangesAsync(ct);
            }
            return Results.Ok(new { account.Id, account.BankCode, account.AccountName, account.VerifiedAt });
        });
        banks.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal user, WhoSabiDbContext db, CancellationToken ct) => {
            var uid = user.UserId(); await db.Set<BankAccount>().Where(x => x.Id == id && x.UserId == uid).ExecuteDeleteAsync(ct); return Results.NoContent();
        });
        // No money is reserved or debited without a configured payout and identity-verification workflow.
        wallet.MapPost("/withdrawals", () => Results.Json(new { code = "PAYOUTS_NOT_CONFIGURED", message = "Withdrawals require merchant approval, verified identity and a configured disbursement workflow." }, statusCode: 503));
        wallet.MapGet("/withdrawals", () => Results.Json(new { code = "PAYOUTS_NOT_CONFIGURED", message = "Withdrawal processing is not configured." }, statusCode: 503));
    }
}
