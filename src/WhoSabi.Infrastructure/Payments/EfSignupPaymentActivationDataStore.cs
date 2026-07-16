using Microsoft.EntityFrameworkCore;
using WhoSabi.Application.Payments;
using WhoSabi.Contracts.Payments;
using WhoSabi.Domain.Auth;
using WhoSabi.Domain.Payments;
using WhoSabi.Domain.Users;
using WhoSabi.Domain.Wallets;
using WhoSabi.Infrastructure.Persistence;

namespace WhoSabi.Infrastructure.Payments;

public sealed class EfSignupPaymentActivationDataStore(WhoSabiDbContext dbContext)
    : ISignupPaymentActivationDataStore
{
    public async Task<SignupPaymentActivationRecord?> FindSignupPaymentAsync(
        string paymentReference,
        CancellationToken cancellationToken = default)
    {
        var payment = await dbContext.PaymentTransactions.SingleOrDefaultAsync(
            transaction => transaction.InternalReference == paymentReference,
            cancellationToken);

        if (payment?.PendingSignupId is null)
        {
            return null;
        }

        var pendingSignup = await dbContext.PendingSignups.SingleOrDefaultAsync(
            signup => signup.Id == payment.PendingSignupId.Value,
            cancellationToken);

        return pendingSignup is null
            ? null
            : new SignupPaymentActivationRecord(pendingSignup, payment);
    }

    public async Task<ConfirmSignupPaymentResponse?> FindActivatedSignupAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(
            candidate => candidate.NormalizedEmail == normalizedEmail,
            cancellationToken);

        if (user is null)
        {
            return null;
        }

        var wallet = await dbContext.WalletAccounts.SingleOrDefaultAsync(
            account => account.UserId == user.Id,
            cancellationToken);

        if (wallet is null)
        {
            return null;
        }

        return new ConfirmSignupPaymentResponse(
            user.Id,
            wallet.Id,
            0,
            wallet.BalanceKobo,
            "already_completed");
    }

    public async Task<ConfirmSignupPaymentResponse> CompleteSignupPaymentAsync(
        Guid paymentTransactionId,
        string providerReference,
        long starterBonusKobo,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var payment = await dbContext.PaymentTransactions.SingleOrDefaultAsync(
            candidate => candidate.Id == paymentTransactionId,
            cancellationToken);

        if (payment is null)
        {
            throw new ConfirmSignupPaymentException(
                "PAYMENT_NOT_FOUND",
                "Payment reference was not found.");
        }

        if (payment.PendingSignupId is null)
        {
            throw new ConfirmSignupPaymentException(
                "INVALID_PAYMENT_STATE",
                "Payment is not connected to a pending signup.");
        }

        var pendingSignup = await dbContext.PendingSignups.SingleOrDefaultAsync(
            signup => signup.Id == payment.PendingSignupId.Value,
            cancellationToken);

        if (pendingSignup is null)
        {
            throw new ConfirmSignupPaymentException(
                "PENDING_SIGNUP_NOT_FOUND",
                "Pending signup was not found.");
        }

        if (pendingSignup.Status == PendingSignupStatus.Completed &&
            payment.Status == PaymentStatus.Paid)
        {
            var activatedSignup = await FindActivatedSignupAsync(
                pendingSignup.NormalizedEmail,
                cancellationToken);

            if (activatedSignup is not null)
            {
                await transaction.CommitAsync(cancellationToken);
                return activatedSignup;
            }
        }

        if (pendingSignup.Status != PendingSignupStatus.PendingPayment ||
            payment.Status is not (PaymentStatus.Initiated or PaymentStatus.Pending))
        {
            throw new ConfirmSignupPaymentException(
                "INVALID_PAYMENT_STATE",
                "Signup payment cannot be completed from its current state.");
        }

        var providerReferenceExists = await dbContext.PaymentTransactions.AnyAsync(
            candidate =>
                candidate.ProviderReference == providerReference &&
                candidate.Id != payment.Id,
            cancellationToken);

        if (providerReferenceExists)
        {
            throw new ConfirmSignupPaymentException(
                "PROVIDER_REFERENCE_EXISTS",
                "Provider reference has already been processed.");
        }

        var existingUser = await dbContext.Users.SingleOrDefaultAsync(
            user => user.NormalizedEmail == pendingSignup.NormalizedEmail,
            cancellationToken);

        if (existingUser is not null)
        {
            throw new ConfirmSignupPaymentException(
                "USER_ALREADY_EXISTS",
                "A user already exists for this email.");
        }

        var user = new User
        {
            Username = pendingSignup.Username,
            Email = pendingSignup.Email,
            NormalizedEmail = pendingSignup.NormalizedEmail,
            PasswordHash = pendingSignup.PasswordHash,
            AvatarLabel = BuildAvatarLabel(pendingSignup.Username),
            ReferralCode = BuildReferralCode(pendingSignup.Username),
            HasPaidRegistrationFee = true,
            CreatedAt = now
        };

        var wallet = new WalletAccount
        {
            UserId = user.Id,
            BalanceKobo = starterBonusKobo,
            Currency = payment.Currency,
            CreatedAt = now
        };

        var ledgerEntry = new WalletLedgerEntry
        {
            WalletAccountId = wallet.Id,
            UserId = user.Id,
            AmountKobo = starterBonusKobo,
            BalanceAfterKobo = starterBonusKobo,
            Type = WalletLedgerEntryType.Bonus,
            Title = "Starter bonus credited",
            Description = "Signup payment completed. Starter wallet bonus credited.",
            Reference = payment.InternalReference,
            CreatedAt = now
        };

        pendingSignup.Status = PendingSignupStatus.Completed;
        pendingSignup.UpdatedAt = now;

        payment.Status = PaymentStatus.Paid;
        payment.ProviderReference = providerReference;
        payment.PaidAt = now;
        payment.UpdatedAt = now;

        dbContext.Users.Add(user);
        dbContext.WalletAccounts.Add(wallet);
        dbContext.WalletLedgerEntries.Add(ledgerEntry);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new ConfirmSignupPaymentResponse(
            user.Id,
            wallet.Id,
            starterBonusKobo,
            wallet.BalanceKobo,
            "completed");
    }

    private static string BuildAvatarLabel(string username)
    {
        var parts = username.Split(
            ' ',
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length >= 2)
        {
            return string.Concat(parts[0][0], parts[1][0]).ToUpperInvariant();
        }

        return username.Length <= 2
            ? username.ToUpperInvariant()
            : username[..2].ToUpperInvariant();
    }

    private static string BuildReferralCode(string username)
    {
        var prefix = new string(
            username
                .Where(char.IsLetterOrDigit)
                .Take(4)
                .ToArray());

        if (string.IsNullOrWhiteSpace(prefix))
        {
            prefix = "SABI";
        }

        return $"{prefix.ToUpperInvariant()}{Guid.NewGuid():N}"[..12].ToUpperInvariant();
    }
}
