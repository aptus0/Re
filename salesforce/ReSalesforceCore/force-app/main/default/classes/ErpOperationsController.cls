public with sharing class ErpOperationsController {
    public class InventorySummary {
        @AuraEnabled public Decimal totalQuantity;
        @AuraEnabled public Decimal totalValue;
        @AuraEnabled public Integer itemCount;
        @AuraEnabled public Integer lowStockCount;
        @AuraEnabled public List<ERP_Stock_Item__c> items;
        @AuraEnabled public List<ERP_Stock_Movement__c> movements;
    }

    public class InvoiceWorkspace {
        @AuraEnabled public ERP_Invoice__c invoice;
        @AuraEnabled public List<ERP_Invoice_Line__c> lines;
        @AuraEnabled public Integer daysToDue;
        @AuraEnabled public Decimal collectionRate;
    }

    @AuraEnabled(cacheable=true)
    public static InventorySummary getInventorySummary(Id warehouseId) {
        InventorySummary result = new InventorySummary();
        String itemQuery =
            'SELECT Id, Name, Product_Code__c, Barcode__c, Current_Stock__c, Reserved_Stock__c, ' +
            'Minimum_Stock__c, Maximum_Stock__c, Unit__c, Unit_Cost__c, Sale_Price__c, ' +
            'Warehouse__c, Warehouse__r.Name, Is_Active__c, Last_Sync_At__c ' +
            'FROM ERP_Stock_Item__c WHERE Is_Active__c = true';
        if (warehouseId != null) {
            itemQuery += ' AND Warehouse__c = :warehouseId';
        }
        itemQuery += ' ORDER BY Current_Stock__c ASC NULLS FIRST LIMIT 100';
        result.items = Database.query(itemQuery);

        Set<Id> itemIds = new Map<Id, ERP_Stock_Item__c>(result.items).keySet();
        result.movements = itemIds.isEmpty()
            ? new List<ERP_Stock_Movement__c>()
            : [
                SELECT Id, Name, Stock_Item__c, Stock_Item__r.Name, Movement_Type__c,
                    Direction__c, Quantity__c, Unit_Cost__c, Movement_Date__c,
                    Reference_Document__c
                FROM ERP_Stock_Movement__c
                WHERE Stock_Item__c IN :itemIds
                ORDER BY Movement_Date__c DESC
                LIMIT 20
            ];

        result.itemCount = result.items.size();
        result.lowStockCount = 0;
        result.totalQuantity = 0;
        result.totalValue = 0;
        for (ERP_Stock_Item__c item : result.items) {
            Decimal quantity = item.Current_Stock__c == null ? 0 : item.Current_Stock__c;
            Decimal cost = item.Unit_Cost__c == null ? 0 : item.Unit_Cost__c;
            result.totalQuantity += quantity;
            result.totalValue += quantity * cost;
            if (item.Minimum_Stock__c != null && quantity <= item.Minimum_Stock__c) {
                result.lowStockCount++;
            }
        }
        return result;
    }

    @AuraEnabled(cacheable=true)
    public static InvoiceWorkspace getInvoiceWorkspace(Id invoiceId) {
        if (invoiceId == null) {
            throw new AuraHandledException('Fatura kayıt kimliği gereklidir.');
        }
        InvoiceWorkspace result = new InvoiceWorkspace();
        result.invoice = [
            SELECT Id, Name, Document_Number__c, Account__c, Account__r.Name,
                Warehouse__c, Warehouse__r.Name, Status__c, Document_Date__c,
                Due_Date__c, Sub_Total__c, Discount_Amount__c, Tax_Amount__c,
                Total_Amount__c, Paid_Amount__c, Remaining_Amount__c,
                Currency_Code__c, Payment_Type__c, E_Invoice_Status__c,
                Approval_Status__c, Overdue_Days__c, Notes__c, Last_Sync_At__c,
                Owner.Name
            FROM ERP_Invoice__c
            WHERE Id = :invoiceId
            LIMIT 1
        ];
        result.lines = [
            SELECT Id, Name, Product_Code__c, Product_Name__c, Quantity__c,
                Unit__c, Unit_Price__c, Discount_Percent__c, Vat_Rate__c,
                Line_Total__c, Tax_Amount__c, Sort_Order__c
            FROM ERP_Invoice_Line__c
            WHERE Invoice__c = :invoiceId
            ORDER BY Sort_Order__c ASC NULLS LAST, CreatedDate ASC
        ];
        result.daysToDue = result.invoice.Due_Date__c == null
            ? null
            : Date.today().daysBetween(result.invoice.Due_Date__c);
        Decimal total = result.invoice.Total_Amount__c == null ? 0 : result.invoice.Total_Amount__c;
        Decimal paid = result.invoice.Paid_Amount__c == null ? 0 : result.invoice.Paid_Amount__c;
        result.collectionRate = total == 0 ? 0 : (paid / total) * 100;
        return result;
    }
}
