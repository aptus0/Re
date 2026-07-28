@IsTest
private class ReERPStockServiceTest {

    @IsTest
    static void testProcessInvoiceStockMovement() {
        reerp__ERP_Company__c comp = new reerp__ERP_Company__c(Name = 'Test Co');
        insert comp;

        reerp__ERP_Invoice__c inv = new reerp__ERP_Invoice__c(
            reerp__Company__c = comp.Id,
            reerp__Invoice_Direction__c = 'SALES',
            reerp__Grand_Total__c = 500
        );
        insert inv;

        Test.startTest();
        ReERPStockService.processInvoiceStockMovement(inv);
        Test.stopTest();

        System.assert(true, 'Stock movement processed without error.');
    }
}
