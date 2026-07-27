# ReSoft Adaptive CRM & Retail ERP

ReSoft Adaptive CRM & Retail ERP is an integrated retail operations platform that
connects Salesforce Sales Cloud with the ReSoft ERP ecosystem. It gives retail
stores, regional teams, finance departments, warehouse operators, and sales
leaders a shared view of customers, opportunities, quotations, orders, invoices,
inventory, payments, service cases, and operational risk.

The solution combines:

- Salesforce Sales Cloud for customer engagement and commercial execution
- Custom Lightning Web Components for focused retail workspaces
- Salesforce Flow and Validation Rules for controlled automation
- ReSoft ERP services for inventory, invoicing, finance, and store operations
- A Windows desktop application with an embedded, persistent Salesforce workspace
- Secure Salesforce CLI and metadata deployment capabilities

The objective is simple: give every retail employee the right customer and
operational context without forcing them to switch repeatedly between disconnected
systems.

## Salesforce-First Retail Experience

### Retail Customer 360

The Customer 360 workspace combines commercial and operational information in one
Salesforce record experience. A store or account team can review the customer's
profile, contacts, opportunities, orders, invoices, overdue balances, activities,
service cases, credit health, and recommended actions without leaving the account.

![Salesforce Retail Customer 360](docs/images/salesforce-retail-customer-360.png)

The workspace is designed to answer the questions retail teams ask every day:

- Who is this customer and which stores, contacts, or business units belong to it?
- What products, orders, and quotations are currently open?
- How much has the customer purchased and how much remains unpaid?
- Are there overdue invoices or credit-limit risks?
- Which service issues may affect the next sale?
- What is the most appropriate next action for the salesperson?

### Quotation and Approval Workspace

The quotation workspace brings pricing, product lines, discount governance,
approval stages, files, ERP synchronization, and commercial guidance into one
Salesforce screen.

![Salesforce Quotation and Approval Workspace](docs/images/salesforce-quote-approval-workspace.png)

It supports a controlled quote-to-order process:

1. A salesperson creates or revises a quotation.
2. Products, quantities, prices, taxes, and discounts are calculated.
3. Salesforce validation rules prevent incomplete or invalid commercial data.
4. Salesforce Flow starts the appropriate approval path.
5. Regional, finance, or executive approvers review the quotation.
6. Approved commercial data is synchronized with ReSoft ERP.
7. ERP invoice, stock, and payment status become visible in Salesforce.

## Why Salesforce for Retail Stores?

Retail organizations frequently have strong transaction systems but fragmented
customer engagement. Point-of-sale, warehouse, finance, e-commerce, and service
applications may all contain part of the customer story. Salesforce provides the
engagement layer that turns these operational records into coordinated action.

For a retail organization, the Salesforce layer can support:

- Store and branch account hierarchies
- Consumer, corporate, dealer, and franchise customer segments
- Lead and opportunity management
- Store visit and field-sales planning
- Product and campaign-based selling
- Quotations and discount approvals
- Order and invoice visibility
- Loyalty and customer-value segmentation
- Service cases and after-sales processes
- Overdue-payment and credit-risk awareness
- Manager dashboards and sales forecasting
- Agentforce-assisted next-best actions

ReSoft ERP remains the operational source for financial and inventory processes,
while Salesforce becomes the system of engagement for sales, customer service,
approvals, collaboration, and decision support.

## Retail Use Cases

### Store Sales

A salesperson can open the Salesforce customer record before or during a store
interaction and review recent purchases, open quotations, unpaid invoices, service
issues, and relevant contacts. This reduces repeated questions and enables a more
personal, informed sales conversation.

### Regional Sales Management

Regional managers can monitor pipeline quality, quotation discounts, approval
queues, expected revenue, store performance, and customer risk. High-value or
high-discount quotations can be routed automatically for approval.

### Inventory-Aware Selling

Salesforce users can see ERP stock cards, current quantities, reserved inventory,
critical stock levels, warehouse availability, recent movements, and inventory
value. This reduces the risk of selling unavailable products or promising
unrealistic delivery dates.

### Finance and Collections

Invoice totals, collected amounts, remaining balances, due dates, payment status,
and overdue days are available in Salesforce. Account teams can coordinate with
finance before extending additional credit or preparing a new offer.

### Customer Service

Service cases can be reviewed alongside opportunities, orders, and invoices.
Salespeople can identify whether unresolved after-sales issues may affect renewal,
upsell, or customer satisfaction.

### Franchise and Dealer Networks

Account hierarchies can represent headquarters, regions, stores, dealers, and
franchisees. Commercial activity can be reported at both the individual location
and parent-account level.

