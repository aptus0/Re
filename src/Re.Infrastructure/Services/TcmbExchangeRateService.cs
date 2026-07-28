using System.Xml.Linq;

namespace Re.Infrastructure.Services;

public record ExchangeRateInfo(string CurrencyCode, decimal BuyingRate, decimal SellingRate, DateTime EffectiveDate);

public interface ITcmbExchangeRateService
{
    Task<List<ExchangeRateInfo>> GetDailyRatesAsync(CancellationToken cancellationToken = default);
}

public sealed class TcmbExchangeRateService : ITcmbExchangeRateService
{
    private readonly HttpClient _httpClient;

    public TcmbExchangeRateService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<ExchangeRateInfo>> GetDailyRatesAsync(CancellationToken cancellationToken = default)
    {
        const string tcmbUrl = "https://www.tcmb.gov.tr/kurlar/today.xml";
        var stream = await _httpClient.GetStreamAsync(tcmbUrl, cancellationToken);
        var doc = XDocument.Load(stream);

        var rates = new List<ExchangeRateInfo>();
        var rootDateStr = doc.Root?.Attribute("Tarih")?.Value;
        var effectiveDate = DateTime.TryParse(rootDateStr, out var d) ? d : DateTime.Today;

        foreach (var currency in doc.Descendants("Currency"))
        {
            var code = currency.Attribute("CurrencyCode")?.Value;
            if (string.IsNullOrWhiteSpace(code)) continue;

            var buyingStr = currency.Element("ForexBuying")?.Value;
            var sellingStr = currency.Element("ForexSelling")?.Value;

            if (decimal.TryParse(buyingStr?.Replace('.', ','), out var buying) &&
                decimal.TryParse(sellingStr?.Replace('.', ','), out var selling))
            {
                rates.Add(new ExchangeRateInfo(code, buying, selling, effectiveDate));
            }
        }

        return rates;
    }
}
