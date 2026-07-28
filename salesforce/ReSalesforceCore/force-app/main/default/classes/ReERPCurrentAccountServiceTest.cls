@IsTest
private class ReERPCurrentAccountServiceTest {

    @IsTest
    static void testProcessInvoiceCurrentTransaction() {
        reerp__ERP_Company__c comp = new reerp__ERP_Company__c(Name = 'Test Co');
        insert comp;
        Account acc = new Account(Name = 'Customer Acc');
        insert acc;

        reerp__ERP_Invoice__c inv = new reerp__ERP_Invoice__c(
            reerp__Company__c = comp.Id,
            reerp__Account__c = acc.Id,
            reerp__Invoice_Direction__c = 'SALES',
            reerp__Grand_Total__c = 1500
        );
        insert inv;

        Test.startTest();
        ReERPCurrentAccountService.processInvoiceCurrentTransaction(inv);
        Test.stopTest();

        List<reerp__ERP_Current_Transaction__c> txs = [SELECT Id, reerp__Debit_Amount__c FROM reerp__ERP_Current_Transaction__c WHERE reerp__Account__c = :acc.Id];
        System.assertEquals(1, txs.size(), '1 transaction should be posted.');
    }
}
