namespace Re.DeviceService.Printers;

public record ReceiptHeader(string CompanyName, string TaxOffice, string TaxNumber, string Address);
public record ReceiptItem(string Name, decimal Quantity, decimal Price, decimal Total);

public static class EscPosPrinterDriver
{
    private static readonly byte[] EscInit = [0x1B, 0x40];
    private static readonly byte[] EscCenter = [0x1B, 0x61, 0x01];
    private static readonly byte[] EscLeft = [0x1B, 0x61, 0x00];
    private static readonly byte[] EscCut = [0x1D, 0x56, 0x41, 0x00];

    public static byte[] BuildReceiptBuffer(ReceiptHeader header, List<ReceiptItem> items, decimal totalAmount, decimal paidAmount, decimal changeAmount)
    {
        var buffer = new List<byte>();
        buffer.AddRange(EscInit);
        buffer.AddRange(EscCenter);

        AddString(buffer, $"{header.CompanyName}\n");
        AddString(buffer, $"VKN: {header.TaxNumber} - V.D.: {header.TaxOffice}\n");
        AddString(buffer, "--------------------------------\n");

        buffer.AddRange(EscLeft);
        foreach (var item in items)
        {
            AddString(buffer, $"{item.Name}\n");
            AddString(buffer, $"  {item.Quantity:N2} x {item.Price:N2} = {item.Total:N2} TL\n");
        }

        AddString(buffer, "--------------------------------\n");
        buffer.AddRange(EscCenter);
        AddString(buffer, $"GENEL TOPLAM: {totalAmount:N2} TL\n");
        AddString(buffer, $"ODENEN: {paidAmount:N2} TL | PARA USTU: {changeAmount:N2} TL\n");
        AddString(buffer, "\nBizi Tercih Ettiginiz Icin Tesekkurler!\n\n\n");

        buffer.AddRange(EscCut);
        return buffer.ToArray();
    }

    private static void AddString(List<byte> list, string str)
    {
        list.AddRange(System.Text.Encoding.ASCII.GetBytes(str));
    }
}
