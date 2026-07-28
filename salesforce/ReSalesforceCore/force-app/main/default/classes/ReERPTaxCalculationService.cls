public with sharing class ReERPTaxCalculationService {

    public class TaxResult {
        @AuraEnabled public Decimal netAmount;
        @AuraEnabled public Decimal vatAmount;
        @AuraEnabled public Decimal totalAmount;
    }

    @AuraEnabled
    public static TaxResult calculateVat(Decimal grossOrNet, Decimal vatRate, Boolean isGross) {
        TaxResult res = new TaxResult();
        Decimal rate = vatRate / 100.0;
        if (isGross) {
            res.netAmount = (grossOrNet / (1.0 + rate)).setScale(2, System.RoundingMode.HALF_UP);
            res.vatAmount = (grossOrNet - res.netAmount).setScale(2, System.RoundingMode.HALF_UP);
            res.totalAmount = grossOrNet;
        } else {
            res.netAmount = grossOrNet;
            res.vatAmount = (grossOrNet * rate).setScale(2, System.RoundingMode.HALF_UP);
            res.totalAmount = res.netAmount + res.vatAmount;
        }
        return res;
    }
}
