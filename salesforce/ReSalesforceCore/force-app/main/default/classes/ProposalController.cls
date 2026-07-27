public with sharing class ProposalController {

    public class ProposalDetail {
        @AuraEnabled public String id;
        @AuraEnabled public String name; // TKF-2025-000327
        @AuraEnabled public String opportunityName;
        @AuraEnabled public String accountName;
        @AuraEnabled public String proposalDate;
        @AuraEnabled public String validityDate;
        @AuraEnabled public String ownerName;
        @AuraEnabled public String status;
        @AuraEnabled public String totalAmount;
        @AuraEnabled public String netAmount;
        @AuraEnabled public String discountRate;
        @AuraEnabled public String version;
        @AuraEnabled public Integer daysRemaining;
        
        // Approval stage
        @AuraEnabled public String currentApprovalStage;
        @AuraEnabled public String assignedApprover;
        @AuraEnabled public String approvalDueDate;
        
        // ERP Entegrasyonu
        @AuraEnabled public String erpStatus;
        @AuraEnabled public String erpLastSync;
        @AuraEnabled public String erpCode;
        @AuraEnabled public String erpState;
        @AuraEnabled public String erpSyncedBy;

        // Line Items
        @AuraEnabled public List<ProposalLineItem> lineItems;
    }

    public class ProposalLineItem {
        @AuraEnabled public Integer id;
        @AuraEnabled public String name;
        @AuraEnabled public String description;
        @AuraEnabled public Decimal quantity;
        @AuraEnabled public String unit;
        @AuraEnabled public Decimal unitPrice;
        @AuraEnabled public Decimal discountPercent;
        @AuraEnabled public Decimal netTotal;
    }

    @AuraEnabled(cacheable=true)
    public static ProposalDetail getProposalDetail(String recordId) {
        ProposalDetail detail = new ProposalDetail();
        detail.id = recordId != null ? recordId : 'a018d00000X1234';
        detail.name = 'TKF-2025-000327';
        detail.opportunityName = 'Acıbadem ERP Dönüşüm Projesi';
        detail.accountName = 'Acıbadem Sağlık Hizmetleri A.Ş.';
        detail.proposalDate = '15.05.2025';
        detail.validityDate = '14.06.2025';
        detail.ownerName = 'Berke Yılmaz';
        detail.status = 'Onay Aşamasında';
        detail.totalAmount = '₺1.207.650,00';
        detail.netAmount = '₺1.006.375,00';
        detail.discountRate = '%12,50';
        detail.version = 'v2.1';
        detail.daysRemaining = 30;

        detail.currentApprovalStage = 'Bölge Müdürü Onayı';
        detail.assignedApprover = 'Murat Demir';
        detail.approvalDueDate = '19.05.2025';

        detail.erpStatus = 'Başarılı';
        detail.erpLastSync = '17.05.2025 10:42';
        detail.erpCode = 'TKF-2025-000327';
        detail.erpState = 'Taslak';
        detail.erpSyncedBy = 'Berke Yılmaz';

        detail.lineItems = new List<ProposalLineItem>();

        ProposalLineItem item1 = new ProposalLineItem();
        item1.id = 1;
        item1.name = 'ReSoft ERP Enterprise';
        item1.description = 'Lisans - 100 Kullanıcı';
        item1.quantity = 100;
        item1.unit = 'Kullanıcı';
        item1.unitPrice = 6000.00;
        item1.discountPercent = 10.00;
        item1.netTotal = 540000.00;
        detail.lineItems.add(item1);

        ProposalLineItem item2 = new ProposalLineItem();
        item2.id = 2;
        item2.name = 'ReSoft ERP Finance Module';
        item2.description = 'Finans Yönetimi Modülü';
        item2.quantity = 1;
        item2.unit = 'Adet';
        item2.unitPrice = 180000.00;
        item2.discountPercent = 15.00;
        item2.netTotal = 153000.00;
        detail.lineItems.add(item2);

        ProposalLineItem item3 = new ProposalLineItem();
        item3.id = 3;
        item3.name = 'ReSoft ERP SCM Module';
        item3.description = 'Tedarik Zinciri Modülü';
        item3.quantity = 1;
        item3.unit = 'Adet';
        item3.unitPrice = 150000.00;
        item3.discountPercent = 15.00;
        item3.netTotal = 127500.00;
        detail.lineItems.add(item3);

        ProposalLineItem item4 = new ProposalLineItem();
        item4.id = 4;
        item4.name = 'Danışmanlık Hizmeti';
        item4.description = 'Kurulum ve Konfigürasyon';
        item4.quantity = 20;
        item4.unit = 'Gün';
        item4.unitPrice = 7500.00;
        item4.discountPercent = 0.00;
        item4.netTotal = 150000.00;
        detail.lineItems.add(item4);

        ProposalLineItem item5 = new ProposalLineItem();
        item5.id = 5;
        item5.name = 'Eğitim Hizmeti';
        item5.description = 'Kullanıcı Eğitimleri';
        item5.quantity = 5;
        item5.unit = 'Gün';
        item5.unitPrice = 4000.00;
        item5.discountPercent = 0.00;
        item5.netTotal = 20000.00;
        detail.lineItems.add(item5);

        return detail;
    }
}
