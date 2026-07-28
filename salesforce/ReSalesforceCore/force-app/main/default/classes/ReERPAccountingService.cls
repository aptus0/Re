public with sharing class ReERPAccountingService {

    @AuraEnabled
    public static void createJournalEntryForInvoice(reerp__ERP_Invoice__c inv) {
        if (inv == null || inv.Id == null) return;

        reerp__ERP_Journal_Entry__c entry = new reerp__ERP_Journal_Entry__c(
            reerp__Company__c = inv.reerp__Company__c,
            reerp__Branch__c = inv.reerp__Branch__c,
            reerp__Voucher_Type__c = 'Regular',
            reerp__Voucher_Number__c = ReERPNumberingService.generateNumber(inv.reerp__Company__c, 'JOURNAL'),
            reerp__Entry_Date__c = System.today(),
            reerp__Document_Number__c = inv.reerp__Invoice_Number__c,
            reerp__Source_Module__c = 'Invoice',
            reerp__Source_Document_Id__c = inv.Id,
            reerp__Total_Debit__c = inv.reerp__Grand_Total__c,
            reerp__Total_Credit__c = inv.reerp__Grand_Total__c,
            reerp__Status__c = 'Posted',
            reerp__Posted__c = true
        );

        insert entry;
    }
}
