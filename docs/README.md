# MappingAgent Docs

This folder defines the product and implementation baseline for the accounting document ingestion demo.

Document set:

- `product-overview.md`: product framing, users, and success criteria
- `workflow.md`: end-to-end ingestion workflow and file lifecycle
- `domain-model.md`: canonical accounting model, statuses, and validation rules
- `architecture.md`: backend, agent, storage, and UI architecture
- `implementation-plan.md`: coding sequence and delivery slices

The demo scope is intentionally narrow:

- folder-based ingestion
- supported file types: `pdf`, `xlsx`, `csv`, `txt`
- accounting focus: income and expense invoices
- user review and edit before final approval
- raw ingestion state stored separately from approved accounting records
