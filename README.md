# CallInsights

Upload call recordings, transcribe them, and generate insights from each call.

## How it works

1. A recording is uploaded through the web app.
2. **Process** queues it. The API runs a pipeline: prepare audio (ffmpeg) → transcribe → label speakers → build prompt → generate insights.
3. Progress is pushed live to the browser over SignalR.
4. Transcripts and insights are stored in MySQL and shown in the app.

Transcription and the other model steps run on an external GPU server (configured by `Transcription:BaseUrl`), not inside this repo's containers.

## Tech stack

- **Backend:** ASP.NET Core (.NET 10), EF Core with MySQL (Pomelo), SignalR, FFMpegCore
- **Frontend:** Svelte 5, Vite, Tailwind CSS
- **Deployment:** Docker Compose (MySQL, API, nginx serving the frontend)

## Project structure

```
CallInsights/
├── Core/           shared options, constants, helpers
├── DataLayer/      EF Core DbContext, entities, migrations
├── ServiceLayer/   recording service, pipeline steps, transcription/insights services
├── UI.Web.Api/     controllers, SignalR hub, background worker
├── UI.Web.App/     Svelte frontend (+ nginx config and Dockerfile)
├── Dockerfile.api
└── docker-compose.yml
```

## Run with Docker

Requirements: Docker with Compose.

1. Copy the example environment file and fill in real values:

   ```bash
   cp .env.example .env
   ```

   | Variable | Purpose |
   |---|---|
   | `MYSQL_ROOT_PASSWORD`, `MYSQL_DATABASE`, `MYSQL_USER`, `MYSQL_PASSWORD` | Used by the MySQL container on its first start |
   | `ConnectionStrings__DefaultConnection` | How the API connects to MySQL (host is `db`). Must match the `MYSQL_*` values |
   | `Transcription__ApiKey` | API key for the external transcription server |

   Avoid `;`, `=`, `$`, `#`, spaces and quotes in passwords.

2. Start everything:

   ```bash
   docker compose up -d --build
   ```

3. Open `http://localhost`.

Notes:
- Database migrations are applied automatically when the API starts.
- Recordings, transcriptions and working files are stored in `./data/Recordings`, `./data/Transcriptions` and `./data/Work`.
- MySQL data is kept in the `db-data` Docker volume.
- Stop with `docker compose down`. Do **not** add `-v` unless you want to delete the database.
- The API image downloads a Linux LGPL ffmpeg build during `docker build`.

## Local development

Requirements: .NET 10 SDK, Node.js 22, a MySQL server, and the Windows or Linux ffmpeg build (set `Audio:FfmpegFolder` to the folder containing `ffmpeg` and `ffprobe`).

Store secrets with user secrets, not in `appsettings.json`:

```bash
cd UI.Web.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "server=localhost;port=3306;database=callinsights;uid=<user>;password=<password>;"
dotnet user-secrets set "Transcription:ApiKey" "<key>"
```

Run the API (Visual Studio, or `dotnet run` in `UI.Web.Api`), then the frontend:

```bash
cd UI.Web.App
npm install
npm run dev
```

The frontend runs at `http://localhost:5173`.

## Configuration

Main settings in `UI.Web.Api/appsettings.json`:

| Section | Setting | Meaning |
|---|---|---|
| `Storage` | `RecordingsFolder`, `TranscriptionsFolder`, `MaxFileSizeMb` | Where files are stored and the upload size limit |
| `Audio` | `FfmpegFolder`, `WorkFolder`, `SampleRate` | ffmpeg location, scratch folder, preprocessing sample rate |
| `Transcription` | `BaseUrl`, `TimeoutSeconds`, `Language`, `Prompt` | External transcription server settings |
| `Diarization` | `NumSpeakers`, `AgentSpeaksFirst` | Speaker labelling |
| `Llm` | `Temperature`, `MaxTokens` | Insight generation |
| `Processing` | `MaxConcurrency` | Recordings processed in parallel |

Any setting can be overridden with an environment variable using `__` as the separator, for example `Storage__RecordingsFolder`.

## Deploying to a server

1. Install Docker and Docker Compose.
2. Clone this repository.
3. Create `.env` from `.env.example` with strong, unique values.
4. Open port 80 in the server firewall and cloud security rules.
5. Run `docker compose up -d --build`.