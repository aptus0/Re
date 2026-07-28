@IsTest
private class ReERPTaxCalculationServiceTest {

    @IsTest
    static void testCalculateVat() {
        Test.startTest();
        ReERPTaxCalculationService.TaxResult res1 = ReERPTaxCalculationService.calculateVat(100.0, 20.0, false);
        ReERPTaxCalculationService.TaxResult res2 = ReERPTaxCalculationService.calculateVat(120.0, 20.0, true);
        Test.stopTest();

        System.assertEquals(20.0, res1.vatAmount, 'VAT should be 20.');
        System.assertEquals(100.0, res2.netAmount, 'Net amount should be 100.');
    }
}
