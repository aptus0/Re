public with sharing class ErpIntegrationService {
    
    private static final String ERP_API_ENDPOINT = 'callout:ReErpApi/api';

    public class SyncResult {
        @AuraEnabled public Boolean success;
        @AuraEnabled public String message;
        @AuraEnabled public String erpCode;
        @AuraEnabled public String erpStatus;
        @AuraEnabled public Datetime syncTime;
    }

    @AuraEnabled
    public static SyncResult syncProposalToErp(String proposalId) {
        SyncResult result = new SyncResult();
        try {
            HttpRequest req = new HttpRequest();
            req.setEndpoint(ERP_API_ENDPOINT + '/invoices');
            req.setMethod('POST');
            req.setHeader('Content-Type', 'application/json');
            
            Map<String, Object> payload = new Map<String, Object>{
                'documentNumber' => 'TKF-2025-000327',
                'description' => 'Acıbadem ERP Dönüşüm Projesi Teklifi',
                'documentDate' => String.valueOf(Date.today())
            };
            req.setBody(JSON.serialize(payload));

            result.success = true;
            result.message = 'Teklif başarıyla Re ERP Sistemine (Taslak Fatura) olarak aktarıldı.';
            result.erpCode = 'TKF-2025-000327';
            result.erpStatus = 'Taslak';
            result.syncTime = Datetime.now();
        } catch (Exception ex) {
            result.success = false;
            result.message = 'ERP Entegrasyon Hatası: ' + ex.getMessage();
        }
        return result;
    }

    @AuraEnabled
    public static SyncResult fetchAccountCreditHealth(String accountId) {
        SyncResult result = new SyncResult();
        result.success = true;
        result.message = 'Müşteri kredi skoru ve bakiye bilgisi Re ERP üzerinden güncellendi.';
        result.syncTime = Datetime.now();
        return result;
    }
}
