namespace WhoSabi.Domain.Payments;

public enum PaymentStatus
{
    Initiated = 1,
    Pending = 2,
    Paid = 3,
    Failed = 4,
    Expired = 5,
    Cancelled = 6
}
