# Product Overview

## Goal

Build a demo application that scans a user-selected folder, ingests accounting documents, classifies them, maps them into a canonical invoice structure, and presents the results in a review UI before saving approved records.

## Product Positioning

The product is a document intake and mapping workspace for accounting operations teams.

It is not a generic "AI understands any document" product. It is a controlled workflow for:

- scanning mixed-format accounting files
- extracting invoice-relevant data
- distinguishing income and expense invoices
- categorizing documents
- matching against existing records
- letting a user correct data before approval

## Target Users

Primary end users:

- finance operations analysts
- accounts payable staff
- accounts receivable staff
- back-office data-entry teams

Primary buyer/demo audience:

- head of finance operations
- controller
- operations systems manager

## Demo Scope

Supported file types:

- PDF
- Excel (`.xlsx`)
- CSV
- TXT

Supported business document classes:

- Expense invoice
- Income invoice
- Credit note
- Unknown accounting document

## Core Demo Promise

Given a folder containing mixed accounting documents, the system will:

1. discover supported files
2. ingest each file
3. classify the document
4. extract accounting fields
5. categorize the record
6. match it against existing data
7. mark the result as completed, needs review, or failed
8. let the user edit and approve
9. save approved records into the final store

## Out of Scope for v1

- full general ledger posting
- bank statements
- receipts
- payroll
- purchase orders
- arbitrary custom schemas
- autonomous save with no review step

## Success Criteria

The demo is successful if a user can:

- choose a folder in the UI
- see all supported files listed before ingestion
- start ingestion and observe live per-file progress
- inspect classification and extracted fields
- see why files failed or need review
- edit incorrect fields
- approve a record and see it persisted
