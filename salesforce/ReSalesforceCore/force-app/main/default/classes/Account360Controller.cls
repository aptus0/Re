public with sharing class Account360Controller {

    public class Account360Data {
        @AuraEnabled public String id;
        @AuraEnabled public String name; // ABC MAKİNA SAN. VE TİC. A.Ş.
        @AuraEnabled public String type; // Müşteri
        @AuraEnabled public String sector; // Makine İmalatı
        @AuraEnabled public Integer employees; // 250
        @AuraEnabled public String annualRevenue; // ₺850.000.000
        @AuraEnabled public String segment; // Kurumsal
        @AuraEnabled public String ownerName; // Ayşe Yılmaz

        // Summary
        @AuraEnabled public String address;
        @AuraEnabled public String phone;
        @AuraEnabled public String email;
        @AuraEnabled public String website;

        // Health & Risk
        @AuraEnabled public Integer riskScore; // 62
        @AuraEnabled public String riskLevel; // Yüksek Risk
        @AuraEnabled public String creditLimit; // ₺10.000.000
        @AuraEnabled public String usedLimit; // ₺7.250.000 (%72,5)
        @AuraEnabled public String paymentPerformance; // Orta
        @AuraEnabled public Integer overdueDays; // 36 gün
        @AuraEnabled public String healthTrend; // Negatif
    }

    @AuraEnabled(cacheable=true)
    public static Account360Data getAccount360Data(String recordId) {
        Account360Data data = new Account360Data();
        data.id = recordId != null ? recordId : '0018d00000Y5678';
        data.name = 'ABC MAKİNA SAN. VE TİC. A.Ş.';
        data.type = 'Müşteri';
        data.sector = 'Makine İmalatı';
        data.employees = 250;
        data.annualRevenue = '₺850.000.000';
        data.segment = 'Kurumsal';
        data.ownerName = 'Ayşe Yılmaz';

        data.address = 'OSB 2. Cad. No:12 16140 Nilüfer / Bursa';
        data.phone = '+90 (224) 443 12 12';
        data.email = 'info@abcmakina.com.tr';
        data.website = 'www.abcmakina.com.tr';

        data.riskScore = 62;
        data.riskLevel = 'Yüksek Risk';
        data.creditLimit = '₺10.000.000';
        data.usedLimit = '₺7.250.000 (%72,5)';
        data.paymentPerformance = 'Orta';
        data.overdueDays = 36;
        data.healthTrend = 'Negatif';

        return data;
    }
}
