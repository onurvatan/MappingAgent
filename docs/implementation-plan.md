# Implementation Plan

## Phase 1: Project Skeleton

- create backend solution and projects
- create frontend app
- define shared contracts for ingestion status and mapped document payload
- set up local configuration

## Phase 2: Data Model and Persistence

- implement EF Core model for approved accounting documents
- implement NoSQL model for ingestion artifacts
- seed sample counterparties and duplicate-check data

## Phase 3: Folder Scan and Parsing

- implement folder scan endpoint
- discover supported files recursively
- store discovered file metadata
- implement parsers for `pdf`, `xlsx`, `csv`, and `txt`
- normalize parsed output

## Phase 4: Agent Workflow

- implement classification agent
- implement field extraction agent
- implement categorization agent
- implement deterministic validation and matching services
- produce mapped document results with confidence and issues

## Phase 5: UI

- build scan page
- build ingestion monitor with per-file progress
- build review workspace with editable fields
- build saved records view

## Phase 6: Approval and Persistence

- approve mapped document into EF Core entities
- reject or reprocess failed mappings
- display completed saved records

## Phase 7: Demo Hardening

- add sample test documents
- add representative failure cases
- add duplicate detection cases
- add logging and demo-friendly seed data

## Initial Technical Defaults

- backend: ASP.NET Core on .NET 10
- workflow framework: Microsoft Agent Framework
- ingestion store: document-store abstraction, in-memory first, MongoDB later
- approved store: EF Core with SQLite
- realtime updates: polling first, SignalR optional later
- frontend: React

## Coding Order

Recommended implementation order:

1. contracts and entities
2. folder scan APIs
3. persistence
4. parsers
5. agent workflow
6. UI monitor
7. review/edit flow
8. approval/save flow

## Done Criteria

Implementation is done when:

- a folder can be scanned from the UI
- files appear in a job list
- ingestion status updates are visible
- income and expense invoices are classified
- mapped fields can be edited
- duplicate or validation failures are surfaced
- approved records are saved and visible
