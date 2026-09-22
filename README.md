# DFDS Smart Gate – Truck Visit Management API

[![CI](https://github.com/selcukgural/DFDS.SmartGate/actions/workflows/ci.yml/badge.svg)](https://github.com/selcukgural/DFDS.SmartGate/actions/workflows/ci.yml)

REST API for tracking truck visits at DFDS terminals: gate operators pre-register a visit (truck, driver,
deliveries/collections), move it through `PreRegistered → AtGate → OnSite → Completed`, and search visits per
terminal. Every status change is kept as an immutable audit trail.

Built with .NET 10 (C# 14), ASP.NET Core minimal APIs, EF Core 10 and PostgreSQL 17, following Clean Architecture.

- [Architecture](docs/architecture.md) – diagram, module boundaries, API design, storage rationale, security model,
  operations (Docker / Kubernetes / AWS).
- [Assumptions & trade-offs](docs/assumptions-and-trade-offs.md) – decisions on ambiguous requirements, alternatives,
  limitations and future work.

## Contents

1. [Tooling requirements](#tooling-requirements)
2. [Quick start](#quick-start)
3. [Configuration](#configuration)
4. [Running the tests](#running-the-tests)
5. [API overview](#api-overview)
6. [Repository layout](#repository-layout)

## Tooling requirements

| Tool | Version | Notes |
|---|---|---|
| .NET SDK | 10.0.1xx | Pinned by `global.json` (`rollForward: latestPatch`). Preview SDKs (11.x) are not used. |
| PostgreSQL | 17 | Local install or a container (see below). |
| Podman or Docker | any recent | For the integration tests (Testcontainers), for running PostgreSQL in a container and for building the image. |
| `dotnet-ef` | 10.0.12+ | `dotnet tool install -g dotnet-ef` (or `dotnet tool update -g dotnet-ef` if already installed) – applies the schema migration. Older 10.0.x builds leave a stray `bin\Debug` directory in the Infrastructure project that breaks the container build. |

The API needs ICU (globalisation support) at runtime: identifier normalisation uses Unicode NFKC. It is present on
macOS/Windows and on Linux distributions with `libicu`; container images must include it (the provided
[Dockerfile](Dockerfile) uses a runtime image that does).

## Quick start

All commands run from the repository root.

### 1. Start PostgreSQL

Container (Podman shown; replace with `docker` if that is what you have):

```bash
podman run -d --name smartgate-postgres -e POSTGRES_PASSWORD=postgres -p 5432:5432 postgres:17-alpine
podman exec smartgate-postgres createdb -U postgres dfds_visits_dev
```

If port 5432 is already taken by a local PostgreSQL, publish the container on another port (`-p 5433:5432`) and use
`Port=5433` in the connection strings below. Or use an existing local server and create an empty database
`dfds_visits_dev`.

### 2. Configure the connection string (user-secrets, never committed)

```bash
dotnet user-secrets set "ConnectionStrings:Visits" \
  "Host=localhost;Database=dfds_visits_dev;Username=postgres;Password=postgres" \
  --project src/DFDS.SmartGate.Api
```

### 3. Build and apply the schema

```bash
dotnet build DFDS.SmartGate.slnx

ConnectionStrings__Visits="Host=localhost;Database=dfds_visits_dev;Username=postgres;Password=postgres" \
  dotnet ef database update --project src/DFDS.SmartGate.Infrastructure
```

The build restores packages (`dotnet ef` needs a restored project) and confirms the SDK pin works. The
Infrastructure project has a design-time factory, so no running host is needed; the migration creates the three
tables, their indexes and the append-only trigger on the audit table.

### 4. Create a development bearer token

The API authenticates with OAuth2/JWT bearer tokens. For local development, ASP.NET Core's `user-jwts` tool issues
tokens the Development profile trusts (signing key stored in user-secrets):

```bash
dotnet user-jwts create --project src/DFDS.SmartGate.Api \
  --name gate-operator \
  --claim "terminal=DKCPH SEGOT" \
  --output token
```

`terminal` lists the UN/LOCODEs of the terminals the caller may access (space-separated because `user-jwts` cannot
repeat a claim; real identity providers emit a multi-valued claim). Copy the printed token.

### 5. Run the API

```bash
dotnet run --project src/DFDS.SmartGate.Api                           # http://localhost:5072
dotnet run --project src/DFDS.SmartGate.Api --launch-profile https    # https://localhost:7172 (dev certificate)
```

In the Development environment the OpenAPI document is served anonymously at `/openapi/v1.json`.

### 6. Try it

```bash
TOKEN="<paste the token>"

curl -s http://localhost:5072/health/ready

curl -s -X POST http://localhost:5072/api/visits \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{
    "terminalId": "DKCPH",
    "truck": { "unitNumber": "tr 001", "licensePlate": "ab 12 345", "carrier": "ACME Haulage" },
    "driver": { "name": "Ada Lovelace", "licenseNumber": "dl 1234", "phone": "+4512345678" },
    "movements": [
      { "type": "Delivery",   "unitNumber": "cont 1", "location": "SEGOT", "reference": "BK-1" },
      { "type": "Collection", "unitNumber": "cont 2", "location": "NLRTM" }
    ]
  }'

# use the "id" from the response
curl -s http://localhost:5072/api/visits/<id> -H "Authorization: Bearer $TOKEN"

curl -s -X POST http://localhost:5072/api/visits/<id>/status \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{ "status": "AtGate", "reason": "Arrived at gate 3" }'

curl -s "http://localhost:5072/api/visits?terminalId=DKCPH&currentStatus=AtGate&pageSize=10" \
  -H "Authorization: Bearer $TOKEN"
```

`src/DFDS.SmartGate.Api/DFDS.SmartGate.Api.http` contains the same requests (plus error cases) for the Rider / VS Code
HTTP client.

## Configuration

Nothing secret lives in `appsettings*.json`. Values come from user-secrets locally and from environment variables /
a secret store in deployed environments (`ConnectionStrings__Visits`, `Authentication__Schemes__Bearer__Authority`, …).

| Key | Required | Purpose |
|---|---|---|
| `ConnectionStrings:Visits` | yes | PostgreSQL connection string. |
| `ConnectionStrings:Redis` | no | When set, `HybridCache` gains a Redis L2 tier (multi-instance cache coherence). |
| `Authentication:Schemes:Bearer:Authority` | prod | Identity provider (OIDC discovery). Signing keys are downloaded from it. |
| `Authentication:Schemes:Bearer:ValidAudiences` | yes | Accepted `aud` values. |
| `Authentication:Schemes:Bearer:ValidIssuer` | no | Accepted `iss`; defaults to the authority's issuer. |
| `Authentication:Schemes:Bearer:RequireHttpsMetadata` | prod | Must stay `true` outside Development (enforced at start-up). |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | no | When set, traces and metrics are exported over OTLP (otherwise they stay in-process). |
| `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | prod | `true` behind a TLS-terminating load balancer so HTTPS redirection / HSTS see the original scheme. |

Start-up validation fails fast when bearer authentication cannot work (no authority or signing key, no audience,
symmetric key or plain-HTTP metadata outside Development) instead of answering every request with 401.

Environment differences (`ASPNETCORE_ENVIRONMENT`):

| | Development | anything else |
|---|---|---|
| Signing keys | `dotnet user-jwts` symmetric key allowed | asymmetric keys from the authority only |
| HTTPS | http profile allowed | HSTS; HTTP→HTTPS redirect when `ASPNETCORE_HTTPS_PORT` is set |
| Logs | plain console | JSON console, one combined line per request |
| OpenAPI | `/openapi/v1.json` (anonymous) | not served |

## Running the tests

xunit.v3 runs on the Microsoft.Testing.Platform, so `dotnet test` takes `--project` / `--solution` and filters go
after `--`.

```bash
dotnet test --solution DFDS.SmartGate.slnx                                                   # everything (unit + integration)
dotnet test --project tests/DFDS.SmartGate.UnitTests/DFDS.SmartGate.UnitTests.csproj         # unit tests only, < 1 s
dotnet test --project tests/DFDS.SmartGate.IntegrationTests/DFDS.SmartGate.IntegrationTests.csproj   # integration, ~6 s
dotnet test --project tests/DFDS.SmartGate.UnitTests/DFDS.SmartGate.UnitTests.csproj -- --filter-class "*VisitTransitionTests*"
```

- **Unit tests** (`tests/DFDS.SmartGate.UnitTests`, 225): Domain (value objects, normalisation, status transitions,
  aggregate), Application (handlers, validators, authorisation rules, caching) and the Api host glue (claims → caller
  context, error mapping, validation filter, correlation id) – all without a server or database.
- **Integration tests** (`tests/DFDS.SmartGate.IntegrationTests`, 63): the real host (`Program.cs`, full middleware
  pipeline, production auth rules with RSA-signed test tokens) against PostgreSQL. By default Testcontainers starts
  `postgres:17-alpine` – Podman (via its Docker-compatible socket) or Docker must be running. To use an existing
  server instead:

  ```bash
  SMARTGATE_TEST_DATABASE="Host=localhost;Database=dfds_visits_test;Username=postgres;Password=postgres" \
    dotnet test --project tests/DFDS.SmartGate.IntegrationTests/DFDS.SmartGate.IntegrationTests.csproj
  ```

  The suite migrates that database and leaves its rows behind (each test works in its own random terminal, so
  no clean-up is needed between runs). It covers authentication (401/403), the HTTP pipeline (security headers,
  correlation ids, framework error mapping), all visit endpoints and search filters, terminal scoping (403 vs 404),
  migration completeness, the append-only audit trigger and optimistic concurrency.

### Load test (k6)

`tests/load/k6/visits.js` sends a realistic mix (60 % search, 30 % get, 6 % create, 4 % status update) at a fixed
arrival rate – 300 req/s for two minutes, then 450 req/s – and fails when the error rate exceeds 1 % or p95 latency
exceeds 200 ms. Needs [k6](https://grafana.com/docs/k6/latest/set-up/install-k6/) (`brew install k6`) and a running
API (steps 1–5 above, ideally `dotnet run -c Release`):

```bash
TOKEN=$(dotnet user-jwts create --project src/DFDS.SmartGate.Api --name gate-operator --claim "terminal=DKCPH SEGOT" --output token | tail -1)
k6 run -e TOKEN=$TOKEN tests/load/k6/visits.js                 # full profile, ~4 min
k6 run -e TOKEN=$TOKEN -e PROFILE=ci tests/load/k6/visits.js   # 300 req/s for 60 s

# live dashboard at http://127.0.0.1:5665 while it runs, plus a self-contained HTML report at the end
K6_WEB_DASHBOARD=true K6_WEB_DASHBOARD_EXPORT=my-report.html k6 run -e TOKEN=$TOKEN tests/load/k6/visits.js
```

**Recorded evidence** of the full profile is committed under [`docs/load-test/`](docs/load-test/): open
[`report.html`](docs/load-test/report.html) in a browser for the time-series charts (request rate, latency
percentiles, errors per stage), `summary.json` for the raw metrics and `summary.txt` for the console output. The
numbers are discussed in [docs/architecture.md](docs/architecture.md#91-load-test-results). The CI workflow publishes
the same report as a build artifact and the key metrics in the job summary of each run.

### Continuous integration

`.github/workflows/ci.yml` runs on every push and pull request: build with warnings as errors, unit tests,
integration tests (Testcontainers on the runner's Docker) and the container image build.
`.github/workflows/load-test.yml` runs the k6 scenario on demand against a runner-local PostgreSQL.

Warnings are errors in every project (`TreatWarningsAsErrors`, .NET analyzers at `latest-recommended`, code-style
enforced in build).

## API overview

All endpoints require a bearer token (`401` otherwise) whose `sub` / `client_id` identifies the caller and whose
`terminal` claim lists the accessible terminals. Errors are RFC 9457 `application/problem+json` with a stable `code`
extension and the request's `correlationId`.

| Method & path | Purpose | Success | Errors |
|---|---|---|---|
| `POST /api/visits` | Pre-register a visit | `201` + `Location` | `400` validation, `403` terminal not in token |
| `GET /api/visits/{id}` | Visit with full status history | `200` | `404` unknown *or* other terminal's visit |
| `GET /api/visits?…` | Search: `terminalId`, `currentStatus`, `movementFrom`, `movementTo`, `createdTimeFrom`, `createdTimeTo`, `createdBy`, `page`, `pageSize` | `200` + paging metadata | `400` validation, `403` terminal not in token |
| `POST /api/visits/{id}/status` | Move to the next status (`{ "status", "reason"? }`) | `200` updated visit | `404`, `409` invalid transition / concurrent update |
| `GET /health/live`, `GET /health/ready` | Probes (anonymous) | `200 Healthy` | `503` when PostgreSQL is unreachable (ready) |

Details – contracts, normalisation rules, search semantics, status flow – are in
[docs/architecture.md](docs/architecture.md#4-api-design).

## Container image

```bash
podman build -t smartgate-api .          # or: docker build -t smartgate-api .
podman run --rm -p 8080:8080 \
  -e ConnectionStrings__Visits="Host=host.containers.internal;Database=dfds_visits_dev;Username=postgres;Password=postgres" \
  -e Authentication__Schemes__Bearer__Authority="https://idp.example.com/realms/dfds" \
  -e Authentication__Schemes__Bearer__ValidAudiences__0="smartgate-api" \
  smartgate-api
curl -s http://localhost:8080/health/ready
```

The image runs the production configuration (non-root, HSTS, JSON logs, asymmetric keys from the authority only),
so `dotnet user-jwts` tokens are not accepted by it; see [docs/architecture.md](docs/architecture.md#11-operations-containers-kubernetes-aws)
for the Kubernetes manifest and the AWS mapping.

## Repository layout

```
DFDS.SmartGate.slnx                    solution (src/ and tests/ folders)
Directory.Build.props                  net10.0, nullable, warnings-as-errors, analyzers, XML docs required
Directory.Packages.props               central package versions
global.json                            SDK pin + Microsoft.Testing.Platform runner
src/DFDS.SmartGate.Domain/             entities, value objects, status transitions, Result/DomainError – no dependencies
src/DFDS.SmartGate.Application/        use-case handlers, FluentValidation validators, read models, ports
src/DFDS.SmartGate.Infrastructure/     EF Core + PostgreSQL: DbContext, configurations, migrations, port implementations
src/DFDS.SmartGate.Api/                ASP.NET Core host: endpoints, JWT auth, ProblemDetails, observability
tests/DFDS.SmartGate.UnitTests/        xunit.v3 unit tests (Domain, Application, Api glue)
tests/DFDS.SmartGate.IntegrationTests/ xunit.v3 + WebApplicationFactory + Testcontainers
tests/load/k6/                         k6 load-test scenario
.github/workflows/                     CI (build, tests, image) and on-demand load test
docs/                                  architecture and assumptions documents; docs/load-test/ holds the recorded k6 report
Dockerfile, .dockerignore              multi-stage image (chiseled, non-root, ICU)
deploy/k8s/                            sample Kubernetes manifest
```
