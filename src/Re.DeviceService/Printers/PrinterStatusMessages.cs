namespace Re.DeviceService.Printers;

public static class PrinterStatusMessages
{
    public const string PrinterReady = "Termal yazıcı hazır ve çevrimiçi.";
    public const string PaperOut = "Yazıcı kağıdı bitti. Lütfen yeni termal rulo takın.";
    public const string CoverOpen = "Yazıcı kapağı açık. Lütfen kapağı kapatın.";
    public const string PrintingFailed = "Yazdırma işlemi sırasında hata oluştu. Bağlantıyı kontrol edin.";
    public const string BarcodePrinted = "Barkod etiketi başarıyla yazdırıldı.";
}
