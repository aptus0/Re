public with sharing class ReERPFinancialInstrumentService {

    @AuraEnabled
    public static void updateInstrumentStatus(Id instrumentId, String newStatus, String description) {
        if (instrumentId == null || String.isBlank(newStatus)) {
            throw new AuraHandledException('Instrument ID and New Status are required.');
        }

        reerp__ERP_Financial_Instrument__c inst = [
            SELECT Id, reerp__Current_Status__c, reerp__Account__c 
            FROM reerp__ERP_Financial_Instrument__c 
            WHERE Id = :instrumentId 
            FOR UPDATE
        ];

        String prevStatus = inst.reerp__Current_Status__c;
        inst.reerp__Current_Status__c = newStatus;
        update inst;

        reerp__ERP_Financial_Instrument_History__c hist = new reerp__ERP_Financial_Instrument_History__c(
            reerp__Financial_Instrument__c = inst.Id,
            reerp__Transaction_Date__c = System.today(),
            reerp__Previous_Status__c = prevStatus,
            reerp__New_Status__c = newStatus,
            reerp__Description__c = description
        );
        insert hist;
    }
}
