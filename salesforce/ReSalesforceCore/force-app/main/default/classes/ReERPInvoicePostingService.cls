public with sharing class ReERPInvoicePostingService {

    @AuraEnabled
    public static String postInvoice(Id invoiceId) {
        if (invoiceId == null) {
            throw new AuraHandledException('Invoice ID cannot be null.');
        }

        // Fatura ve satırlarını kitleyip çek
        List<reerp__ERP_Invoice__c> invoices = [
            SELECT Id, Name, reerp__Invoice_Number__c, reerp__Company__c, reerp__Branch__c, 
                   reerp__Account__c, reerp__Invoice_Direction__c, reerp__Grand_Total__c, 
                   reerp__Status__c, reerp__Invoice_Date__c
            FROM reerp__ERP_Invoice__c
            WHERE Id = :invoiceId
            LIMIT 1
            FOR UPDATE
        ];

        if (invoices.isEmpty()) {
            throw new AuraHandledException('Invoice not found.');
        }

        reerp__ERP_Invoice__c inv = invoices[0];
        if (inv.reerp__Status__c == 'POSTED') {
            throw new AuraHandledException('Invoice is already posted.');
        }

        // 1. Belge Numarasını Güncelle / Üret
        if (String.isBlank(inv.reerp__Invoice_Number__c)) {
            inv.reerp__Invoice_Number__c = ReERPNumberingService.generateNumber(inv.reerp__Company__c, 'SALES_INVOICE');
        }

        // 2. Durumu POSTED Yap
        inv.reerp__Status__c = 'POSTED';
        inv.reerp__Posted_Date__c = System.now();
        update inv;

        return inv.reerp__Invoice_Number__c;
    }
}
