namespace Re.Infrastructure.Services;

public static class TurkishNotificationTemplates
{
    public static string InvoiceCreatedSms(string invoiceNumber, decimal total) =>
        $"Sayın Müşterimiz, {invoiceNumber} nolu ₺{total:N2} tutarındaki satış faturanız oluşturulmuştur. Re ERP";

    public static string PasswordResetEmail(string username, string resetLink) =>
        $"Sayın {username}, şifrenizi sıfırlamak için şu bağlantıyı kullanabilirsiniz: {resetLink}";

    public static string LowStockAlertEmail(string productName, int currentStock) =>
        $"DİKKAT: {productName} ürününde kritik stok seviyesine ulaşıldı. Mevcut stok: {currentStock}";
}
