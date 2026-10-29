using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WhoSabi.Api.Platform;
using WhoSabi.Infrastructure.Persistence;

namespace WhoSabi.Api.Payments;

public sealed record PaymentReferenceRequest(string PaymentReference);

public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/payments").WithTags("Payments");
        // References are checkout capabilities, never proof that a payment succeeded.
        group.MapPost("/signup/confirm", async (PaymentReferenceRequest input, PaymentService service, CancellationToken ct) =>
            Results.Ok(await service.VerifyAndApply(ApiSupport.Required(input.PaymentReference, 80, "PaymentReference"), null, ct)));
        group.MapPost("/signup/session", async (PaymentReferenceRequest input, PaymentService service, CancellationToken ct) =>
            Results.Ok(await service.Checkout(ApiSupport.Required(input.PaymentReference, 80, "PaymentReference"), null, ct)));
        group.MapPost("/monnify/webhook", async (HttpRequest request, MonnifyClient provider, PaymentService service, WhoSabiDbContext db, CancellationToken ct) => {
            using var buffer = new MemoryStream();
            if (request.ContentLength > 65_536) return Results.StatusCode(413);
            var chunk = new byte[8192]; int count;
            while ((count = await request.Body.ReadAsync(chunk, ct)) > 0)
            {
                if (buffer.Length + count > 65_536) return Results.StatusCode(413);
                buffer.Write(chunk, 0, count);
            }
            if (!provider.ValidSignature(buffer.ToArray(), request.Headers["monnify-signature"])) return Results.Unauthorized();
            JsonDocument document;
            try { document = JsonDocument.Parse(buffer.ToArray()); }
            catch (JsonException) { return Results.BadRequest(); }
            using (document)
            {
                var root = document.RootElement;
                if (!root.TryGetProperty("eventType", out var eventType) || eventType.GetString() != "SUCCESSFUL_TRANSACTION") return Results.Ok();
                if (!root.TryGetProperty("eventData", out var data) || !data.TryGetProperty("paymentReference", out var reference)) return Results.BadRequest();
                var refValue = reference.GetString();
                var payment = await db.PaymentTransactions.AsNoTracking().SingleOrDefaultAsync(x => x.InternalReference == refValue, ct);
                if (payment is null) return Results.Ok();
                await service.VerifyAndApply(payment.InternalReference, payment.UserId, ct);
                return Results.Ok();
            }
        });
        return app;
    }
}
