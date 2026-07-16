using WhoSabi.Domain.Auth;
using WhoSabi.Domain.Payments;

namespace WhoSabi.Application.Payments;

public sealed record SignupPaymentActivationRecord(
    PendingSignup PendingSignup,
    PaymentTransaction PaymentTransaction);
