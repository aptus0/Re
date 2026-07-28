# Re Funding Intelligence — Product Plan

## Product position

Re Funding Intelligence is an explainable financing decision and operations
workspace powered by ReSoft ERP data and Salesforce workflow. It is not a clone
of another underwriting product. Its differentiator is the ability to combine
verified invoices, collections, account balances, stock turnover, branch
performance, customer history, uploaded documents, and Salesforce activity.

## Operating principles

- AI assists with extraction, summarization, anomaly detection, and scenarios.
- Deterministic, versioned rules produce the initial eligibility result.
- A human remains responsible for material approval and rejection decisions.
- Every rule, input, override, and decision is written to an audit trail.
- Sensitive documents follow least-privilege access and retention policies.
- Salesforce is the engagement and approval layer; ReSoft is the operational
  and financial source of truth.

## Primary user journey

1. Capture an application from Salesforce, a secure form, email, or API.
2. Resolve the applicant against an ERP account and prevent duplicates.
3. Collect and classify required documents.
4. Calculate verified cash-flow and operating signals from ERP data.
5. Run policy rules and create an explainable risk profile.
6. Route exceptions to an analyst and approvals to the correct authority.
7. Simulate an offer and record the applicant's acceptance.
8. Synchronize the approved offer and repayment plan with ERP and Salesforce.

## Screen roadmap

### Phase 1 — Decision workspace

- Application pipeline and SLA counters
- Applicant 360 summary
- Explainable risk score and policy findings
- ERP cash-flow indicators
- Document completeness and verification
- Offer simulation
- Analyst decision actions and audit timeline

### Phase 2 — Intake and document intelligence

- Secure application form
- Drag-and-drop document inbox
- OCR extraction review
- Duplicate and tamper indicators
- Missing-document automation

### Phase 3 — Policy and portfolio operations

- Versioned rule designer
- Approval authority matrix
- Portfolio concentration and vintage views
- Repayment monitoring and early-warning alerts
- Salesforce Flow and Platform Event synchronization

## Proposed Salesforce model

- `Funding_Application__c`
- `Financial_Assessment__c`
- `Policy_Finding__c`
- `Funding_Offer__c`
- `Verification_Check__c`
- `Payment_Schedule__c`

Recommended relationships: `Account` to applicant, `Opportunity` to commercial
pipeline, `ContentDocument` to evidence, and custom objects to underwriting,
offer, verification, and repayment lifecycle.

## Delivery gates

- Gate 1: UI prototype and domain language validation
- Gate 2: application API and SQLite persistence
- Gate 3: ERP metric calculation and policy engine
- Gate 4: Salesforce metadata, Flow, permissions, and LWC
- Gate 5: OCR provider evaluation and controlled AI rollout
- Gate 6: security review, model validation, UAT, and signed release
