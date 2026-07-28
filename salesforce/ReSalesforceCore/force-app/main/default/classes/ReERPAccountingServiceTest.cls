@IsTest
private class ReERPAccountingServiceTest {

    @IsTest
    static void testCreateJournalEntryForInvoice() {
        reerp__ERP_Company__c comp = new reerp__ERP_Company__c(Name = 'Test Co');
        insert comp;

        reerp__ERP_Invoice__c inv = new reerp__ERP_Invoice__c(
            reerp__Company__c = comp.Id,
            reerp__Grand_Total__c = 2500
        );
        insert inv;

        Test.startTest();
        ReERPAccountingService.createJournalEntryForInvoice(inv);
        Test.stopTest();

        List<reerp__ERP_Journal_Entry__c> entries = [SELECT Id FROM reerp__ERP_Journal_Entry__c WHERE reerp__Company__c = :comp.Id];
        System.assertEquals(1, entries.size(), 'Journal entry should be created.');
    }
}
