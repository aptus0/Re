# Re Connected Commerce Architecture

## Transaction chain

The same identifiers and records flow through every module:

`Customer Account → Product → Warehouse → Invoice → Stock Movement → Account Movement → Cash Collection`

The API is the only transaction boundary. Desktop screens never write directly
to SQLite. This keeps validation, tenant isolation, audit fields, stock effects,
and financial effects consistent for Desktop, Salesforce, and future clients.

## Module responsibilities

- Customer 360 owns identity, credit limit, current balance, invoice exposure,
  and recent account activity.
- Product and Inventory own catalog identity, price, tax, warehouse availability,
  and stock movements.
- Invoice owns the commercial document and line calculations.
- POS orchestrates a sale through existing Invoice and Finance APIs.
- Cash-only mode creates a collection without creating a product sale.
- Funding Intelligence consumes verified operational signals but does not change
  accounting records directly.
- Salesforce owns engagement, approvals, tasks, and synchronized customer context.

## POS modes

### Product sale mode

1. Select a real customer, warehouse, product, payment method, and cash register.
2. Create a draft invoice.
3. Approve the invoice, producing stock and customer account movements.
4. For immediate payment, create a linked collection in the selected cash register.
5. For on-account sales, leave the invoice balance open.

### Cash-only mode

Use for collections where no product, invoice, or stock effect is required.
Customer and cash-register movements are still created together.

## Installation defaults

A fresh SQLite installation automatically creates:

- company
- head-office branch
- default warehouse
- default cash register
- walk-in customer
- administrator role and account

This makes the connected workflow usable immediately while allowing each record
to be replaced or extended during onboarding.

## Next implementation increments

- Database transaction around POS invoice approval and collection
- Stock availability reservation and negative-stock policy
- Card terminal and bank settlement model
- Invoice payment allocation instead of balance-only collection
- Returns, refunds, held carts, shifts, and cash reconciliation
- Salesforce Customer 360 and funding objects synchronized through outbox events
- Permission matrix for cashier, analyst, finance, warehouse, and manager roles
