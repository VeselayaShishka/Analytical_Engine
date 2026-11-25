# Analytical Engine

A full-stack web application for managing, analysing, and visualising open data sets. Key capabilities include:

- **Domain model** – `User`, `Dataset`, `DatasetVersion`, `DatasetColumn`, `DataRecord`, `Report`, `ErrorLog`.
- **Roles & RBAC** – `Analyst` & `DataAdministrator` (plus a seeded admin account).
- **Streaming analytics** – custom Welford’s algorithm for online variance and standard deviation ($O(N)$ time).
- **REST API** – documented with OpenAPI (Swagger) and secured by JWT Bearer authentication.
- **Frontend** – React 19 + Vite + Mantine UI, using TanStack Query/Table and Recharts.
- **Automated testing** – 76 passing unit and integration tests covering services, controllers, and import pipelines.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Environment Configuration](#environment-configuration)
3. [Running with Docker (Recommended)](#running-with-docker-recommended)
4. [Running Locally (Without Docker)](#running-locally-without-docker)
5. [API Documentation (Swagger)](#api-documentation-swagger)
6. [Testing](#testing)
7. [Sample Data](#sample-data)
8. [Contribution & License](#contribution--license)

---

## Prerequisites

| Tool | Minimum Version |
| :--- | :--- |
| **.NET SDK** | 10.0 (`10.0.100` or newer) |
| **Node.js** | 20.x |
| **Docker Desktop** | 4.30+ (for containerized deployment) |
| **PostgreSQL client** | Any (optional, for local inspection) |

Verify your .NET installation:

```bash
dotnet --version
```

---

## Environment Configuration

Copy the example environment file and adjust values if needed:

```bash
cp .env.example .env
```

### Key Variables

| Variable | Meaning |
| :--- | :--- |
| `POSTGRES_DB` | Database name (`analytical_engine` by default) |
| `POSTGRES_USER` / `POSTGRES_PASSWORD` | PostgreSQL credentials |
| `JWT_SECRET` | Secret key (32+ characters) for signing JWT tokens |
| `JWT_ISSUER` / `JWT_AUDIENCE` | Token issuer and audience metadata |
| `SEED_ADMIN_USERNAME` / `SEED_ADMIN_EMAIL` / `SEED_ADMIN_PASSWORD` | Administrator account created on startup |
| `FRONTEND_PORT` / `BACKEND_PORT` / `DB_PORT` | Host ports exposed by Docker Compose |

---

## Running with Docker (Recommended)

Build and start all services (PostgreSQL, .NET API, and Nginx + React UI):

```bash
docker compose up --build -d
```

Once running, the services are available at:

- **Frontend UI:** http://localhost:3000 (or `FRONTEND_PORT`)
- **Backend API:** http://localhost:5000/api (or `BACKEND_PORT`)
- **Swagger UI:** http://localhost:5000/swagger

On first startup, EF Core automatically applies database migrations and seeds the administrator account using the credentials from `.env`.

To stop and remove containers:

```bash
docker compose down
```

---

## Running Locally (Without Docker)

1. **Database:** Start a local PostgreSQL instance (default port `5432`) and create a database named `analytical_engine`.
2. **Apply EF Core migrations:**
   ```bash
   cd Analytical_Engine
   dotnet ef database update
   ```
3. **Run the backend API:**
   ```bash
   dotnet run --project Analytical_Engine/Analytical_Engine.csproj
   ```
4. **Run the frontend development server:**
   ```bash
   cd frontend
   npm install
   npm run dev
   ```

The backend API will be reachable at `http://localhost:5000/api`, and the frontend at `http://localhost:5173`.

---

## API Documentation (Swagger)

Swagger UI is enabled automatically in `Development` mode:

- **URL:** `http://localhost:<BACKEND_PORT>/swagger`
- Displays all endpoints, request/response schemas, RFC 9457 error models, and the **Authorize** button for JWT Bearer tokens.
- XML documentation comments from controllers appear as descriptions for each endpoint.

---

## Testing

Run the automated xUnit test suite:

```bash
dotnet test Analytical_Engine.Tests/Analytical_Engine.Tests.csproj
```

All 76 tests should pass. *(Warnings regarding EF Core minor version resolution in test dependencies do not affect test execution.)*

---

## Sample Data

A demo dataset (`TestData/ITU_GCI.csv`) is included in the repository. Use the **Import → Dataset** screen in the web UI to upload it, inspect rows, and build charts in the **Analytics Builder**.

---

## Contribution & License

- Fork the repository, create a feature branch, and submit a Pull Request.
- Follow the project code style (C# 12/13, nullable reference types enabled, `async/await`).
- Licensed under the **MIT License** – see `LICENSE` for details.
