# E-Commerce Microservices

This solution is a .NET Aspire e-commerce lab organized as deployable microservices. Each bounded context is a single project and uses Vertical Slice Architecture internally.

## Solution Layout

- `src/ECommerce.AppHost`: Aspire local orchestration.
- `src/ECommerce.ServiceDefaults`: shared telemetry, health checks, messaging infrastructure, outbox/inbox primitives.
- `src/ECommerce.Contracts`: shared integration event contracts only.
- `src/Services/Catalog`: product, category, brand, variant, and base list price.
- `src/Services/Inventory`: stock, reservations, deductions, and stock release.
- `src/Services/Basket`: temporary customer basket backed by Redis.
- `src/Services/Ordering`: order lifecycle and order status transitions.
- `src/Services/Payment`: fake payment intent, transaction, and refund flow.
- `src/Services/Notification`: event-driven notification log.
- `tests`: focused tests for core service slices.

## Architecture Rules

- One bounded context equals one deployable service project.
- No `Service.Api` / `Service.Application` / `Service.Domain` / `Service.Infrastructure` split.
- Features are vertical slices under each service's `Features` folder.
- Each service owns its own `DbContext` and migrations.
- Services do not query another service's database.
- Entities are not shared across services.
- Shared contracts are limited to integration events and cross-service DTOs.
- Commands that need in-process dispatch use MassTransit Mediator.
- Cross-service workflow uses Kafka topics through MassTransit.
- Database-backed event publishing uses the local outbox table.
- Event consumers claim inbox messages before processing and mark them processed after business changes are saved. Stale processing locks can be retried.

## Checkout Flow

1. Customer browses Catalog.
2. Customer adds items to Basket.
3. Basket checkout publishes `BasketCheckedOutIntegrationEvent`.
4. Ordering creates a pending order and publishes `OrderCreatedIntegrationEvent`.
5. Inventory reserves stock and publishes `StockReservedIntegrationEvent` or `StockReservationFailedIntegrationEvent`.
6. Payment consumes stock-reserved events and publishes `PaymentSucceededIntegrationEvent` or `PaymentFailedIntegrationEvent`.
7. Ordering confirms or cancels the order based on payment/reservation result.
8. Notification consumes important integration events and stores notification records.

The flow is eventually consistent. It does not use distributed transactions or shared databases. Ordering also runs a checkout saga timeout worker that fails stuck checkouts and releases reserved stock when needed.

## Local Development

Restore, build, and test:

```powershell
dotnet restore ECommerce.slnx
dotnet build ECommerce.slnx
dotnet test ECommerce.slnx
```

Run the Aspire AppHost:

```powershell
dotnet run --project src/ECommerce.AppHost/ECommerce.AppHost.csproj
```

Aspire provisions PostgreSQL databases, Redis, Kafka, and the service projects for local development. Runtime connection strings are supplied by Aspire configuration.

## Gateway Authentication

The Aspire AppHost exposes `src/ECommerce.Gateway` as the API entrypoint. Gateway authentication uses JWT bearer tokens.

Development token helper:

```powershell
Invoke-RestMethod -Method Post -Uri https://localhost:<gateway-port>/auth/dev-token -ContentType 'application/json' -Body '{"subject":"dev-user","role":"Customer"}'
```

Use the returned token as:

```text
Authorization: Bearer <accessToken>
```

Route policies:

- `/api/catalog/**`: public.
- `/api/basket/**`, `/api/orders/**`, `/api/payments/**`: `Customer` or `Admin`.
- `/api/inventory/**`, `/api/notifications/**`: `Admin`.

Production must set `Authentication:SigningKey` from a secret store and should replace the development token endpoint with a real identity provider.

Gateway also forwards `X-Internal-Api-Key` to backend services when `InternalApi:ApiKey` is configured. Backend services enforce that header when their own `InternalApi:ApiKey` is configured. `HttpClientFactory` clients created through service defaults also attach the same header for backend-to-backend calls. Keep `InternalApi:RequireInbound=false` on the gateway so external customers authenticate with JWT instead of the internal service key.

Production required secrets:

- `Authentication:SigningKey` on `ECommerce.Gateway`.
- `InternalApi:ApiKey` on `ECommerce.Gateway` and every backend service.
- `PaymentProvider:WebhookSecret` on `Payment`.

The development values in `appsettings.Development.json` are local-only placeholders and must not be reused in production.

## Reliability Notes

Payment APIs support `Idempotency-Key` on create and refund commands. Payment records store the idempotency key, provider transaction id, failure reason, completion timestamp, and refund timestamp.

Fake payment provider webhooks are available at:

```text
POST /api/payments/webhooks/fake-provider
```

The webhook payload must include `EventId`, `PaymentId`, `ProviderTransactionId`, `Status`, and `Signature`. New clients should also send `OccurredAt`; events outside the 5 minute replay window are rejected. The signature is an HMAC-SHA256 over:

```text
{EventId}:{PaymentId}:{ProviderTransactionId}:{Status}
```

Inventory records stock movements for reserve, release, deduct, and reservation expiry. Reservations default to a 15 minute expiry and a background service releases expired reservations. Stock reservation uses conditional database updates so concurrent orders cannot reserve more than the available quantity.

## Production Operations Gaps

The application code includes the core reliability primitives, but production deployment still needs environment-specific infrastructure:

- Container images and Kubernetes/Helm manifests with readiness/liveness probes, resource limits, HPA, and network policies.
- A real identity provider for OIDC/OAuth2 and service-to-service identity such as mTLS or workload identity.
- A secret manager such as Vault, cloud secret manager, or Kubernetes Secrets wired by deployment tooling.
- OpenTelemetry export targets and dashboards for Prometheus/Grafana/Loki/Tempo or an equivalent stack.
- Testcontainers integration tests for PostgreSQL, Redis, and Kafka, plus concurrency tests for inventory reservation and inbox duplicate delivery.
- Reviewed SQL migration rollout using expand-contract for destructive schema changes.

## Migrations

Each database-backed service stores migrations in its own project under `Data/Migrations`.

Create a new migration from the repo root:

```powershell
dotnet ef migrations add <MigrationName> --project src\Services\<Service>\<Service>.csproj --startup-project src\Services\<Service>\<Service>.csproj --output-dir Data\Migrations
```

Do not run `database update` unless you intentionally want to mutate a local database.

## Adding A Service

1. Add `src/Services/<Service>/<Service>.csproj`.
2. Keep service code inside that project using `Features`, `Data`, `Models`, `Contracts`, `IntegrationEvents`, and `Extensions`.
3. Add a service-specific `DbContext` only if the service owns persistent state.
4. Add integration events to `src/ECommerce.Contracts` only when other services need them.
5. Wire MassTransit Kafka consumers/producers and outbox/inbox only when the service participates in async workflows.
6. Register the project and dependencies in `src/ECommerce.AppHost/Program.cs`.
7. Add focused tests under `tests/<Service>.Tests`.
