using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace WhoSabi.Api.Platform;

public sealed record VerifiedPayment(string Reference, string ProviderReference, string Status, long AmountKobo, string Currency, string Email);

public sealed class MonnifyClient(HttpClient http, IConfiguration config)
{
    public bool Configured => !string.IsNullOrWhiteSpace(config["Monnify:ApiKey"]) &&
        !string.IsNullOrWhiteSpace(config["Monnify:SecretKey"]) && !string.IsNullOrWhiteSpace(config["Monnify:ContractCode"]);

    public void RequireConfigured()
    {
        if (!Configured) throw new ApiFault(503, "PAYMENTS_NOT_CONFIGURED", "Payment checkout is unavailable until Monnify credentials are configured.");
    }

    private string BaseUrl
    {
        get
        {
            var url = config["Monnify:BaseUrl"] ?? "https://sandbox.monnify.com";
            if (url != "https://sandbox.monnify.com" && url != "https://api.monnify.com")
                throw new ApiFault(503, "PAYMENT_CONFIG_INVALID", "Unsupported payment provider URL.");
            if (url == "https://api.monnify.com" && !config.GetValue<bool>("Monnify:AllowLivePayments"))
                throw new ApiFault(503, "LIVE_PAYMENTS_DISABLED", "Live payments are disabled.");
            return url;
        }
    }

    private async Task<JsonElement> Call(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        RequireConfigured();
        try
        {
            using var auth = new HttpRequestMessage(HttpMethod.Post, BaseUrl + "/api/v1/auth/login");
            auth.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{config["Monnify:ApiKey"]}:{config["Monnify:SecretKey"]}")));
            using var authResponse = await http.SendAsync(auth, ct);
            var tokenBody = await Read(authResponse, ct);
            using var request = new HttpRequestMessage(method, BaseUrl + path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenBody.GetProperty("accessToken").GetString());
            if (body is not null) request.Content = JsonContent.Create(body);
            using var response = await http.SendAsync(request, ct);
            return await Read(response, ct);
        }
        catch (Exception e) when (e is HttpRequestException or JsonException or KeyNotFoundException || e is TaskCanceledException && !ct.IsCancellationRequested)
        { throw new ApiFault(502, "PAYMENT_PROVIDER_UNAVAILABLE", "The payment provider could not be reached. Retry the same reference."); }
    }

    private static async Task<JsonElement> Read(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode) throw new ApiFault(502, "PAYMENT_PROVIDER_ERROR", "The payment provider rejected the request.");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        if (!json.RootElement.GetProperty("requestSuccessful").GetBoolean())
            throw new ApiFault(502, "PAYMENT_PROVIDER_ERROR", "The payment provider could not process this request.");
        return json.RootElement.GetProperty("responseBody").Clone();
    }

    public async Task<(string Url, string Reference)> Initialize(string reference, long amount, string email, string name, CancellationToken ct)
    {
        var result = await Call(HttpMethod.Post, "/api/v1/merchant/transactions/init-transaction", new {
            amount = amount / 100m, customerName = name, customerEmail = email,
            paymentReference = reference, paymentDescription = "Who Sabi " + reference,
            currencyCode = "NGN", contractCode = config["Monnify:ContractCode"],
            redirectUrl = config["Monnify:RedirectUrl"] ?? "https://whosabi.ng/payments/return",
            paymentMethods = new[] { "CARD", "ACCOUNT_TRANSFER", "USSD" }
        }, ct);
        var returnedRef = result.GetProperty("paymentReference").GetString();
        var url = result.GetProperty("checkoutUrl").GetString() ?? "";
        if (returnedRef != reference || !Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != "https" || !uri.Host.EndsWith(".monnify.com", StringComparison.OrdinalIgnoreCase))
            throw new ApiFault(502, "PAYMENT_SESSION_INVALID", "Provider returned an invalid payment session.");
        return (url, result.GetProperty("transactionReference").GetString()!);
    }

    public async Task<VerifiedPayment> Verify(string reference, CancellationToken ct)
    {
        var result = await Call(HttpMethod.Get, "/api/v2/merchant/transactions/query?paymentReference=" + Uri.EscapeDataString(reference), null, ct);
        var amount = decimal.Parse(result.GetProperty("amountPaid").ToString(), CultureInfo.InvariantCulture) * 100m;
        if (amount != decimal.Truncate(amount) || amount < 0 || amount > long.MaxValue)
            throw new ApiFault(502, "PAYMENT_AMOUNT_INVALID", "Provider returned an invalid amount.");
        return new VerifiedPayment(result.GetProperty("paymentReference").GetString()!, result.GetProperty("transactionReference").GetString()!,
            result.GetProperty("paymentStatus").GetString()!, (long)amount, result.GetProperty("currency").GetString()!,
            result.GetProperty("customer").GetProperty("email").GetString()!);
    }

    public bool ValidSignature(byte[] body, string? signature)
    {
        if (!Configured || string.IsNullOrWhiteSpace(signature)) return false;
        try
        {
            var expected = HMACSHA512.HashData(Encoding.UTF8.GetBytes(config["Monnify:SecretKey"]!), body);
            return CryptographicOperations.FixedTimeEquals(expected, Convert.FromHexString(signature));
        }
        catch (FormatException) { return false; }
    }

    public async Task<string> ResolveBank(string bankCode, string accountNumber, CancellationToken ct)
    {
        var result = await Call(HttpMethod.Get, $"/api/v1/disbursements/account/validate?accountNumber={Uri.EscapeDataString(accountNumber)}&bankCode={Uri.EscapeDataString(bankCode)}", null, ct);
        return result.GetProperty("accountName").GetString() ?? throw new ApiFault(502, "BANK_LOOKUP_FAILED", "Account name not returned.");
    }
}
