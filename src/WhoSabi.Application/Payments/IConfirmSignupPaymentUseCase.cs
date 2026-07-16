using WhoSabi.Contracts.Payments;

namespace WhoSabi.Application.Payments;

public interface IConfirmSignupPaymentUseCase
{
    Task<ConfirmSignupPaymentResponse> HandleAsync(
        ConfirmSignupPaymentRequest request,
        CancellationToken cancellationToken = default);
}
