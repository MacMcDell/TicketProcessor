# 🎟️ TicketProcessor — Concert Ticket Management System

A sample .NET 8 **Azure Functions API** for managing concert tickets.  
Implements **Events**, **Ticket Reservations with Redis idempotency**, and **Purchases with external payment simulation**.

---

## 📦 Features

- **Event Management**
  - Create/update events and venues
  - Set per-event ticket categories (GA, VIP, Balcony, etc.) with price & capacity
  - Auto-seeded sample data

- **Reservations**
  - Hold tickets temporarily with an idempotency key
  - Expire after configurable window (default 10 minutes)
  - Reflected in availability counts

- **Purchases**
  - Confirm reservation & increment sold count
  - Simulated payment gateway (`https://httpbin.org/post`)

- **Availability**
  - `Available = Capacity - Sold - ActivePending`

- **Infrastructure**
  - Postgres (EF Core, Code First, `xmin` concurrency)
  - Redis (idempotency keys)
  - Auto-seed database with demo venues, events, and a sample pending reservation

---

## 🛠️ Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://docs.docker.com/get-docker/)
- [Azure Functions Core Tools](https://learn.microsoft.com/azure/azure-functions/functions-run-local)

---

## 🚀 Quick Start

### 1. LocalSettings.json
Ensure you have a `LocalSettings.json` file in the `src/TicketProcessor.Api`

Ensure you have a `LocalSettings.jon` file in the `src/Infrastructure`

### 2. Start infra (Postgres + Redis)

```bash

docker compose up -d
--> the db aleady contains a migration
cd .\TicketProcessor.Infrastructure\
dotnet ef database update
```
### 3. Start API

```bash

cd .\TicketProcessor.Api\
dotnet run
```
### 4. Test API (Swagger)

RenderSwaggerUI: [GET] http://localhost:7071/api/swagger/ui


```json

{

  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
  },
  "ConnectionStrings": {
    "Postgres": "Host=localhost;Port=5432;Database=ticketing;Username=postgres;Password=postgres;Include Error Detail=true;"
  },
  "Redis": {
    "Connection": "localhost:6379"
  }

}

```