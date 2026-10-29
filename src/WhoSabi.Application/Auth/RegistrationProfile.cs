using System.Text.RegularExpressions;

namespace WhoSabi.Application.Auth;

public static partial class RegistrationProfile
{
    public const string CurrentPolicyVersion = "2026-10-29";
    public static string NormalizePhone(string? phone)
    {
        var value = (phone ?? "").Trim().Replace(" ", "").Replace("-", "");
        if (Regex.IsMatch(value, "^0[789][0-9]{9}$")) value = "+234" + value[1..];
        if (!Regex.IsMatch(value, "^\\+[1-9][0-9]{7,14}$"))
            throw new RegistrationInitiationException("INVALID_PHONE", "Use a valid international phone number, for example +2348012345678.");
        return value;
    }
}
