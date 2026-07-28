public with sharing class ReERPCurrentAccountService {

    @AuraEnabled
    public static void processInvoiceCurrentTransaction(reerp__ERP_Invoice__c inv) {
        if (inv == null || inv.Id == null || inv.reerp__Account__c == null) return;

        Boolean isSales = inv.reerp__Invoice_Direction__c == 'SALES';

        reerp__ERP_Current_Transaction__c trans = new reerp__ERP_Current_Transaction__c(
            reerp__Company__c = inv.reerp__Company__c,
            reerp__Branch__c = inv.reerp__Branch__c,
            reerp__Account__c = inv.reerp__Account__c,
            reerp__Transaction_Date__c = System.today(),
            reerp__Document_Type__c = isSales ? 'Sales Invoice' : 'Purchase Invoice',
            reerp__Document_Id__c = inv.Id,
            reerp__Document_Number__c = inv.reerp__Invoice_Number__c,
            reerp__Transaction_Type__c = isSales ? 'Sales Invoice' : 'Purchase Invoice',
            reerp__Debit_Amount__c = isSales ? inv.reerp__Grand_Total__c : 0,
            reerp__Credit_Amount__c = isSales ? 0 : inv.reerp__Grand_Total__c,
            reerp__Local_Debit_Amount__c = isSales ? inv.reerp__Grand_Total__c : 0,
            reerp__Local_Credit_Amount__c = isSales ? 0 : inv.reerp__Grand_Total__c,
            reerp__Remaining_Amount__c = inv.reerp__Grand_Total__c,
            reerp__Payment_Status__c = 'UNPAID',
            reerp__Posted__c = true
        );

        insert trans;
    }
}
