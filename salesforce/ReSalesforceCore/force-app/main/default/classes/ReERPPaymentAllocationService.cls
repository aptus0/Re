public with sharing class ReERPPaymentAllocationService {

    @AuraEnabled
    public static void allocatePayment(Id accountId, Id debitTransId, Id creditTransId, Decimal amount) {
        if (accountId == null || debitTransId == null || creditTransId == null || amount == null || amount <= 0) {
            throw new AuraHandledException('Invalid payment allocation parameters.');
        }

        reerp__ERP_Transaction_Allocation__c alloc = new reerp__ERP_Transaction_Allocation__c(
            reerp__Account__c = accountId,
            reerp__Source_Transaction__c = creditTransId,
            reerp__Target_Transaction__c = debitTransId,
            reerp__Allocated_Amount__c = amount,
            reerp__Allocation_Date__c = System.today()
        );

        insert alloc;
    }
}
