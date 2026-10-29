using Microsoft.EntityFrameworkCore;
using WhoSabi.Application.Payments;
using WhoSabi.Contracts.Payments;
using WhoSabi.Domain.Payments;
using WhoSabi.Domain.Wallets;
using WhoSabi.Infrastructure.Persistence;

namespace WhoSabi.Api.Platform;

public sealed class PaymentService(WhoSabiDbContext db, MonnifyClient provider, ISignupPaymentActivationDataStore activation)
{
    public async Task<object> Checkout(string reference, Guid? userId, CancellationToken ct)
    {
        provider.RequireConfigured();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM payment_transactions WHERE \"InternalReference\" = {reference} FOR UPDATE", ct);
        db.ChangeTracker.Clear();
        var payment = await db.PaymentTransactions.SingleOrDefaultAsync(x => x.InternalReference == reference, ct)
            ?? throw new ApiFault(404, "PAYMENT_NOT_FOUND", "Payment reference not found.");
        if (payment.Purpose == PaymentPurpose.WalletTopUp && payment.UserId != userId)
            throw new ApiFault(404, "PAYMENT_NOT_FOUND", "Payment reference not found.");
        if (payment.Status == PaymentStatus.Paid) { await transaction.CommitAsync(ct); return new { paymentReference = reference, status = "paid", paymentUrl = (string?)null }; }
        if (payment.Status is PaymentStatus.Failed or PaymentStatus.Cancelled) throw new ApiFault(409, "PAYMENT_CLOSED", "Start a new payment.");
        if (payment.ExpiresAt <= DateTimeOffset.UtcNow) throw new ApiFault(409, "PAYMENT_EXPIRED", "Start a new payment.");
        if (payment.PaymentUrl is null)
        {
            var signup = payment.PendingSignupId is null ? null : await db.PendingSignups.SingleAsync(x => x.Id == payment.PendingSignupId, ct);
            var user = payment.UserId is null ? null : await db.Users.SingleAsync(x => x.Id == payment.UserId, ct);
            var session = await provider.Initialize(reference, payment.AmountKobo, signup?.Email ?? user!.Email, signup?.Username ?? user!.Username, ct);
            payment.PaymentUrl = session.Url; payment.ProviderReference = session.Reference; payment.Status = PaymentStatus.Pending;
            await db.SaveChangesAsync(ct);
        }
        await transaction.CommitAsync(ct);
        return new { paymentReference = reference, paymentUrl = payment.PaymentUrl, amountKobo = payment.AmountKobo, currency = payment.Currency, status = payment.Status.ToString().ToLowerInvariant(), expiresAt = payment.ExpiresAt };
    }

    public async Task<object> VerifyAndApply(string reference, Guid? userId, CancellationToken ct)
    {
        var payment = await db.PaymentTransactions.AsNoTracking().SingleOrDefaultAsync(x => x.InternalReference == reference, ct)
            ?? throw new ApiFault(404, "PAYMENT_NOT_FOUND", "Payment reference not found.");
        if (payment.Purpose == PaymentPurpose.WalletTopUp && payment.UserId != userId)
            throw new ApiFault(404, "PAYMENT_NOT_FOUND", "Payment reference not found.");
        var verified = await provider.Verify(reference, ct);
        var email = payment.PendingSignupId is not null
            ? (await db.PendingSignups.SingleAsync(x => x.Id == payment.PendingSignupId, ct)).Email
            : (await db.Users.SingleAsync(x => x.Id == payment.UserId, ct)).Email;
        if (verified.Reference != reference || verified.Currency != payment.Currency || !string.Equals(verified.Email, email, StringComparison.OrdinalIgnoreCase) ||
            payment.ProviderReference is not null && verified.ProviderReference != payment.ProviderReference)
            throw new ApiFault(409, "PAYMENT_MISMATCH", "Provider details do not match this payment.");
        if (verified.Status != "PAID")
        {
            if (verified.Status is "FAILED" or "CANCELLED" or "EXPIRED")
                await db.PaymentTransactions.Where(x => x.Id == payment.Id && x.Status != PaymentStatus.Paid)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, verified.Status == "FAILED" ? PaymentStatus.Failed : verified.Status == "EXPIRED" ? PaymentStatus.Expired : PaymentStatus.Cancelled), ct);
            throw new ApiFault(409, "PAYMENT_NOT_PAID", "Provider payment status: " + verified.Status);
        }
        if (verified.AmountKobo != payment.AmountKobo) throw new ApiFault(409, "PAYMENT_AMOUNT_MISMATCH", "Verified payment amount differs from the expected amount.");
        if (payment.Purpose == PaymentPurpose.SignupActivation)
            return await activation.CompleteSignupPaymentAsync(payment.Id, verified.ProviderReference, 50_000, DateTimeOffset.UtcNow, ct);
        if (payment.Purpose != PaymentPurpose.WalletTopUp) throw new ApiFault(409, "PAYMENT_PURPOSE_INVALID", "Unsupported payment purpose.");

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM payment_transactions WHERE \"Id\" = {payment.Id} FOR UPDATE", ct);
        db.ChangeTracker.Clear();
        var current = await db.PaymentTransactions.SingleAsync(x => x.Id == payment.Id, ct);
        if (current.Status != PaymentStatus.Paid)
        {
            var wallet = await WalletService.Lock(db, current.UserId!.Value, ct);
            WalletService.Credit(db, wallet, current.AmountKobo, WalletLedgerEntryType.Credit, "Wallet deposit", current.InternalReference);
            db.Add(new WhoSabi.Domain.Platform.AppNotification { UserId = current.UserId.Value, Title = "Deposit confirmed", Body = $"Your wallet was credited {current.AmountKobo / 100m:0.00} NGN.", Link = "/wallet" });
            current.Status = PaymentStatus.Paid; current.PaidAt = DateTimeOffset.UtcNow; current.ProviderReference = verified.ProviderReference;
            await db.SaveChangesAsync(ct);
        }
        await tx.CommitAsync(ct);
        return new { paymentReference = reference, status = "paid" };
    }
}
