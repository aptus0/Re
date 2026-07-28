# 🛡️ Re ERP for Salesforce — Second-Generation Managed Package (2GP) Architecture

## 1. Package Specifications
- **Package Name:** Re ERP for Salesforce
- **Namespace:** `reerp`
- **Distribution Model:** Managed 2GP (Second Generation Package)
- **API Version:** 66.0
- **Post-Install Script:** `ReERPInstallHandler`

## 2. Core Custom Objects
- `reerp__ERP_Company__c`: Multi-tenant Company & Legal Entity management
- `reerp__ERP_Branch__c`: Branch & Store hierarchy
- `reerp__ERP_Warehouse__c`: Physical & Virtual warehouse locations
- `reerp__ERP_Invoice__c`: Sales and Purchase Invoice header with E-Invoice UUID
- `reerp__ERP_Stock_Balance__c`: Real-time stock quantity tracking per warehouse
- `reerp__ERP_Current_Transaction__c`: Customer & Supplier Ledger Debit/Credit movements
- `reerp__ERP_Financial_Instrument__c`: Cheques and Promissory Notes lifecycle tracking

## 3. Apex Engine Services
- `ReERPInstallHandler.cls`: Automated permission assignment and seed configuration.
- `ReERPInvoicePostingService.cls`: Document posting engine.
- `ReERPStockService.cls`: Automatic inventory movement and balance update.
- `ReERPCurrentAccountService.cls`: Ledger transaction posting.
- `ReERPAccountingService.cls`: Automatic balanced Journal Entry creation.
- `ReERPNumberingService.cls`: Document number generator.
- `ReERPPaymentAllocationService.cls`: Payment allocation & invoice closing logic.
- `ReERPFinancialInstrumentService.cls`: Cheque/Note endorsement & collection.

## 4. Lightning Web Components (LWCs)
- `reerpSetupWizard`: Post-install onboarding & setup wizard.
- `reerpHomeDashboard`: Executive dashboard with financial KPIs.
- `reerpInvoiceEditor`: Quick invoice entry line item calculator.
- `reerpCheckLifecycle`: Visual portfolio cheque lifecycle manager.
- `reerpPaymentAllocation`: Payment-to-Invoice allocation card.
