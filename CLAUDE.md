# Media Service

## Purpose

.NET 10 service responsible for record upload, compression, indexing, streaming, and downloads. It is deployed as a Docker image and relies on Identity plus PostgreSQL, Redis, and RabbitMQ.

## Solution structure

- `Media.API` exposes controllers, contracts, middleware, filters, hosted services, and HTTP-facing services.
- `Media.Application` contains application contracts, models, constants, extensions, and message consumers.
- `Media.Infrastructure` owns filesystem I/O, repositories, and external services.
- `Media.DBContext` owns EF Core models, extensions, and migrations.

Keep API contracts separate from infrastructure details. Media files are external state and must be handled with validation, bounded paths, authorization, and correct cleanup. Do not modify deployed migrations in place.

## Local commands

```bash
dotnet restore Media.sln
dotnet build Media.sln
dotnet test Media.sln
docker compose --env-file dev.env up --build -d
```

Create local appsettings from the provided template. Keep media volume paths, credentials, certificates, and environment files out of commits. Preserve authorization checks and message contracts when changing upload or processing workflows.

Update `mml.project/docs/setup/backend.mdx`, record concepts, or architecture documentation when media configuration, endpoints, processing, storage, streaming, or downloads change.

## graphify

This project has a knowledge graph at graphify-out/ with god nodes, community structure, and cross-file relationships.

Rules:
- For codebase questions, first run `graphify query "<question>"` when graphify-out/graph.json exists. Use `graphify path "<A>" "<B>"` for relationships and `graphify explain "<concept>"` for focused concepts. These return a scoped subgraph, usually much smaller than GRAPH_REPORT.md or raw grep output.
- If graphify-out/wiki/index.md exists, use it for broad navigation instead of raw source browsing.
- Read graphify-out/GRAPH_REPORT.md only for broad architecture review or when query/path/explain do not surface enough context.
- After modifying code, run `graphify update .` to keep the graph current (AST-only, no API cost).
