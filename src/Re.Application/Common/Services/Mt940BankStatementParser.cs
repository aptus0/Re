namespace Re.Application.Common.Services;

public record Mt940StatementLine(
    string Reference,
    DateTime ValueDate,
    string DebitCreditIndicator,
    decimal Amount,
    string Description
);

public static class Mt940BankStatementParser
{
    public static List<Mt940StatementLine> Parse(string fileContent)
    {
        var lines = new List<Mt940StatementLine>();
        if (string.IsNullOrWhiteSpace(fileContent)) return lines;

        var rawLines = fileContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in rawLines)
        {
            if (line.StartsWith(":61:"))
            {
                // SWIFT MT940 Tag :61: Statement Line format parser
                var payload = line.Substring(4);
                if (payload.Length >= 10)
                {
                    var dateStr = payload.Substring(0, 6); // YYMMDD
                    var dc = payload.Substring(6, 1);     // D or C
                    var rest = payload.Substring(7);

                    var commaIndex = rest.IndexOf(',');
                    if (commaIndex > 0 && commaIndex + 3 <= rest.Length)
                    {
                        var amtStr = rest.Substring(0, commaIndex + 3).Replace(',', '.');
                        if (decimal.TryParse(amtStr, System.Globalization.CultureInfo.InvariantCulture, out var amt))
                        {
                            lines.Add(new Mt940StatementLine("REF-" + dateStr, DateTime.Today, dc, amt, rest));
                        }
                    }
                }
            }
        }

        return lines;
    }
}
