# OutBox-Project-in-.NET

This project is an implementation of the **Outbox pattern** in .NET with an MVC-style architecture (controllers, services, models) backed by SQL Server or PostgreSQL, and integrated with **Debezium** and **Apache Kafka** for reliable event streaming.

The main goal is to **safely publish wallet transactions as events** by first persisting them in a local `OutboxMessages` table, and then letting Debezium stream these changes into Kafka topics for downstream consumers.

---

## What is the Outbox Pattern?

In a typical microservice or layered architecture, you often need to:

1. Save state in a database (e.g., a wallet debit/credit).
2. Publish an event (e.g., “WalletDebited”) to a message broker like Kafka.

If you do (1) and (2) separately, you risk **inconsistency** when one succeeds and the other fails (the classic *dual-write* problem).

The **Outbox pattern** solves this by:

1. Writing both the business data **and** an “outbox message” to the **same database transaction**.
2. Having a separate process (Debezium in this project) **reliably read outbox rows** and publish them to Kafka.

This way, your service only worries about its local database transaction; the infrastructure (Debezium + Kafka) handles the rest.

---

## Project Structure (High Level)

- **ASP.NET MVC / Web API**  
  - **Controllers**: HTTP endpoints for user/wallet operations.  
  - **Services**: Business logic for users, wallets, and transactions.  
  - **Models / Entities**: `User`, `Wallet`, `Transaction`, `TransactionLog`, `OutboxMessage`.  

- **OutboxBroker (integration side)**  
  Supporting code to integrate with Kafka / Debezium setup.

- **Infrastructure**  
  - `docker-compose.yml` – spins up Kafka, Zookeeper, Kafka Connect (+ Debezium), and databases.
  - `mssql-connector.json` – Debezium SQL Server connector config.
  - `postgres-connector.json` – Debezium PostgreSQL connector config.

---

## Database Model

The project uses 5 main tables that model a simple wallet system with an outbox for events.

### 1. `Users`

- Represents an application user.
- Primary key: `Id`.

### 2. `Wallets`

- Each **user** has their own **wallet** (or wallets) with a unique wallet ID.
- Primary key: `Id` (this is also the **aggregate ID** used in the outbox).
- Foreign key: `UserId` → `Users(Id)`.

### 3. `Transactions`

- Represents a **business transaction** (credit or debit) on a wallet.
- Typical columns:
  - `Id`
  - `WalletId`
  - `Amount`
  - `Type` (Credit/Debit)
  - `CreatedAt`

### 4. `TransactionLogs`

- Stores a detailed log of wallet transactions (audit trail).
- Logs **amount**, **date/time**, and possibly **previous/new balances**.
- Linked to `Transactions` (e.g., `TransactionId`).

### 5. `OutboxMessages` (Core of the Outbox Pattern)

This is the **most important table**. Every time a transaction occurs, an outbox message is written in the same DB transaction.

**Columns (conceptually):**

- `Id`  
  Unique identifier for the outbox message.

- `TransactionId`  
  The business transaction this message refers to.

- `UserId`  
  The user who owns the wallet / transaction.

- `AggregateId`  
  The **aggregate root ID**, e.g. the `WalletId`. This lets consumers group events by wallet.

- `Sequence`  
  The **position** of the message in the sequence for the aggregate.  
  Examples: `1`, `2`, `3`, …  
  This provides ordering of events per wallet.

- `Type`  
  Event type – **`Debit`** or **`Credit`**.

- `Status`  
  Life-cycle state of the outbox message:
  - `0 = New` – freshly written by the app, not yet published.
  - `1 = Publishing` – currently being processed/published.
  - `2 = Dead` – failed permanently (dead letter, for manual investigation).

**Flow:**

1. User performs an operation → transaction is created and applied to the wallet.
2. The same DB transaction inserts a row into `OutboxMessages`.
3. Debezium reads changes from `OutboxMessages` and sends them to Kafka.
4. Kafka exposes them as events for downstream consumers, split into topics (e.g., by table or type).

---

## Debezium & Kafka Overview

