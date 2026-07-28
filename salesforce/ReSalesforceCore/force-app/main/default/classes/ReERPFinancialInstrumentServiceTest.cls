@IsTest
private class ReERPFinancialInstrumentServiceTest {

    @IsTest
    static void testUpdateInstrumentStatus() {
        reerp__ERP_Company__c comp = new reerp__ERP_Company__c(Name = 'Test Co');
        insert comp;
        reerp__ERP_Financial_Instrument__c inst = new reerp__ERP_Financial_Instrument__c(
            reerp__Current_Status__c = 'Portföyde'
        );
        insert inst;

        Test.startTest();
        ReERPFinancialInstrumentService.updateInstrumentStatus(inst.Id, 'Ciro Edildi', 'Tedarikciye Ciro');
        Test.stopTest();

        reerp__ERP_Financial_Instrument__c updated = [SELECT reerp__Current_Status__c FROM reerp__ERP_Financial_Instrument__c WHERE Id = :inst.Id];
        System.assertEquals('Ciro Edildi', updated.reerp__Current_Status__c, 'Status should be updated.');
    }
}
