namespace Re.Application.Common.Services;

public record TaxCalculationResult(
    decimal NetAmount,
    decimal VatAmount,
    decimal WithholdingAmount,
    decimal TotalAmount
);

public static class TaxCalculationEngine
{
    public static TaxCalculationResult Calculate(decimal grossOrNet, decimal vatRatePercent, decimal withholdingFraction = 0, bool isGrossInput = false)
    {
        decimal vatRate = vatRatePercent / 100m;
        decimal netAmount;
        decimal vatAmount;

        if (isGrossInput)
        {
            netAmount = Math.Round(grossOrNet / (1m + vatRate), 4, MidpointRounding.AwayFromZero);
            vatAmount = Math.Round(grossOrNet - netAmount, 4, MidpointRounding.AwayFromZero);
        }
        else
        {
            netAmount = Math.Round(grossOrNet, 4, MidpointRounding.AwayFromZero);
            vatAmount = Math.Round(netAmount * vatRate, 4, MidpointRounding.AwayFromZero);
        }

        decimal withholdingAmount = 0;
        if (withholdingFraction > 0 && withholdingFraction <= 1)
        {
            withholdingAmount = Math.Round(vatAmount * withholdingFraction, 4, MidpointRounding.AwayFromZero);
        }

        decimal totalAmount = Math.Round(netAmount + vatAmount - withholdingAmount, 4, MidpointRounding.AwayFromZero);
        return new TaxCalculationResult(netAmount, vatAmount, withholdingAmount, totalAmount);
    }
}
