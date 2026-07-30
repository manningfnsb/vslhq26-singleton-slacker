using System.Text.RegularExpressions;

namespace PolicyProof.Services;

public interface IPiiMaskerService
{
    string MaskPii(string text);
}

public partial class PiiMaskerService : IPiiMaskerService
{
    public string MaskPii(string text)
    {
        text = SsnRegex().Replace(text, "[SSN-REDACTED]");
        text = EmailRegex().Replace(text, "[EMAIL-REDACTED]");
        text = PhoneRegex().Replace(text, "[PHONE-REDACTED]");
        text = CreditCardRegex().Replace(text, "[CC-REDACTED]");
        text = ApiKeyRegex().Replace(text, "[SECRET-REDACTED]");
        return text;
    }

    [GeneratedRegex(@"\b\d{3}-\d{2}-\d{4}\b")]
    private static partial Regex SsnRegex();

    [GeneratedRegex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"\b(\+?1[-.\s]?)?\(?\d{3}\)?[-.\s]?\d{3}[-.\s]?\d{4}\b")]
    private static partial Regex PhoneRegex();

    [GeneratedRegex(@"\b\d{4}[-\s]?\d{4}[-\s]?\d{4}[-\s]?\d{4}\b")]
    private static partial Regex CreditCardRegex();

    [GeneratedRegex(@"\b(sk-|api[_-]?key|secret[_-]?key|password)\s*[:=]\s*\S+", RegexOptions.IgnoreCase)]
    private static partial Regex ApiKeyRegex();
}
