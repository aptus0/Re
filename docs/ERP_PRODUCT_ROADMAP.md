# Re Modern ERP Product Roadmap

This roadmap turns the product vision into small, testable releases. The target
is not a collection of CRUD screens: every commercial document must update the
next operational and financial step through an auditable workflow.

## Product principles

1. One source of truth: customer, product, stock, document and payment data use
   stable identifiers and tenant isolation.
2. Connected documents: quote -> order -> shipment -> invoice -> collection ->
   cash/bank and accounting movement.
3. Action-oriented screens: every dashboard card must answer a question or open
   the related work queue.
4. Reversible accounting: approved records are never silently edited or deleted;
   cancellation and reversal records preserve the audit trail.
5. Role-aware experience: dashboards, prices, cost, discounts, branches,
   warehouses and fields are restricted by permission.
6. Local-first reliability: SQLite installations remain fast and usable locally,
   while integrations synchronize through idempotent background jobs.

## Delivery phases

### Phase 1 — Executive Dashboard

- Tenant-safe KPI aggregation
- Today's collections and payments
- Cash and bank balances
- Current/previous month sales comparison
- Draft and overdue invoice work queues
- Critical/out-of-stock inventory warnings
- Six-month sales trend, top products and recent activity

Acceptance: all values come from the authenticated company and refresh from one
API request; empty databases display zero-state content without errors.

### Phase 2 — Customer 360 and risk

- Unified movements, invoices, orders, notes, documents and contacts
- Credit limit, open balance, overdue amount and pending-order exposure
- Customer activity timeline and reminders
- One-click invoice, collection, quote and task actions

### Phase 3 — Inventory operations

- Multi-barcode, serial/lot, variants, documents and technical attributes
- Inbound, outbound, transfer, count, waste, return and production movements
- Warehouse transfer approval and movement timeline
- Mobile/barcode counting with difference posting

### Phase 4 — Sales and purchase invoices

- Fast product/barcode search with stock, last price, cost and margin context
- Discount, VAT, withholding, expense, freight and campaign calculations
- Approval -> stock -> customer account -> collection -> cash/bank posting
- PDF, email, e-Invoice/e-Archive adapters and immutable audit trail

### Phase 5 — Order-to-cash

- Quote, sales order, shipment, partial shipment, invoice and collection
- Status machine, approval rules, reservation and backorder handling
- End-to-end document relationship map and timeline

### Phase 6 — Treasury

- Cash, bank, POS, card, loan, expense and income registers
- Collection/payment planning and expected versus actual cash flow
- Cheque/promissory-note lifecycle and maturity alerts
- Currency-aware transfers and reconciliation

### Phase 7 — Procurement

- Request -> supplier quote -> approval -> purchase order -> receipt -> invoice
  -> payment
- Supplier comparison, lead time, landed cost and replenishment suggestions

### Phase 8 — Governance and reporting

- Dynamic filters, drill-down, pivot, Excel/PDF and favourite reports
- Notification and document centres
- Role, branch, warehouse, menu, action and field-level permissions
- Old/new values, user, timestamp, IP, device and correlation audit records

### Phase 9 — Intelligence

- Sales forecast and late-payment risk
- Reorder, critical-stock and smart-price recommendations
- Customer purchasing-pattern and profitability analysis
- Natural-language management summaries with explainable source metrics

### Phase 10 — Expansion

- Production/BOM/work orders and costing
- Human resources, service management and mobile application
- Salesforce synchronization and retail/POS extensions over the same workflow

## Current implementation step

Phase 1 is the active milestone. Later phases must reuse its tenant-safe query
patterns and deep-link work queues instead of creating independent data silos.
