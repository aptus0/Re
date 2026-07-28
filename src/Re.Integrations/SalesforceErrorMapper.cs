namespace Re.Integrations;

public static class SalesforceErrorMapper
{
    public static string MapSalesforceErrorCode(string statusCode) => statusCode switch
    {
        "INVALID_SESSION_ID" => "Salesforce oturum süresi doldu. Lütfen tekrar giriş yapın.",
        "NOT_FOUND" => "İstenen Salesforce nesnesi veya kaydı bulunamadı.",
        "DUPLICATE_VALUE" => "Bu kayıt Salesforce üzerinde zaten mevcut (Mükerrer Kayıt).",
        "REQUIRED_FIELD_MISSING" => "Salesforce üzerinde zorunlu alanlar eksik.",
        _ => $"Salesforce Entegrasyon Hatası: {statusCode}"
    };
}
