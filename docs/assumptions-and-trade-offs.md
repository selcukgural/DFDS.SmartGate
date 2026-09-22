# Assumptions and trade-offs

The brief is intentionally open-ended. This document lists the assumptions made where it was silent or ambiguous,
the main architectural decisions with their rationale and the alternatives that were considered, the trade-offs
accepted, and the known limitations with the intended way forward. The design itself is described in
[architecture.md](architecture.md).

1. [Assumptions](#1-assumptions)
2. [Architectural decisions, rationale and alternatives](#2-architectural-decisions-rationale-and-alternatives)
3. [Accepted trade-offs](#3-accepted-trade-offs)
4. [Known limitations and future improvements](#4-known-limitations-and-future-improvements)

## 1. Assumptions

### 1.1 Domain and API contract

| # | Ambiguity in the brief | Assumption | Why |
|---|---|---|---|
| A1 | `movementFrom` / `movementTo` – a time window or locations? | **Locations** (origin / destination of a movement). | The brief names the time filters explicitly with "Time" (`createdTimeFrom/To`); the movement parameters carry no such suffix. A time-window reading would also duplicate `createdTime*`. |
| A2 | Format of terminal and movement locations. | **UN/LOCODE** (`DKCPH`, `SEGOT`, `NLRTM`): 2-letter country + 3 characters, validated by format only. | An international operator needs an unambiguous, language-neutral code; it makes terminal ids type-compatible with movement locations and lets a 2-letter filter mean "country". Existence is not validated (no master data in scope); display names are a presentation concern. |
| A3 | What a movement record contains. | Client sends `type`, `unitNumber`, one `location` (the external counterpart) and an optional `reference`. The terminal side is derived from `type`. | The brief defines no movement fields, so the minimum was added. Deriving the terminal side means an invalid origin/destination combination cannot be expressed. |
| A4 | Search semantics with movement filters. | A visit matches if **at least one** movement satisfies the filter(s); `movementFrom` and `movementTo` together apply to the **same** movement. 2 characters → country match, 5 → location match, anything else → `400`. | Matches how an operator asks the question ("trucks bringing something from Sweden"); cross-leg matching would return visits that never made that journey. |
| A5 | Status names in JSON. | `PreRegistered`, `AtGate`, `OnSite`, `Completed` (PascalCase enum names, case-insensitive on input); the brief's display names ("Pre-Registered", "At Gate") are for UIs. | Stable identifiers without spaces are safer in query strings and client code. |
| A6 | Status flow. | **Strictly forward, no skips, no backward moves**: `PreRegistered → AtGate → OnSite → Completed`; `Completed` is terminal. Every visit is created as `PreRegistered`; the client cannot choose the initial status. | The four statuses read as a physical sequence through the terminal. Backward moves would make the audit history ambiguous. |
| A7 | Gate rejection / cancellation. | **Not modelled in v1.** A rejected truck stays `AtGate`; a no-show stays `PreRegistered`. | The brief gives a closed list of four statuses. Adding `Rejected` is a product decision (see §4); the transition table makes it a one-row change when taken. |
| A8 | "Unit numbers and license plates are capitalized and contain no whitespaces." | Applied to truck unit number, licence plate, movement unit numbers **and** driver licence number: strip all Unicode whitespace, NFKC-normalise, upper-case (invariant), allow letters, digits and `-`. Not restricted to Latin letters (Chinese/Japanese plates). No ISO 6346 check-digit validation. | The rule is about identifiers in general; NFKC handles full-width characters and compatibility forms that would otherwise create duplicates. |
| A9 | Driver information. | `name` (required), `licenseNumber` (required, normalised like a plate), `phone` (optional, E.164). | "Driver information must be captured" – the minimum a gate needs to identify the person, without collecting more personal data than necessary. |
| A10 | Truck. | `unitNumber` + `licensePlate` required (brief), `carrier` optional. | |
| A11 | Updating a visit. | Only the status can change (`POST /api/visits/{id}/status` with an optional `reason`). Truck, driver and movements are immutable after creation. | An audit-relevant record should not be edited in place; a mistake is corrected by a new visit. |
| A12 | `createdBy`. | Always the token subject (`sub`, else `client_id`); it cannot be supplied by the client. | Otherwise the audit trail would trust client input. |
| A13 | Ids. | Server-generated UUID v7; a client-supplied `id` is rejected. | Time-ordered for index locality, unguessable enough combined with A16. |
| A14 | Time. | UTC everywhere (`DateTimeOffset`, ISO-8601); `createdTimeFrom/To` inclusive; a search without a window defaults to **the last 30 days** and echoes the effective window. | Prevents unbounded scans from "search everything" clients while keeping the response self-describing. |
| A15 | Paging. | `page` 1-based default 1; `pageSize` default 20, max 100 (over → `400`); fixed order `createdTime desc, id desc`; response `{ items, page, pageSize, totalCount, totalPages }`. | The brief asks for paging metadata; a fixed order makes pages stable. |

### 1.2 Security

| # | Ambiguity | Assumption |
|---|---|---|
| A16 | "Authorization based on terminal access." | The JWT carries a multi-valued `terminal` claim with UN/LOCODEs. Any terminal in the claim grants **read and write** for that terminal (no roles in v1). Cross-terminal access: `POST` and `GET ?terminalId=` → `403`; `GET /{id}` and status updates on another terminal's visit → **`404`** (existence must not leak); search without `terminalId` → restricted to the caller's terminals. |
| A17 | Identity provider. | An external OAuth2/OIDC provider (corporate IdP, Cognito, Keycloak …) issues tokens; the API validates them and never issues its own. Locally `dotnet user-jwts` stands in. |
| A18 | Machine clients. | Terminal systems authenticate with `client_credentials`; their `client_id` is used as the subject when `sub` is absent. |
| A19 | TLS. | Terminates at the load balancer; the API enforces HTTPS redirection and HSTS outside Development and trusts forwarded headers there. |
| A20 | Rate limiting / WAF. | Provided by the API gateway / ingress, not the application. |

### 1.3 Operations

| # | Assumption |
|---|---|
| A21 | Idempotency of `POST /api/visits` is **not** required in v1: the same plate legitimately visits several times a day, so there is no natural key; a retried create may produce a duplicate (see §4). |
| A22 | "Near real-time" means a status change is visible on the next read within seconds; a 30-second in-process cache for get-by-id, evicted on the instance that handles the change, is acceptable. Search is not cached. |
| A23 | Availability and scale targets are met with stateless replicas plus a managed, highly available PostgreSQL; no active-active multi-region. |
| A24 | Container runtime for local integration tests may be Podman or Docker. |

## 2. Architectural decisions, rationale and alternatives

| # | Decision | Rationale | Alternatives considered |
|---|---|---|---|
| D1 | **Clean Architecture in four projects** (Domain → Application → Infrastructure / Api), dependencies enforced by project references. | Business rules are testable without a framework; the store and the host are replaceable; the layering is compiler-checked, not convention-checked. | Single project with folders (faster to start, but boundaries erode); vertical slices per feature (good for large teams, overkill for four use cases). |
| D2 | **Rich domain model**: `Visit` aggregate with `Create`/`TransitionTo`, value objects with `Create(raw) → Result<T>` factories, transition rules as a `FrozenDictionary` table. | Invariants live next to the data they protect; the "capitalised, no whitespace" rule and the status flow are unit-tested in isolation; a new status is a table row. | Anaemic entities + service classes (rules scattered across services and validators); state-machine library (extra dependency for four states). |
| D3 | **Handlers, no mediator**; `ICommandHandler<,>` / `IQueryHandler<,>` registered by assembly scan; cross-cutting behaviour via decorators. | One less dispatch and one less package; the call path endpoint → filter → handler → port is explicit. | MediatR / Mediator.SourceGenerator (pipeline behaviours are convenient, but the same is achieved with decorators when needed). |
| D4 | **Validate at the edge, once** with FluentValidation in an endpoint filter; format rules delegate to value-object factories. | No duplicated rules, no re-validation inside handlers, field-level `400`s keyed by JSON path. | Data annotations (weaker composition, no delegation to domain rules); validating inside handlers (mixes concerns, loses field paths). |
| D5 | **`Result` / `DomainError` for expected failures**, `ErrorKind` → HTTP status, stable `code` in ProblemDetails. | Expected outcomes (not found, invalid transition) are control flow, not exceptions; clients get machine-readable codes; no exception cost on hot paths. | Throwing domain exceptions mapped by middleware (simpler call sites, but exceptions for expected cases and hidden control flow). |
| D6 | **PostgreSQL, three relational tables**, truck/driver as columns on `visits`, generated country columns, `sequence` for movement order. | Every search filter is index-backed; triggers, generated columns, `xmin` and partitioning are exactly the tools the audit/retention requirements need; boring to operate as a managed service. | JSONB movements (order for free, but GIN expression indexes for country/location search and no append-only trigger); document database (same indexing issue, weaker transactional guarantees across the three collections); separate `trucks`/`drivers` tables (no dedupe requirement, spreads PII). |
| D7 | **Append-only audit enforced by a database trigger** in addition to the aggregate. | "Immutable audit history" is a regulatory property; the database is the last line of defence against bugs and privileged access. | Application-only enforcement (cheaper, but a single `UPDATE` from a console undoes it); event sourcing (perfect fit for history, but projections and rebuild tooling for a four-row history are not justified in v1 – recorded as the evolution path if audit requirements grow). |
| D8 | **Optimistic concurrency on `xmin`.** | No version column, no locks; a lost race is a clear `409` the client retries. | Pessimistic `SELECT … FOR UPDATE` (lock waits under contention, connection pinning); serializable transactions (retry storms). |
| D9 | **CQRS-lite**: `IVisitRepository` (aggregate, writes) vs `IVisitReadStore` (`AsNoTracking` projections, reads). | Reads never materialise the aggregate; the read side can add projections/caches without touching write logic. | Repository for everything (simpler, but every read pays for tracking and full materialisation). |
| D10 | **HybridCache for get-by-id** (L1 30 s, optional Redis L2 5 min, evicted on change); search uncached. | Get-by-id after a status change is the hottest read (operators refresh); the abstraction makes Redis a connection string, not a code change. | No cache (simplest; fine for v1 volumes, but the cache costs nothing and demonstrates the coherence trade-off); output caching (cannot be keyed by terminal authorisation safely). |
| D11 | **Standard JWT bearer handler bound from `Authentication:Schemes:Bearer`**, start-up validation that refuses insecure configurations outside Development. | No custom auth code to audit; misconfiguration fails deployment instead of running open. | Custom token endpoint for development (extra attack surface); signing key in `appsettings` (secret in source control). |
| D12 | **Terminal authorisation in the handlers**, `403` vs `404` per operation. | The decision depends on the resource (is the visit's terminal in the token?), which only the handler knows; `404` for foreign visits prevents enumeration. | Policy-based authorisation with resource handlers (works, but splits the rule between Api and Application and still needs the visit loaded). |
| D13 | **Minimal APIs** with one `Map*` line per endpoint and static handler methods. | Least ceremony, endpoint filters for validation, native OpenAPI. | Controllers (more conventions than needed here); FastEndpoints (extra dependency). |
| D14 | **RFC 9457 ProblemDetails for every error**, including framework-generated ones, with a client-safe `detail`. | One error contract; no leakage of internals. | Default ASP.NET Core bodies (inconsistent shapes, type names in binding errors). |
| D15 | **OpenTelemetry + OTLP**, JSON console logs, one combined request log line, `LoggerMessage` events. | Vendor-neutral, cheap to emit, works with CloudWatch/Grafana/Datadog via a collector; no per-step log noise. | Vendor SDK (lock-in); log-per-middleware (noise at 300 req/s). |
| D16 | **Integration tests run the production configuration** (non-Development environment, RSA-signed tokens, HSTS, no OpenAPI) against a throw-away PostgreSQL container. | Tests prove the hardened configuration works, not a relaxed one; the trigger, `xmin` and indexes are only meaningful on the real database. | SQLite/in-memory provider (cannot exercise triggers, generated columns or `xmin`); Development environment in tests (would hide production-only failures). |

## 3. Accepted trade-offs

| Trade-off | Chosen side | Cost accepted |
|---|---|---|
| Simplicity vs. cache coherence | In-process L1 with a short TTL; eviction only on the instance that handled the write. | Another replica may serve a ≤ 30 s stale get-by-id after a status change. Redis L2 (one connection string) reduces this to the L1 TTL; a pub/sub backplane is the full fix (§4). |
| Offset paging with `COUNT(*)` vs. keyset paging | Offset + count, because `totalPages` is a required piece of paging metadata. | Deep pages and counts over very wide windows cost more; mitigated by the 30-day default window and terminal-leading indexes. |
| Strict forward status flow vs. operational flexibility | Strict. | Operators cannot correct a mis-click by moving back; the fix is a product decision on a `Rejected`/`Cancelled` status or an explicit correction flow (§4). |
| Trigger-enforced immutability vs. operability | Enforced. | Even legitimate data fixes to history rows need a migration that drops and re-creates the trigger – deliberately visible in the schema history. |
| Full normalisation of identifiers (NFKC, Unicode letters) vs. simplicity | Full. | Requires ICU at runtime (container images must include it, invariant globalisation must stay off). |
| Validation at the edge vs. defensive re-checks | Edge only; handlers trust validated input. | A handler invoked without its filter (e.g. from a future non-HTTP entry point) must add the validation step itself. |
| No idempotency key vs. exactly-once creates | No key in v1. | A client retry after a timeout may create a duplicate visit. |
| Terminal claim grants read + write vs. fine-grained scopes | Coarse. | A read-only integration currently receives write access to its terminals; scopes are the planned refinement. |
| Warnings as errors, analyzers at `latest-recommended`, XML docs on every public member | Strict. | Slower to write, but the reviewer gets consistent, self-documenting code and the build catches regressions. |

## 4. Known limitations and future improvements

Ordered by the value they would add.

1. **Gate rejection / cancellation** (open product question). Proposed design: `Rejected` (from `AtGate`) and
   `Cancelled` (from `PreRegistered`) as terminal statuses – two enum members, two rows in
   `VisitStatusTransitions`, tests; no schema change (statuses are `smallint`).
2. **Idempotent creates**: an `Idempotency-Key` header stored with the visit (unique per caller), returning the
   original `201` on replay.
3. **Cache coherence across replicas**: Redis L2 is already switchable; add a Redis pub/sub backplane so a status
   change evicts the L1 of every replica immediately.
4. **Scopes / roles**: `visits:read` and `visits:write` scopes as additional policies; terminal claim unchanged.
5. **Retention automation**: yearly range partitioning of the three tables by `created_at`, detach + archive to
   object storage after seven years; a documented DBA runbook until it is automated (EF migrations do not manage
   partitions).
6. **Keyset paging** (`after` cursor) as an additional paging mode for deep pages; offset mode stays for
   `totalPages`.
7. **Location master data**: validate UN/LOCODEs against a reference table and expose display names.
8. **Personal-data controls**: field-level encryption for driver licence number and phone, a documented legal basis
   for the 7-year retention, and a data-subject access/erasure procedure compatible with the immutable history (e.g.
   pseudonymising `changed_by`/driver fields in place while keeping the rows).
9. **API versioning** via route groups when the first breaking change arrives; additive changes need none.
10. **Search by truck / driver** (`licensePlate`, `unitNumber`) – not in the brief's parameter list but an obvious
    operator need; would add two indexed columns to the search criteria.
11. Known framework quirk: after a request body that fails JSON deserialisation, Kestrel logs a "Reading is already
    in progress" warning and closes the connection; the client has already received its `400`. Tracked upstream;
    harmless for clients.
