# Facet

Facet is a full-stack document privacy application built as a university team project. It lets users upload documents, detect sensitive information, review detections, and create redacted copies.

> **Project status:** portfolio / academic project. It is not presented as a production-ready service. Review the known limitations in [SECURITY.md](SECURITY.md) before using it with real documents or exposing it to the internet.

## What it does

- Uploads and manages PDF and DOCX documents.
- Detects sensitive values such as Estonian personal identification codes, email addresses, phone numbers, and IBANs.
- Supports reviewing detections and redacting selected values, including saving a redacted copy.
- Provides user registration and JWT-based sign-in, plus user administration screens.
- Offers an interface in Estonian, English, and Russian.
- Includes API request tests in Postman/Newman.

## Technology

| Area | Tools |
| --- | --- |
| Backend | C#, ASP.NET Core 9, Entity Framework Core, PostgreSQL |
| Frontend | Nuxt 4, Vue 3, TypeScript, Pinia, Tailwind CSS |
| Documents | PDF and DOCX processing libraries |
| Local environment | Docker Compose |
| API testing | Postman collection, Newman |

## Architecture

```text
Browser (Nuxt/Vue) → ASP.NET Core REST API → PostgreSQL
                                      ↘ document storage volume
```

The frontend calls the API using the configured public API base URL. The API stores document metadata in PostgreSQL and uploaded files in its storage directory.

## Run locally with Docker

Requirements: Docker with the Compose plugin.

1. Copy `.env.example` to `.env` and set unique values for `JWT_KEY` (at least 32 characters) and `BOOTSTRAP_ADMIN_PASSWORD`. The bootstrap administrator is created only if both bootstrap variables are set and the username does not already exist. These values are for local development only.
2. From the repository root, run:

   ```bash
   docker compose up --build
   ```

3. Open the frontend at [http://localhost:3000](http://localhost:3000). The API is available at [http://localhost:5000](http://localhost:5000), with Swagger at [http://localhost:5000/swagger](http://localhost:5000/swagger) in the Development environment.

To stop the services, run `docker compose down`. Add `-v` only if you also want to delete the local database and uploaded documents.

## Run the frontend separately

```bash
cd Frontend
corepack enable
pnpm install --frozen-lockfile
NUXT_PUBLIC_API_BASE=http://localhost:5000/api pnpm dev
```

On Windows PowerShell, set the environment variable first with `$env:NUXT_PUBLIC_API_BASE="http://localhost:5000/api"`.

## API tests

`ApiTests` contains a Postman collection and a Compose setup for Newman. From `ApiTests`, run:

```bash
docker compose up --abort-on-container-exit --exit-code-from newman
docker compose down -v
```

The test environment uses disposable local database credentials. Do not reuse them outside isolated local testing.

## Repository layout

```text
Backend/FacetApi/     ASP.NET Core API and EF Core migrations
Frontend/             Nuxt application
ApiTests/             Postman/Newman API tests and sample documents
POSTGRES/             Standalone PostgreSQL/pgAdmin development setup
```

## Team project and contribution

Facet was developed collaboratively. My contributions, as reflected in the project history, include:

- Developing and refining document upload, preview, listing, deletion, and redaction flows across the API and frontend.
- Building sensitive-data review features, including custom pattern search and risk indicators.
- Improving responsive layouts and multilingual interface behavior.
- Updating API test requests and supporting local container configuration.

Other contributors also worked on the project; the repository history shows the shared development work.

## License

No license has been added yet. Until the authors choose one, this repository is publicly viewable but reuse and redistribution are not granted by an open-source license.