## Salesforce Solution Architecture

```text
Salesforce Sales Cloud
  ├─ Accounts, Contacts and Opportunities
  ├─ Retail Customer 360 LWC
  ├─ Quotation and Approval LWC
  ├─ Inventory 360 LWC
  ├─ Invoice 360 LWC
  ├─ Record-Triggered Flows
  ├─ Validation Rules
  ├─ Permission Sets
  └─ ReSoft ERP Lightning Application
                 │
                 │ Secure API / Salesforce CLI / Metadata API
                 ▼
ReSoft Integration and Application Services
  ├─ Customer and account synchronization
  ├─ Product and inventory synchronization
  ├─ Quotation and invoice synchronization
  ├─ Deployment and metadata operations
  └─ Integration logging
                 │
                 ▼
ReSoft ERP
  ├─ Stores and branches
  ├─ Warehouses and stock movements
  ├─ Products, variants and barcodes
  ├─ Sales invoices and invoice lines
  ├─ Payments and outstanding balances
  └─ Finance and operational reporting
```

## Salesforce Data Model

The Salesforce metadata package is located in
[`salesforce/ReSalesforceCore`](salesforce/ReSalesforceCore).

### Standard Salesforce Objects

| Object | Retail purpose |
| --- | --- |
| `Account` | Customer, dealer, franchise, corporate buyer, or store hierarchy |
| `Contact` | Customer stakeholder, store manager, purchasing contact, or finance contact |
| `Opportunity` | Sales opportunity, expansion, store project, or commercial initiative |
| `Product2` | Salesforce product catalog representation when standard product capabilities are required |

The Opportunity model includes ERP synchronization and approval fields:

- `ERP_Sync_Id__c`
- `ERP_Approval_Status__c`

### ReSoft ERP Custom Objects

| Salesforce object | Purpose |
| --- | --- |
| `ERP_Warehouse__c` | Physical or virtual warehouse definition |
| `ERP_Stock_Item__c` | Product and warehouse-level inventory card |
| `ERP_Stock_Movement__c` | Inventory receipt, issue, return, transfer, count, waste, or production movement |
| `ERP_Invoice__c` | ERP sales invoice header and payment status |
| `ERP_Invoice_Line__c` | Product, quantity, pricing, discount, and tax details |

### Warehouse

`ERP_Warehouse__c` stores:

- ERP external identifier
- Warehouse code and name
- Description
- Default warehouse indicator
- Active status

### Stock Item

`ERP_Stock_Item__c` stores:

- Product code and barcode
- Warehouse relationship
- Current and reserved quantities
- Minimum and maximum stock thresholds
- Unit of measure
- Unit cost and sale price
- VAT rate
- Stock tracking and negative-stock policies
- ERP synchronization timestamp

### Stock Movement

`ERP_Stock_Movement__c` supports:

- Purchase and sales movements
- Purchase and sales returns
- Warehouse transfers
- Inventory counts
- Waste and loss
- Production receipts
- Lot and serial-number tracking
- Movement date, quantity, cost, and reference document

### Invoice

`ERP_Invoice__c` stores:

- ERP external identifier and document number
- Customer and warehouse relationships
- Invoice and due dates
- Status and approval state
- Subtotal, discount, VAT, total, paid, and remaining amounts
- Currency and exchange rate
- Payment type
- e-Invoice status and UUID
- Overdue days
- Notes and last synchronization timestamp

### Invoice Line

`ERP_Invoice_Line__c` stores:

- Invoice and stock-item relationships
- Product snapshot information
- Quantity and unit
- Unit price and discount
- VAT rate and tax amount
- Net line total and display order

## Salesforce Automation

### ERP Invoice Automation

`ERP_Invoice_Automation` is a record-triggered Flow that:

- Calculates remaining balance
- Calculates overdue days
- Updates the invoice payment state
- Preserves cancelled invoice status
- Runs before save to avoid unnecessary secondary updates

### ERP Stock Balance Automation

`ERP_Stock_Balance_Automation` runs after a stock movement is created. It reads the
related stock card, converts the movement into a signed quantity, and updates the
current stock balance.

### Opportunity ERP Approval

`Opportunity_ERP_Approval_Flow` evaluates the opportunity amount and starts ERP
approval governance for high-value opportunities. The threshold can be adjusted to
match each retailer's authorization matrix.

## Salesforce Validation Rules

The metadata package protects operational quality with rules including:

- Stock movement quantity must be greater than zero
- Minimum stock cannot exceed maximum stock
- Paid invoice amount cannot exceed invoice total
- Invoice due date cannot precede the invoice date
- Invoice-line quantity, price, and discount must remain within valid ranges
- Closed Won opportunities require an ERP synchronization identifier

These rules provide immediate feedback in Salesforce and prevent invalid records
from reaching downstream ERP processes.

## Lightning Web Components

### Inventory 360 Dashboard

`stok360Dashboard` provides:

- Active stock-card count
- Total stock quantity
- Inventory value
- Critical-stock count
- Warehouse-level product table
- Available quantity after reservations
- Recent inventory movements
- Purchasing alerts

### Invoice 360 Detail

`faturaDetail` provides:

- Invoice header and customer context
- Invoice, due-date, warehouse, and ownership information
- Subtotal, VAT, total, paid, and remaining amounts
- Invoice-line table
- Collection progress
- ERP, approval, and e-Invoice states

### Customer and Quotation Components

The project also contains customer and quotation workspaces that demonstrate the
target Salesforce interaction model:

- `musteri360Dashboard`
- `teklifDetail`

The production direction is to replace remaining demonstration data with Salesforce
and ERP-backed queries while preserving the visual hierarchy shown in the reference
screens.

## ReSoft ERP Lightning Application

The `ReSoft ERP` Lightning application provides direct navigation to:

- Home
- Accounts
- Opportunities
- Stock Cards
- Stock Movements
- Warehouses
- Invoices

The `ReSoft ERP User` permission set grants the object, field, tab, application,
and Apex access required by retail users.

## Embedded Salesforce Workspace

The Windows desktop application embeds Salesforce Lightning through WebView2.
The implementation is designed for fast, repeated daily use in retail environments.

### Persistent Session

Salesforce cookies and browser profile data are stored under:

```text
%LocalAppData%\ReSoft\Envanzo\WebView2\Salesforce
```

Users authenticate once and the WebView2 profile preserves the Salesforce session
according to Salesforce's own session policy. Passwords are not stored by the
application.

### Local Cache and Performance

The embedded browser uses:

- A persistent Chromium HTTP cache
- A 512 MB disk-cache allocation for Lightning assets
- A singleton WebView and ViewModel lifecycle
- In-memory reuse when the Salesforce tab is reopened
- A stable download directory
- Protection against repeated frontdoor-login navigation

Downloaded files are stored under:

```text
%LocalAppData%\ReSoft\Envanzo\WebView2\SalesforceDownloads
```

The user can explicitly reset the Salesforce session when changing accounts. Cache
and cookies are never cleared automatically during ordinary navigation.

## Integration Principles

### System of Record

Recommended ownership:

| Data domain | Primary system |
| --- | --- |
| Leads, opportunities, sales activities | Salesforce |
| Customer engagement and service cases | Salesforce |
| Products and warehouse balances | ReSoft ERP |
| Stock movements | ReSoft ERP |
| Invoices and payments | ReSoft ERP |
| Approval and collaboration state | Salesforce |
| Cross-system external identifiers | Both systems |

### External IDs

ERP identifiers are stored in Salesforce external-ID fields. Integrations should use
upsert operations so retries remain idempotent and do not create duplicate records.

### Synchronization

A production integration should:

- Use incremental synchronization rather than full reloads
- Include a correlation identifier in every transaction
- Store source and target timestamps
- Retry transient failures with bounded backoff
- Send failed operations to an integration log or dead-letter process
- Avoid storing Salesforce access tokens in source code
- Respect Salesforce API limits and ERP transaction boundaries

## Security

The project follows these principles:

- Salesforce authentication is delegated to Salesforce OAuth and Salesforce CLI
- User passwords are not persisted by ReSoft
- WebView2 password autosave and general autofill are disabled
- Session cookies remain in the Windows user-specific WebView2 profile
- Permission sets follow least-privilege access
- Apex controllers use Salesforce sharing rules
- Integration credentials should be stored in secure platform configuration
- Production deployments should use dedicated integration users
- Sensitive logs must exclude tokens, passwords, and complete financial payloads

Retail organizations should additionally configure:

- Multi-factor authentication
- Login IP and session policies
- Field-level security for credit and payment information
- Record sharing by region, brand, store, or legal entity
- Salesforce Shield capabilities where regulatory requirements justify them
- Audit retention aligned with company and jurisdictional policies

## Repository Structure

