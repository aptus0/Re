@IsTest
private class ReERPEInvoiceServiceTest {

    @IsTest
    static void testGenerateEInvoiceUuid() {
        reerp__ERP_Company__c comp = new reerp__ERP_Company__c(Name = 'Test Co');
        insert comp;

        reerp__ERP_Invoice__c inv = new reerp__ERP_Invoice__c(
            reerp__Company__c = comp.Id,
            reerp__Grand_Total__c = 100
        );
        insert inv;

        Test.startTest();
        String uuid = ReERPEInvoiceService.generateEInvoiceUuid(inv.Id);
        Test.stopTest();

        System.assertNotEquals(null, uuid, 'E-Invoice UUID should be generated.');
    }
}