[Debezium](https://github.com/debezium/debezium) is an open-source **change data capture (CDC)** platform built on **Kafka Connect**. It monitors database transaction logs and emits events to Kafka whenever rows are inserted, updated, or deleted. :contentReference[oaicite:0]{index=0}  

In this project:

- Debezium watches **only the `OutboxMessages` table**.
- Each insert (or update) to `OutboxMessages` becomes a Kafka event.
- Consumer services subscribe to Kafka topics and react to those events (e.g., send emails, update read models, sync with other systems, etc.).

---

## Connector Configuration Files

At the repository root:

- **`mssql-connector.json`**  
  Debezium connector configuration for **SQL Server**.

- **`postgres-connector.json`**  
  Debezium connector configuration for **PostgreSQL**.

Both files are **sample configs**.  
You must update the following fields (names may vary slightly depending on the connector version):

- `database.hostname`
- `database.port`
- `database.user`
- `database.password`
- `database.dbname`
- `database.server.name` / `topic.prefix` (logical name used in topic naming)
- `table.include.list` or filters to include `OutboxMessages`
- For PostgreSQL: `publication.name` / `slot.name` etc.

> After editing, you’ll POST these JSON files to Kafka Connect to register the connectors.

---

## Getting Started (Step-by-Step)

This section explains how to bring everything up **end-to-end**:

1. Run the .NET app.
2. Configure SQL Server *or* PostgreSQL for Debezium.
3. Register Debezium connectors.
4. Verify Kafka topics and read the outbox events.

All commands shown here work in:

* **Linux/macOS**: in a normal shell (`bash`, `zsh`, etc).
* **Windows**: in **CMD** or **PowerShell** (no changes needed unless noted).

---

## 1. Run the .NET Application

### 1.1. Clone and build

```bash
git clone https://github.com/AlirezaEslamikhah/OutBox-Project-in-.NET.git
cd OutBox-Project-in-.NET

dotnet restore
dotnet build
```

### 1.2. Configure database connection

Update the connection string in `appsettings.json` (or your desired config):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=WalletDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;"
  }
}
```

For PostgreSQL:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=walletsystemdb;Username=postgres;Password=postgres;"
  }
}
```

### 1.3. Apply migrations / create schema

If you have EF Core migrations:

```bash
dotnet ef database update
```

Otherwise, create the 5 main tables manually:

* `Users`
* `Wallets`
* `Transactions`
* `TransactionLogs`
* `OutboxMessages`

### 1.4. Run the application

```bash
dotnet run --project "OutBox Project/OutBox Project.csproj"
```

---

## 2. SQL Server + Debezium

### 2.1. Enable CDC on database

```bash
sqlcmd -S localhost,1433 -U sa -P "YourStrong!Passw0rd" -Q "USE WalletDb; EXEC sys.sp_cdc_enable_db;"
```

### 2.2. Enable CDC on `OutboxMessages` table

```bash
sqlcmd -S localhost,1433 -U sa -P "YourStrong!Passw0rd" ^
  -Q "USE WalletDb; EXEC sys.sp_cdc_enable_table @source_schema = N'dbo', @source_name = N'OutboxMessages', @role_name = NULL, @supports_net_changes = 0;"
```

(on Linux/macOS you can write it as a single line without `^`)

### 2.3. Verify CDC configuration

```bash
sqlcmd -S localhost,1433 -U sa -P "YourStrong!Passw0rd" -Q "USE WalletDb; EXEC sys.sp_cdc_help_change_data_capture;"
```

### 2.4. Start Docker stack (Kafka, Connect, etc.)

```bash
docker-compose up -d
docker-compose ps
```

Stop stack:

```bash
docker-compose down
```

### 2.5. Configure and register SQL Server Debezium connector

Edit `mssql-connector.json` (example):

```json
{
  "name": "sqlserver-outbox-connector",
  "config": {
    "connector.class": "io.debezium.connector.sqlserver.SqlServerConnector",
    "database.hostname": "sqlserver",
    "database.port": "1433",
    "database.user": "sa",
    "database.password": "YourStrong!Passw0rd",
    "database.names": "WalletDb",
    "database.server.name": "wallet-sqlserver",
    "table.include.list": "WalletDb.dbo.OutboxMessages",
    "snapshot.mode": "initial",
    "poll.interval.ms": "1000"
  }
}
```

Register connector:

```bash
curl -i -X POST \
  -H "Accept: application/json" \
  -H "Content-Type: application/json" \
  http://localhost:8083/connectors \
  --data @mssql-connector.json
```

Check connector status:

```bash
curl -s http://localhost:8083/connectors
curl -s http://localhost:8083/connectors/sqlserver-outbox-connector/status
```

Delete connector (optional):

```bash
curl -X DELETE http://localhost:8083/connectors/sqlserver-outbox-connector
```

---

## 3. PostgreSQL + Debezium

### 3.1. Connect to PostgreSQL

Docker:

```bash
docker-compose exec postgres psql -U postgres -d walletsystemdb
```

Local:

```bash
psql -h localhost -U postgres -d walletsystemdb
```

### 3.2. Check WAL / replication settings

```sql
SHOW wal_level;
SHOW max_replication_slots;
SHOW max_wal_senders;
SHOW wal_level;
SHOW data_directory;
```

### 3.3. Enable logical replication

```sql
ALTER SYSTEM SET wal_level = 'logical';
ALTER SYSTEM SET max_replication_slots = 10;
ALTER SYSTEM SET max_wal_senders = 10;
SELECT pg_reload_conf();
```

### 3.4. Create publication for `OutboxMessages` (insert + update only)

```sql
CREATE PUBLICATION wallet_pub_insupd
  FOR TABLE public."OutboxMessages"
  WITH (publish = 'insert,update');
```

Check publication:

```sql
SELECT pubname, pubinsert, pubupdate, pubdelete, pubtruncate
FROM pg_publication
WHERE pubname = 'wallet_pub_insupd';
```

Update existing publication (if already created):

