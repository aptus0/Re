namespace Re.Domain.ValueObjects;

/// <summary>
/// Para tutarı ve para birimi. Değer nesnesi – değiştirilemez.
/// </summary>
public sealed record Money(decimal Amount, string Currency = "TRY")
{
    public static Money Zero(string currency = "TRY") => new(0, currency);

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return this with { Amount = Amount + other.Amount };
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return this with { Amount = Amount - other.Amount };
    }

    public Money Multiply(decimal factor) => this with { Amount = Amount * factor };

    public Money ApplyDiscount(decimal discountPercent)
    {
        if (discountPercent < 0 || discountPercent > 100)
            throw new ArgumentOutOfRangeException(nameof(discountPercent), "İndirim oranı 0-100 arasında olmalıdır.");
        return this with { Amount = Amount * (1 - discountPercent / 100) };
    }

    public override string ToString() => $"{Amount:N2} {Currency}";

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Para birimi uyuşmuyor: {Currency} ≠ {other.Currency}");
    }
}

/// <summary>
/// Adres değer nesnesi.
/// </summary>
public sealed record Address(
    string Line1,
    string? Line2,
    string City,
    string? District,
    string? PostalCode,
    string Country = "Türkiye")
{
    public override string ToString() =>
        string.Join(", ", new[] { Line1, Line2, District, City, PostalCode, Country }
            .Where(x => !string.IsNullOrWhiteSpace(x)));
}

/// <summary>
/// Vergi kimlik numarası – TCKN veya VKN.
/// </summary>
public sealed record TaxNumber
{
    public string Value { get; }
    public bool IsIndividual => Value.Length == 11;

    public TaxNumber(string value)
    {
        var digits = value.Trim().Replace(" ", "");
        if (digits.Length != 10 && digits.Length != 11)
            throw new ArgumentException("Vergi numarası 10 (VKN) veya 11 (TCKN) haneli olmalıdır.");
        if (!digits.All(char.IsDigit))
            throw new ArgumentException("Vergi numarası yalnızca rakamlardan oluşmalıdır.");
        Value = digits;
    }

    public override string ToString() => Value;
}

