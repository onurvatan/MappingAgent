# Frontend

This frontend is the Phase 5 workspace for `MappingAgent`.

It provides a local UI for the accounting document ingestion demo:

- scan a folder
- view discovered files
- start parsing
- start agent mapping
- inspect parsed text
- review validation issues and match results
- edit mapped fields in the UI before approval/persistence is added

## Project

- app: [MappingAgent.Web](./MappingAgent.Web)
- framework: React + Vite + TypeScript

## Run

From [Frontend/MappingAgent.Web](./MappingAgent.Web):

```powershell
npm install
npm run dev
```

The Vite dev server runs on `http://localhost:5173`.

## Backend dependency

The frontend expects the backend API to be running locally.

Start the API from the repository root:

```powershell
dotnet run --project Backend\src\MappingAgent.Api\MappingAgent.Api.csproj
```

The frontend uses the Vite proxy in [vite.config.ts](./MappingAgent.Web/vite.config.ts) to forward `/api` calls to the backend.

## Demo flow

Recommended demo folder:

```text
c:\Users\Techp\git\MappingAgent\DemoData\Phase4Sample
```

Typical flow in the UI:

1. enter or keep the demo folder path
2. click `Scan Folder`
3. select the created job
4. click `Parse Job`
5. inspect parsed text and warnings
6. click `Run Agent Map`
7. inspect mapped payload, validation issues, and match results

## Current Phase 5 scope

Implemented:

- folder scan UI
- ingestion job list
- per-file status list
- auto-refresh while parsing or mapping
- parsed text viewer
- validation and parser warning panels
- editable mapped-field form on the client
- approved-records view

Not implemented yet:

- saving edited mapped fields back to the backend
- approve/reject actions
- persisting reviewed changes into EF Core entities

Those belong to Phase 6.

## Agent mapping requirement

`Run Agent Map` depends on backend Foundry/OpenAI configuration.

Configure the backend file:

- [Backend/src/MappingAgent.Api/appsettings.Development.json](../Backend/src/MappingAgent.Api/appsettings.Development.json)

Required settings:

```json
"Foundry": {
  "Endpoint": "<your-endpoint>",
  "Deployment": "<your-chat-deployment>",
  "ApiKey": "<optional-if-using-key-auth>"
}
```

Without that configuration:

- scan works
- parse works
- map fails per file with `AgentNotConfigured`

## Build

From [Frontend/MappingAgent.Web](./MappingAgent.Web):

```powershell
npm run build
```

## Main files

- [App.tsx](./MappingAgent.Web/src/App.tsx)
- [App.css](./MappingAgent.Web/src/App.css)
- [main.tsx](./MappingAgent.Web/src/main.tsx)
- [vite.config.ts](./MappingAgent.Web/vite.config.ts)
