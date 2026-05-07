# Digital Twin Backend Handoff

## Purpose
This document is a handoff/context file for continuing development of the `basic-makerspace-digital-twin-backend` in a fresh chat without losing architecture and implementation context.

## Repository shape
Project root:
- `BasicMakerspaceDigitalTwinBackend.sln`
- `Dockerfile`
- `.dockerignore`
- `src/`
  - `DigitalTwin.Api`
  - `DigitalTwin.Application`
  - `DigitalTwin.Domain`
  - `DigitalTwin.Infrastructure`

### Layer responsibilities
- `DigitalTwin.Api`
  - controllers
  - hosted workers
  - appsettings / Program.cs
- `DigitalTwin.Application`
  - DTOs
  - abstractions/interfaces
  - non-EF models used across services
- `DigitalTwin.Domain`
  - persistent EF entities only
- `DigitalTwin.Infrastructure`
  - EF DbContext/configurations
  - external integrations (Bambu proxy, Influx)
  - query/read services
  - simulation services
  - scheduling services

## Main databases/services
- PostgreSQL: main relational DB (hosted on another PC)
- Redis: hosted on another PC, optional right now
- InfluxDB: hosted on another PC, used for time-series telemetry
- Bambu proxy: external service for bind/version/tasks/messages sync

## Important runtime port
- Backend runs on port `5017`

## Major implemented features

### 1. Printer catalog and metadata
Main tables / concepts:
- `printers`
- `printer_firmware_statuses`
- `printer_ams_units`

Bind/version sync exists and separates:
- basic printer metadata
- firmware/version metadata
- AMS unit data

Notes:
- `printer_ams_slots` was removed from the design
- AMS is stored at unit level for now

### 2. Printer activity
Main tables:
- `printer_tasks`
- `printer_task_ams_details`
- `printer_messages`

Meaning:
- `printer_tasks` = execution records
- `printer_task_ams_details` = per-task AMS/material details
- `printer_messages` = event/history/notification style records tied to printer/task

### 3. Simulation system
Simulation allows fake start/stop of printers.

Rules:
- Start only if printer is online
- Start only if `PrintStatus` is one of: `ACTIVE`, `SUCCESS`, `FAIL`
- Stop only if printer is online and currently `RUNNING`
- Stop marks the active simulated task as failed
- Auto-completion marks simulated task as success

Simulation creates rich synthetic rows in:
- `printer_tasks`
- `printer_task_ams_details`
- `printer_messages`

It generates logical fake values such as:
- fake design title / file-derived title
- duration
- filament usage
- AMS colors/materials
- message titles/details

### 4. Simulation lock design
To avoid conflict between simulation and sync workers, simulation control is stored separately.

Table:
- `printer_simulation_controls`

Entity:
- `PrinterSimulationControl`

Meaning:
- tracks whether printer is currently simulation-controlled
- tracks lock expiration
- tracks local simulation state (`RUNNING`, `FAIL`, `SUCCESS`)
- tracks active simulated task id

This is intentionally NOT stored directly as raw upstream printer data.

### 5. Telemetry system
For simulated running printers:
- random but logical telemetry points are generated every 2–5 seconds
- written to InfluxDB
- pushed to frontend through SSE

Important route:
- `GET /api/printers/{deviceId}/telemetry/stream`

Frontend should open it once and keep the connection open. It should not poll every few seconds.

Historical route exists / planned via telemetry query endpoint.

Task summary endpoint exists / planned logic:
- latest progress/layer from latest telemetry point
- average temperatures across task time range
- handle edge case where no Influx telemetry exists

### 6. Scheduling agent
Users can create fake scheduled print jobs.

Main table:
- `scheduled_print_jobs`

Entity:
- `ScheduledPrintJob`

Main statuses:
- `QUEUED`
- `RUNNING`
- `COMPLETED`
- `FAILED`
- `CANCELLED`

Main rules:
- user provides fake file name
- can target a preferred printer or allow any printer
- can set priority
- system generates logical estimated metadata such as duration, material, color, filament grams

Important distinction:
- `scheduled_print_jobs` = user queue / scheduler requests
- `printer_tasks` = actual simulated execution records

### 7. Scheduler agent behavior
Core service:
- `PrintSchedulerService`

Core worker:
- `PrintSchedulingWorker`

Key scheduler actions:
- `run-dispatch-cycle` = try to assign/start queued jobs now
- `reconcile` = update already running scheduled jobs based on underlying `printer_tasks`

Expected lifecycle:
- queued job created in `scheduled_print_jobs`
- scheduler selects best available printer
- scheduler calls `PrinterSimulationService.StartPrinterAsync(...)`
- scheduled job becomes `RUNNING`
- when underlying task finishes successfully => scheduled job becomes `COMPLETED`
- when underlying task fails/stops => scheduled job becomes `FAILED`

Scheduler should NOT remove jobs from `scheduled_print_jobs`; jobs remain as history.

### 8. Explainability / scheduler control
Part E added lightweight explainability.

Additional fields on scheduled jobs:
- `SchedulerDecisionReason`
- `CompatibilityNote`
- `EstimatedStartAtUtc`
- `EstimatedFinishAtUtc`

Scheduler control table/entity:
- `scheduler_controls`
- `SchedulerControl`

Supports:
- pause scheduler
- resume scheduler
- keep pause reason

## Important controllers/routes already discussed

