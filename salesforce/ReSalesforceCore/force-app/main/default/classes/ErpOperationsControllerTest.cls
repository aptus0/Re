@IsTest
private class ErpOperationsControllerTest {
    @TestSetup
    static void setupData() {
        ERP_Warehouse__c warehouse = new ERP_Warehouse__c(
            Name = 'Merkez Depo', Code__c = 'MRK', Is_Active__c = true
        );
        insert warehouse;
        ERP_Stock_Item__c item = new ERP_Stock_Item__c(
            Name = 'CNC Kontrol Ünitesi', Product_Code__c = 'CNC-001',
            Warehouse__c = warehouse.Id, Current_Stock__c = 0,
            Minimum_Stock__c = 5, Unit_Cost__c = 1000, Is_Active__c = true
        );
        insert item;
        insert new ERP_Stock_Movement__c(
            Stock_Item__c = item.Id, Movement_Type__c = 'Alis',
            Direction__c = 'Giris', Quantity__c = 4, Movement_Date__c = System.now()
        );
        Account account = new Account(Name = 'Test Müşteri');
        insert account;
        ERP_Invoice__c invoice = new ERP_Invoice__c(
            Document_Number__c = 'FTR-TEST-1', Account__c = account.Id,
            Status__c = 'Taslak', Document_Date__c = Date.today(),
            Due_Date__c = Date.today().addDays(10), Total_Amount__c = 1200,
            Paid_Amount__c = 200, Remaining_Amount__c = 1000
        );
        insert invoice;
        insert new ERP_Invoice_Line__c(
            Invoice__c = invoice.Id, Product_Name__c = 'Test Ürün',
            Quantity__c = 1, Unit_Price__c = 1000, Line_Total__c = 1000
        );
    }

    @IsTest
    static void returnsInventorySummary() {
        Id warehouseId = [SELECT Warehouse__c FROM ERP_Stock_Item__c LIMIT 1].Warehouse__c;
        Test.startTest();
        ErpOperationsController.InventorySummary result =
            ErpOperationsController.getInventorySummary(warehouseId);
        Test.stopTest();
        System.assertEquals(1, result.itemCount);
        System.assertEquals(1, result.lowStockCount);
        System.assertEquals(4000, result.totalValue);
    }

    @IsTest
    static void returnsInvoiceWorkspace() {
        Id invoiceId = [SELECT Id FROM ERP_Invoice__c LIMIT 1].Id;
        Test.startTest();
        ErpOperationsController.InvoiceWorkspace result =
            ErpOperationsController.getInvoiceWorkspace(invoiceId);
        Test.stopTest();
        System.assertEquals(1, result.lines.size());
        System.assertEquals(10, result.daysToDue);
        System.assertEquals(1000, result.invoice.Remaining_Amount__c);
    }
}