```sql
ALTER PUBLICATION wallet_pub_insupd
SET (publish = 'insert,update');
```

### 3.5. Helper queries (debug / cleanup)

Only stream `insert` and `update`, ignore deletes; for tests you can still delete manually:

```sql
DELETE FROM public."OutboxMessages"
WHERE "Sequence" = 5;
```

Check outbox content:

```sql
SELECT * FROM public."OutboxMessages"
ORDER BY "Id" ASC
LIMIT 100;
```

Drop replication slot (if you need to reset Debezium):

```sql
SELECT pg_drop_replication_slot('walletsystemdb_slot');
```

### 3.6. Configure and register PostgreSQL Debezium connector

Edit `postgres-connector.json` (example):

```json
{
  "name": "postgres-outbox-connector",
  "config": {
    "connector.class": "io.debezium.connector.postgresql.PostgresConnector",
    "database.hostname": "postgres",
    "database.port": "5432",
    "database.user": "postgres",
    "database.password": "postgres",
    "database.dbname": "walletsystemdb",
    "database.server.name": "wallet-postgres",
    "slot.name": "walletsystemdb_slot",
    "publication.name": "wallet_pub_insupd",
    "table.include.list": "public.OutboxMessages",
    "slot.drop.on.stop": "false",
    "publication.autocreate.mode": "disabled",
    "tombstones.on.delete": "false",
    "snapshot.mode": "initial"
  }
}
```

Register connector:

```bash
curl -i -X POST \
  -H "Accept: application/json" \
  -H "Content-Type: application/json" \
  http://localhost:8083/connectors \
  --data @postgres-connector.json
```

Check connector status:

```bash
curl -s http://localhost:8083/connectors
curl -s http://localhost:8083/connectors/postgres-outbox-connector/status
```

Delete connector (optional):

```bash
curl -X DELETE http://localhost:8083/connectors/postgres-outbox-connector
```

---

## 4. Kafka: Topics and Logs

### 4.1. List topics

```bash
docker-compose exec kafka \
  kafka-topics.sh --bootstrap-server kafka:9092 --list
```

### 4.2. Consume outbox topic

Replace `<outbox-topic-name>` with the actual topic (for example `wallet-postgres.public.OutboxMessages` or `wallet-sqlserver.dbo.OutboxMessages`):

```bash
docker-compose exec kafka \
  kafka-console-consumer.sh \
  --bootstrap-server kafka:9092 \
  --topic <outbox-topic-name> \
  --from-beginning
```

### 4.3. View Kafka Connect logs (Debezium)

```bash
docker-compose logs -f connect
```

### 4.4. View Kafka logs

```bash
docker-compose logs -f kafka
```

---

## 5. End-to-End Test

### 5.1. Start infrastructure

```bash
docker-compose up -d
docker-compose ps
```

### 5.2. Run the .NET app

```bash
dotnet run --project "OutBox Project/OutBox Project.csproj"
```

### 5.3. Trigger wallet operations

Call your API endpoints (examples):

```bash
# example: credit wallet endpoint
curl -X POST http://localhost:5000/api/wallet/credit \
  -H "Content-Type: application/json" \
  -d '{"userId":1,"walletId":1,"amount":1000}'
```

### 5.4. Check `OutboxMessages` in database

SQL Server:

```bash
sqlcmd -S localhost,1433 -U sa -P "YourStrong!Passw0rd" -Q "SELECT TOP (20) * FROM dbo.OutboxMessages ORDER BY Id DESC;"
```

PostgreSQL:

```bash
docker-compose exec postgres \
  psql -U postgres -d walletsystemdb \
  -c 'SELECT * FROM public."OutboxMessages" ORDER BY "Id" DESC LIMIT 20;'
```

### 5.5. Consume Kafka topic and see events

```bash
docker-compose exec kafka \
  kafka-console-consumer.sh \
  --bootstrap-server kafka:9092 \
  --topic <outbox-topic-name> \
  --from-beginning
```

You should see messages for each new outbox row, including:

* `TransactionId`
* `UserId`
* `AggregateId` (wallet id)
* `Sequence`
* `Type` (debit/credit)
* `Status` (0, 1, 2)

---

## 6. Troubleshooting

### 6.1. Connector not running

```bash
curl -s http://localhost:8083/connectors
curl -s http://localhost:8083/connectors/sqlserver-outbox-connector/status
curl -s http://localhost:8083/connectors/postgres-outbox-connector/status
```

### 6.2. No messages in Kafka

```bash
docker-compose logs -f connect
docker-compose logs -f kafka
```

Check:

* CDC enabled (SQL Server)
* `wal_level = logical` and publication (PostgreSQL)
* `OutboxMessages` is included in `table.include.list`
* New rows are actually inserted in `OutboxMessages`

### 6.3. Reset replication slot (PostgreSQL)

```bash
docker-compose exec postgres \
  psql -U postgres -d walletsystemdb \
  -c "SELECT pg_drop_replication_slot('walletsystemdb_slot');"
```

Restart the PostgreSQL connector after dropping the slot.
