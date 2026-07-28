public with sharing class ReERPEInvoiceService {

    @AuraEnabled
    public static String generateEInvoiceUuid(Id invoiceId) {
        if (invoiceId == null) throw new AuraHandledException('Invoice ID is required.');

        reerp__ERP_Invoice__c inv = [
            SELECT Id, reerp__E_Invoice_UUID__c 
            FROM reerp__ERP_Invoice__c 
            WHERE Id = :invoiceId 
            LIMIT 1 
            FOR UPDATE
        ];

        if (String.isBlank(inv.reerp__E_Invoice_UUID__c)) {
            inv.reerp__E_Invoice_UUID__c = String.valueOf(EncodingUtil.ConvertToHex(Crypto.GenerateAESKey(128)));
            update inv;
        }

        return inv.reerp__E_Invoice_UUID__c;
    }
}
