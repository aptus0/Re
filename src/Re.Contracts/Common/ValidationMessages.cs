namespace Re.Contracts.Common;

public static class ValidationMessages
{
    public const string UsernameRequired = "Kullanıcı adı alanı boş bırakılamaz.";
    public const string PasswordRequired = "Giriş şifresi boş bırakılamaz.";
    public const string AccountCodeRequired = "Cari hesap kodu zorunludur.";
    public const string AccountNameRequired = "Cari ünvan / isim alanı zorunludur.";
    public const string ProductCodeRequired = "Ürün kodu zorunludur.";
    public const string ProductNameRequired = "Ürün adı zorunludur.";
    public const string AmountGreaterThanZero = "Tutar 0'dan büyük bir değer olmalıdır.";
    public const string InvalidTaxNumber = "Vergi veya T.C. kimlik numarası geçersizdir.";
    public const string ConnectionError = "API sunucusu ile iletişim sağlanamadı.";
    public const string UnauthorizedAccess = "Bu işlemi gerçekleştirmek için yetkiniz bulunmamaktadır.";
}
