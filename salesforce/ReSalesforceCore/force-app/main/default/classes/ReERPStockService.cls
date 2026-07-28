public with sharing class ReERPStockService {

    @AuraEnabled
    public static void processInvoiceStockMovement(reerp__ERP_Invoice__c inv) {
        if (inv == null || inv.Id == null) return;

        // Fatura satırlarını getir
        List<reerp__ERP_Invoice_Line__c> lines = [
            SELECT Id, reerp__Product__c, reerp__Product_Variant__c, reerp__Warehouse__c, 
                   reerp__Quantity__c, reerp__UnitPrice__c, reerp__Total_Amount__c
            FROM reerp__ERP_Invoice_Line__c
            WHERE reerp__Invoice__c = :inv.Id
        ];

        if (lines.isEmpty()) return;

        // Stok Hareket Başlığı Oluştur
        reerp__ERP_Stock_Movement__c movement = new reerp__ERP_Stock_Movement__c(
            reerp__Movement_Number__c = ReERPNumberingService.generateNumber(inv.reerp__Company__c, 'STOCK_MOV'),
            reerp__Movement_Type__c = inv.reerp__Invoice_Direction__c == 'SALES' ? 'Sales Out' : 'Purchase In',
            reerp__Movement_Date__c = System.today(),
            reerp__Company__c = inv.reerp__Company__c,
            reerp__Branch__c = inv.reerp__Branch__c,
            reerp__Account__c = inv.reerp__Account__c,
            reerp__Status__c = 'Posted',
            reerp__Posted__c = true,
            reerp__Posted_Date__c = System.now()
        );
        insert movement;

        List<reerp__ERP_Stock_Movement_Line__c> movementLines = new List<reerp__ERP_Stock_Movement_Line__c>();
        for (reerp__ERP_Invoice_Line__c line : lines) {
            if (line.reerp__Product__c != null) {
                reerp__ERP_Stock_Movement_Line__c mLine = new reerp__ERP_Stock_Movement_Line__c(
                    reerp__Stock_Movement__c = movement.Id,
                    reerp__Product__c = line.reerp__Product__c,
                    reerp__Product_Variant__c = line.reerp__Product_Variant__c,
                    reerp__Warehouse__c = line.reerp__Warehouse__c != null ? line.reerp__Warehouse__c : inv.reerp__Warehouse__c,
                    reerp__Quantity_In__c = inv.reerp__Invoice_Direction__c == 'PURCHASE' ? line.reerp__Quantity__c : 0,
                    reerp__Quantity_Out__c = inv.reerp__Invoice_Direction__c == 'SALES' ? line.reerp__Quantity__c : 0,
                    reerp__Unit_Cost__c = line.reerp__UnitPrice__c,
                    reerp__Total_Cost__c = line.reerp__Total_Amount__c
                );
                movementLines.add(mLine);
            }
        }

        if (!movementLines.isEmpty()) {
            insert movementLines;
        }
    }
}
