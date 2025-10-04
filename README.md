# RegulaFlow Audit SaaS  
[![Build and Test](https://github.com/anistouati/AuditSaas/actions/workflows/build-test.yml/badge.svg)](https://github.com/anistouati/AuditSaas/actions/workflows/build-test.yml)

Audit Scheduling & Tracking — Governance, Risk & Compliance (GRC) SaaS Demo

## Overview
RegulaFlow Audit SaaS is a multi-tenant compliance demo that implements an Audit Scheduling & Tracking workflow.
It is aligned with Interfacing’s GRC sector and demonstrates a modern SaaS architecture using .NET, Docker, and cloud-native tooling.

## Features
- Audit Scheduling → SQL persistence via EF Core
- Event-Driven Messaging → RabbitMQ publishes `AuditScheduled` events
- Caching → Redis stores audit summaries
- Worker Service → Consumes RabbitMQ messages, writes summaries to Redis
- Authentication → Keycloak (JWT + RBAC), with DevAuth stub for local dev
- Observability → OpenTelemetry traces and metrics, Aspire dashboard
- Schema → Auto-created at startup
- Testing → TUnit unit tests, integration tests in separate project
- CI/CD → GitHub Actions (build + test pipeline with badge)

## Tech Stack

| Layer            | Technology |
|------------------|------------|
| Backend          | .NET 9 Minimal APIs |
| Data             | Entity Framework Core + SQL Server |
| Auth             | Keycloak (OIDC/JWT), DevAuth stub |
| Messaging        | RabbitMQ |
| Caching          | Redis |
| Observability    | OpenTelemetry + Aspire dashboard |
| Testing          | TUnit unit tests, integration tests |
| Infra            | Docker Compose |

## Quick Start (Docker)

```bash
docker-compose up -d --build
```

Services:
- Frontend: http://localhost:3000
- API: http://localhost:5000
- Keycloak: http://localhost:8080
- RabbitMQ: http://localhost:15672 (guest/guest)
- Aspire Dashboard: http://localhost:18888

## Local Dev (Frontend)
```bash
cd frontend
npm install
npm run dev
```

## Endpoints
- POST /api/audit/schedule
- GET  /api/audit
- GET  /api/audit/{id}
- GET  /api/audit/{id}/summary
