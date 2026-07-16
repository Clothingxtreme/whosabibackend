namespace WhoSabi.Domain.Auth;

public enum PendingSignupStatus
{
    PendingPayment = 1,
    PaymentFailed = 2,
    Completed = 3,
    Expired = 4,
    Cancelled = 5
}