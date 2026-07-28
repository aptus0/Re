using System.Xml.Linq;

namespace Re.Integrations.EInvoice;

public record UblInvoiceHeader(string UUID, string ID, DateTime IssueDate, string SupplierVkn, string CustomerVkn, decimal PayableAmount);

public static class UblTrInvoiceGenerator
{
    public static string GenerateUblXml(UblInvoiceHeader header)
    {
        XNamespace cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
        XNamespace cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
        XNamespace xmlns = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";

        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", "yes"),
            new XElement(xmlns + "Invoice",
                new XAttribute(XNamespace.Xmlns + "cbc", cbc),
                new XAttribute(XNamespace.Xmlns + "cac", cac),
                new XElement(cbc + "UBLVersionID", "2.1"),
                new XElement(cbc + "CustomizationID", "TR1.2"),
                new XElement(cbc + "ProfileID", "TICARIFATURA"),
                new XElement(cbc + "ID", header.ID),
                new XElement(cbc + "UUID", header.UUID),
                new XElement(cbc + "IssueDate", header.IssueDate.ToString("yyyy-MM-dd")),
                new XElement(cac + "LegalMonetaryTotal",
                    new XElement(cbc + "PayableAmount", new XAttribute("currencyID", "TRY"), header.PayableAmount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture))
                )
            )
        );

        return doc.ToString();
    }
}
