@IsTest
private class ReERPInvoicePostingServiceTest {

    @IsTest
    static void testPostInvoiceExecution() {
        reerp__ERP_Company__c company = new reerp__ERP_Company__c(
            Name = 'Test Company',
            reerp__Legal_Name__c = 'Test Legal Name',
            reerp__Tax_Number__c = '1111111111'
        );
        insert company;

        reerp__ERP_Branch__c branch = new reerp__ERP_Branch__c(
            Name = 'Merkez Sube',
            reerp__Company__c = company.Id
        );
        insert branch;

        Account acc = new Account(
            Name = 'Test Customer',
            reerp__Tax_Number__c = '1234567890'
        );
        insert acc;

        reerp__ERP_Invoice__c inv = new reerp__ERP_Invoice__c(
            reerp__Company__c = company.Id,
            reerp__Branch__c = branch.Id,
            reerp__Account__c = acc.Id,
            reerp__Invoice_Direction__c = 'SALES',
            reerp__Grand_Total__c = 1250.75,
            reerp__Status__c = 'Draft'
        );
        insert inv;

        Test.startTest();
        String docNumber = ReERPInvoicePostingService.postInvoice(inv.Id);
        Test.stopTest();

        System.assertNotEquals(null, docNumber, 'Document number should be generated.');
        reerp__ERP_Invoice__c postedInv = [SELECT reerp__Status__c FROM reerp__ERP_Invoice__c WHERE Id = :inv.Id];
        System.assertEquals('POSTED', postedInv.reerp__Status__c, 'Invoice status should be POSTED.');
    }
}