### Printers
- `GET /api/printers`
- `GET /api/printers/{deviceId}`
- `GET /api/printers/by-name?name={name}`
- `GET /api/printers/running` (should return currently running printers)
- `GET /api/printers/{deviceId}/firmware`
- `GET /api/printers/by-name/firmware?name={name}`
- `GET /api/printers/{deviceId}/ams-units`
- `GET /api/printers/by-name/ams-units?name={name}`
- `GET /api/printers/{deviceId}/tasks`
- `GET /api/printers/by-name/tasks?name={name}`
- `GET /api/printers/{deviceId}/messages`
- `GET /api/printers/by-name/messages?name={name}`
- `GET /api/printers/{deviceId}/timeline`
- `GET /api/printers/by-name/timeline?name={name}`
- `GET /api/printers/{deviceId}/loaded-spools`
- `GET /api/printers/by-name/loaded-spools?name={name}`
- `GET /api/printers/{deviceId}/replacement-suggestions`
- `GET /api/printers/by-name/replacement-suggestions?name={name}`
- `PUT /api/printers/{deviceId}/loaded-spools`
- `PUT /api/printers/by-name/loaded-spools?name={name}`

### Simulation
- `POST /api/printers/{deviceId}/simulate/start`
- `POST /api/printers/{deviceId}/simulate/stop`
- `POST /api/printers/by-name/simulate/start?name={name}`
- `POST /api/printers/by-name/simulate/stop?name={name}`

### Telemetry
- `GET /api/printers/{deviceId}/telemetry`
- `GET /api/printers/{deviceId}/telemetry/stream`
- `GET /api/printers/by-name/telemetry?name={name}`
- `GET /api/printers/by-name/telemetry/stream?name={name}`
- task summary route planned/added around: `GET /api/tasks/{externalTaskId}/summary`
- task alias summary route added around: `GET /api/tasks/by-alias/{taskAlias}/summary`

### Activity/table-style routes
- `GET /api/tasks`
- `GET /api/tasks/{externalTaskId}`
- `GET /api/tasks/by-alias/{taskAlias}`
- `GET /api/messages`
- `GET /api/messages/{externalMessageId}`
- `GET /api/task-ams-details`
- `GET /api/task-ams-details/by-task/{externalTaskId}`
- `GET /api/task-ams-details/by-task-alias/{taskAlias}`

### Scheduled jobs
- `POST /api/scheduled-print-jobs`
- `GET /api/scheduled-print-jobs`
- `GET /api/scheduled-print-jobs/{jobId}`
- `POST /api/scheduled-print-jobs/{jobId}/cancel`
- `POST /api/scheduled-print-jobs/{jobId}/priority`
- `GET /api/scheduled-print-jobs/queue-preview`
- `POST /api/scheduled-print-jobs/run-dispatch-cycle`
- `POST /api/scheduled-print-jobs/reconcile`
- `GET /api/scheduled-print-jobs/scheduler-control`
- `POST /api/scheduled-print-jobs/scheduler-control`

## Important classes/services already central to the system

### API / hosted workers
- `PrinterCatalogSyncWorker`
- `PrinterSimulationCompletionWorker`
- `PrinterSimulationTelemetryWorker`
- `PrintSchedulingWorker`

### Infrastructure services
- `PrinterCatalogSyncService`
- `PrinterActivitySyncService`
- `PrinterSimulationService`
- `PrintSchedulerService`
- `PrinterReadService`
- `ScheduledPrintJobReadService`
- `TaskTelemetrySummaryService`
- `PrinterTelemetryGenerator`
- `InfluxPrinterTelemetryWriter`

### Streaming / telemetry
- `InMemoryTelemetryPublisher`
- `IPrinterTelemetryPublisher`
- `IPrinterTelemetryWriter`

## Key behavior rules to preserve

### Simulation vs sync precedence
- simulation-controlled printer state must not be overwritten by upstream sync while lock is active
- bind/version sync may still update safe metadata, but not simulation-owned operational state
- tasks/messages from upstream may be skipped for simulation-locked printers

### Source of truth tendencies
- Postgres = source of truth for printer state, tasks, messages, scheduled jobs, simulation lock
- InfluxDB = source of truth for telemetry history
- SSE = live push only
- Redis = optional for future caching/pubsub, not required right now

### Running printer logic
The resolved running state should consider simulation control first, then printer raw status.
The route `GET /api/printers/running` should use resolved operational state, not just raw `PrintStatus`.

## Current known practical issues / areas to check first in future chats
- scheduler may leave jobs stuck in `QUEUED` if dispatch fails silently
- scheduler should write clearer failure reasons into `SchedulerDecisionReason`
- `GET /api/printers/running` may need implementation or routing fix
- worker toggles in config should be checked if jobs do not move
- Bambu proxy worker should often stay disabled during local/simulation testing if proxy is unreachable

## Docker status
Backend-only Docker setup is desired.
- external Postgres / Redis / InfluxDB are already hosted on another PC
- backend container should expose port `5017`
- root files discussed:
  - `Dockerfile`
  - `.dockerignore`
  - optional `docker-compose.yml`

## AI / LLM readiness judgment
Current backend is enough to start AI/LLM integration, but not yet ideal for strong RAG/orchestrator/CoT because it still lacks:
- normalized event layer
- summary/knowledge abstraction layer
- retrieval-ready text chunks
- AI-oriented tool contracts

## Best way to continue in a new chat
Paste this document first, then say:

"Continue helping me on this Digital Twin backend. Keep the current architecture and logic consistent. I will paste the specific file or error next."
