namespace WhoSabi.Application.Payments;

public sealed class ConfirmSignupPaymentException(
    string code,
    string message) : Exception(message)
{
    public string Code { get; } = code;
}
