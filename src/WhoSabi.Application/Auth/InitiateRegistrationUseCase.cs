using System.Net.Mail;
using System.Security.Cryptography;
using WhoSabi.Contracts.Auth;
using WhoSabi.Domain.Auth;
using WhoSabi.Domain.Payments;

namespace WhoSabi.Application.Auth;

public sealed class InitiateRegistrationUseCase(
    IRegistrationDataStore dataStore,
    IPasswordHashingService passwordHashingService) : IInitiateRegistrationUseCase
{
    private const long SignupActivationAmountKobo = 250_000;
    private const string Currency = "NGN";
    private static readonly TimeSpan PaymentExpiryWindow = TimeSpan.FromMinutes(30);
    private static readonly HashSet<string> SupportedPaymentMethods = new(
        ["card", "transfer", "ussd"],
        StringComparer.OrdinalIgnoreCase);

    public async Task<InitiateRegistrationResponse> HandleAsync(
        InitiateRegistrationRequest request,
        CancellationToken cancellationToken = default)
    {
        var username = NormalizeUsername(request.Username);
        var email = NormalizeEmail(request.Email);
        var referralCode = NormalizeReferralCode(request.ReferralCode);
        var paymentMethod = NormalizePaymentMethod(request.PaymentMethod);

        ValidatePassword(request.Password);

        var now = DateTimeOffset.UtcNow;
        var hasRegisteredUser = await dataStore.HasRegisteredUserAsync(
            email,
            cancellationToken);

        if (hasRegisteredUser)
        {
            throw new RegistrationInitiationException(
                "EMAIL_ALREADY_REGISTERED",
                "A user already exists for this email.");
        }

        var hasActivePendingSignup = await dataStore.HasActivePendingSignupAsync(
            email,
            now,
            cancellationToken);

        if (hasActivePendingSignup)
        {
            throw new RegistrationInitiationException(
                "PENDING_SIGNUP_EXISTS",
                "A pending signup already exists for this email.");
        }

        var signup = new PendingSignup
        {
            Username = username,
            Email = email,
            NormalizedEmail = email,
            PasswordHash = passwordHashingService.HashPassword(request.Password),
            ReferralCode = referralCode,
            Status = PendingSignupStatus.PendingPayment,
            ExpiresAt = now.Add(PaymentExpiryWindow),
            CreatedAt = now
        };

        var paymentReference = GeneratePaymentReference(now);
        var payment = new PaymentTransaction
        {
            PendingSignupId = signup.Id,
            InternalReference = paymentReference,
            Purpose = PaymentPurpose.SignupActivation,
            Provider = PaymentProvider.Monnify,
            Status = PaymentStatus.Initiated,
            AmountKobo = SignupActivationAmountKobo,
            Currency = Currency,
            PaymentMethod = paymentMethod,
            ExpiresAt = signup.ExpiresAt,
            CreatedAt = now
        };

        await dataStore.AddPendingRegistrationAsync(signup, payment, cancellationToken);

        return new InitiateRegistrationResponse(
            signup.Id,
            payment.InternalReference,
            payment.AmountKobo,
            payment.AmountKobo / 100m,
            payment.Currency,
            "pending_payment",
            payment.PaymentUrl);
    }

    private static string NormalizeUsername(string username)
    {
        var normalized = username.Trim();

        if (normalized.Length is < 3 or > 50)
        {
            throw new RegistrationInitiationException(
                "INVALID_USERNAME",
                "Username must be between 3 and 50 characters.");
        }

        return normalized;
    }

    private static string NormalizeEmail(string email)
    {
        var normalized = email.Trim().ToLowerInvariant();

        if (normalized.Length > 255 || !IsValidEmail(normalized))
        {
            throw new RegistrationInitiationException(
                "INVALID_EMAIL",
                "Enter a valid email address.");
        }

        return normalized;
    }

    private static string? NormalizeReferralCode(string? referralCode)
    {
        var normalized = referralCode?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        if (normalized.Length > 50)
        {
            throw new RegistrationInitiationException(
                "INVALID_REFERRAL_CODE",
                "Referral code is too long.");
        }

        return normalized.ToUpperInvariant();
    }

    private static string NormalizePaymentMethod(string paymentMethod)
    {
        var normalized = paymentMethod.Trim().ToLowerInvariant();

        if (!SupportedPaymentMethods.Contains(normalized))
        {
            throw new RegistrationInitiationException(
                "INVALID_PAYMENT_METHOD",
                "Payment method must be card, transfer, or ussd.");
        }

        return normalized;
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Trim().Length < 8)
        {
            throw new RegistrationInitiationException(
                "INVALID_PASSWORD",
                "Password must be at least 8 characters.");
        }
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var parsed = new MailAddress(email);
            return parsed.Address.Equals(email, StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string GeneratePaymentReference(DateTimeOffset now)
    {
        Span<byte> randomBytes = stackalloc byte[6];
        RandomNumberGenerator.Fill(randomBytes);
        var suffix = Convert.ToHexString(randomBytes);
        return $"WSB-REG-{now:yyyyMMdd}-{suffix}";
    }
}