```text
Envanzo/
├─ salesforce/ReSalesforceCore/   Salesforce DX metadata and LWC package
├─ src/Re.Api/                    HTTP API and orchestration endpoints
├─ src/Re.Application/            Application interfaces and use cases
├─ src/Re.Domain/                 ERP domain entities and business rules
├─ src/Re.Infrastructure/         Salesforce and external integrations
├─ src/Re.Persistence/            Entity Framework persistence and migrations
├─ src/Re.Desktop/                Windows desktop and embedded Salesforce UI
├─ src/Re.Contracts/              Shared request and response contracts
├─ tests/                         Automated test projects
└─ docs/images/                   Documentation screenshots
```

## Prerequisites

- Windows 10 or later
- .NET SDK compatible with the solution target framework
- SQL Server for the ERP persistence layer
- Microsoft Edge WebView2 Runtime
- Node.js and npm for LWC tooling
- Salesforce CLI
- A Salesforce Developer, Sandbox, or production organization

## Build the Application

Configure the JWT signing key outside source control. For local PowerShell
development:

```powershell
$env:JwtSettings__SecretKey = "<a-random-secret-of-at-least-32-characters>"
```

Production environments should provide `JwtSettings__SecretKey` through their
secret manager or protected environment configuration.

```powershell
dotnet restore
dotnet build ReERP.slnx
```

Run the API and desktop projects according to the development configuration in the
repository.

## Salesforce Development Setup

```powershell
cd salesforce/ReSalesforceCore
npm install
sf org login web --alias ReOrg1
sf org list
```

Validate the package before deployment:

```powershell
sf project deploy start `
  --manifest manifest/package.xml `
  --target-org ReOrg1 `
  --dry-run `
  --test-level RunSpecifiedTests `
  --tests ErpOperationsControllerTest
```

Deploy:

```powershell
sf project deploy start `
  --manifest manifest/package.xml `
  --target-org ReOrg1 `
  --test-level RunSpecifiedTests `
  --tests ErpOperationsControllerTest
```

Assign the permission set:

```powershell
sf org assign permset `
  --name ReSoft_ERP_User `
  --target-org ReOrg1
```

## Lightning Configuration

After deployment:

1. Open the `ReSoft ERP` Lightning application.
2. Add `Inventory 360 Dashboard` to the ERP Warehouse record page.
3. Add `Invoice 360 Detail` to the ERP Invoice record page.
4. Activate the pages for the required applications and profiles.
5. Assign the `ReSoft ERP User` permission set.
6. Configure account, opportunity, warehouse, inventory, and invoice sharing.
7. Confirm Flow activation and validation-rule behavior in a sandbox.

## Testing

Run Salesforce tests:

```powershell
cd salesforce/ReSalesforceCore
sf apex run test `
  --tests ErpOperationsControllerTest `
  --target-org ReOrg1 `
  --result-format human `
  --wait 20
```

Run LWC checks:

```powershell
npm run lint
npm run test:unit
```

Run .NET tests:

```powershell
dotnet test
```

## Suggested Retail Rollout

### Phase 1 — Foundation

- Connect a Salesforce sandbox
- Confirm account and store hierarchy
- Configure products, warehouses, and invoice mappings
- Deploy permission sets, validation rules, and initial Flows

### Phase 2 — Pilot Stores

- Select a limited number of stores or regional users
- Synchronize representative customers and products
- Validate quotation, approval, inventory, and invoice journeys
- Measure data quality, page performance, and adoption

### Phase 3 — Controlled Expansion

- Expand by region or retail brand
- Introduce manager dashboards and service use cases
- Automate monitoring and integration retries
- Establish support and release-management procedures

### Phase 4 — Intelligence

- Add customer-value and churn indicators
- Introduce guided selling and next-best-action models
- Use Agentforce for approved customer and employee workflows
- Improve forecasting with unified CRM and ERP signals

## Production Readiness Checklist

- [ ] Salesforce sandbox validation completed
- [ ] Data ownership and field mappings approved
- [ ] External-ID and duplicate-management policies configured
- [ ] Permission sets and sharing model reviewed
- [ ] MFA and session controls enabled
- [ ] Flow recursion and bulk behavior tested
- [ ] API limits and synchronization volumes measured
- [ ] Error handling and integration monitoring enabled
- [ ] Backup and recovery processes documented
- [ ] Store-user training completed
- [ ] User acceptance testing signed off
- [ ] Production deployment and rollback plan approved

## Project Direction

ReSoft Adaptive CRM is intended to help retail companies operate Salesforce as a
connected commercial workspace rather than an isolated CRM. Salesforce users gain
the ERP context needed to sell responsibly, while ERP users benefit from cleaner
customer, opportunity, and approval processes.

The result is a practical operating model:

> Salesforce coordinates the customer relationship; ReSoft ERP executes and
> records the retail transaction.
