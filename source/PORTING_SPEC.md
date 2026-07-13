# Linux Port Spec — trucksim-gps-server

Authoritative plan for porting the telemetry server from Windows/.NET Framework 4.8
to Linux. This file is the single source of truth. Update it as work lands; it must
survive a full loss of conversation context so any engineer (human or agent) can
resume with zero prior knowledge.

## 0. Binding principles (non-negotiable, apply to every change and every review)

- SOLID, DRY, YAGNI, KISS, Clean Code, Pragmatic Programmer.
- **No comments.** Code must be self-descriptive through naming and structure. The
  only permitted comment explains a genuinely counter-intuitive hack or externally
  imposed weird behaviour that is unreadable without it.
- **Atomic commits.** One logical, human-reviewable change per commit. Never land a
  phase in a single commit. A development cycle is: *atomic change → review against
  these principles → commit*. Nothing lands without the review step.
- **Token-resilient.** Keep the tree buildable at each commit from Phase A5 onward
  (see below). This spec plus committed increments are the recovery point — never
  leave large uncommitted work in flight.

## 1. Target decisions (locked)

| Area | Decision |
|------|----------|
| Runtime | Native Linux game build + native `.so` plugin |
| Shared memory | POSIX shm, file-backed at `/dev/shm/TSGPSTelemetry`, mapped read-only via `MemoryMappedFile.CreateFromFile` |
| UI | Headless console daemon (foreground process, systemd-friendly, logs to file + stdout) |
| Codebase | Linux-first, single `net8.0` project. The Windows WinForms app is not preserved. |
| Setup scope | Minimal. No firewall / URL reservation / VCRedist / auto-update. Plugin install is manual (documented). |
| Framework | `net8.0`, SDK-style project, `PackageReference` |
| Namespace/layout | Unchanged (`Funbit.Ets.Telemetry.Server`) — renaming is out of scope (YAGNI). |
| Logging | Keep `log4net` (has net8-compatible build); configure programmatically from a standalone `log4net.config`. Preserves all existing `Log.*` call sites (DRY). |
| Config | Replace `ConfigurationManager`/`App.config` with a typed `ServerConfig` read from environment variables with sane defaults (12-factor, systemd-friendly). |

## 2. Architecture (target)

Data path (unchanged in shape, this is already the only live serving path):

```
Program (console host)
  → TelemetryServerHost        (lifecycle: start/stop server, status + broadcast timers)
    → MinimalHttpServer         (raw TcpListener, port 31377)
      → Ets2TelemetryController.GetEts2TelemetryJson()   (static)
      → Ets2AppController.GetStatusPageHtml()            (static)
        → ScsTelemetryDataReader (singleton)
          → SharedMemory (CreateFromFile /dev/shm/TSGPSTelemetry, read-only)
          → SCSSdkClient/*  (byte → struct parsing, already portable)
          → Ets2ProcessHelper (process scan)
```

Keep as-is (already portable): `SCSSdkClient/*`, `MinimalHttpServer`, `TelemetryV1`,
`ScsTelemetryDataReader` (mapping logic), `JsonHelper`, `NetworkHelper`, `Settings`.

## 3. Development cycles (each row = one atomic commit = one review)

Build stays GREEN under `dotnet build` from A5 onward. A2–A4 keep the legacy build
compiling; A5 flips the project to net8.0 with Windows files excluded before A6
deletes them, so no commit is left un-buildable on the target.

### Phase A — Reach a green net8.0 headless console build

- **A1** chore: baseline. Confirm `dotnet` SDK ≥ 8 available; ensure `bin/`,`obj/`
  ignored. No functional change.
- **A2** Decouple controllers from dead OWIN/SignalR/WebApi. Delete `Startup.cs` and
  `Controllers/Ets2TelemetryHub.cs`; remove `ApiController` base + `[RoutePrefix]`/
  `[Route]` attributes and `System.Web.Http` usings from `Ets2TelemetryController`
  and `Ets2AppController`. Their static methods are the only thing used.
- **A3** Introduce `Helpers/ServerConfig.cs` (env vars + defaults: `TSGPS_PORT`=31377,
  `TSGPS_SHM_PATH`=/dev/shm/TSGPSTelemetry, `TSGPS_USE_TEST_TELEMETRY`=false,
  `TSGPS_BROADCAST_URL`, `TSGPS_BROADCAST_RATE`=10, `TSGPS_BROADCAST_USER`/`_PASSWORD`).
  Replace all `ConfigurationManager.AppSettings[...]` reads.
- **A4** Extract `TelemetryServerHost` from `MainForm`: platform-agnostic lifecycle
  (start/stop `MinimalHttpServer`, status + broadcast via `System.Threading.Timer` and
  `HttpClient`). `MainForm` delegates to it (legacy build still compiles).
- **A5** Convert to SDK-style `net8.0` console project. New `.csproj`
  (`OutputType=Exe`, `PackageReference` for Newtonsoft.Json + log4net,
  `<Compile Remove>` for every Windows-only file). Delete old `Program.cs`,
  `App.config`, `packages.config`. Add console `Program.cs` (runs `TelemetryServerHost`,
  handles SIGINT/SIGTERM for graceful shutdown) and `log4net.config` (programmatic
  configure). **Milestone: `dotnet build` + `dotnet run` succeed on Linux.**
- **A6** Delete now-orphaned Windows-only files: `MainForm*`, `SetupForm*`,
  `UpdateForm`, `Helpers/Win32Uac.cs`, `Helpers/KeyboardHelper.cs`,
  `Helpers/ProcessHelper.cs`, `Setup/*`, `Resources/*`, `*.resx`, `installer/*`.
  Build stays green (already excluded in A5).

### Phase B — Linux shared-memory transport

- **B1** Change `SharedMemory.Connect` to open a file-backed read-only mapping via
  `MemoryMappedFile.CreateFromFile(path, FileMode.Open, null, size, MemoryMappedFileAccess.Read)`.
  Absent file → `Hooked=false` (no throw to caller).
- **B2** Add reconnect-on-read in `ScsTelemetryDataReader`: when not hooked, retry
  `Connect` (game/plugin may start after the server). Source the map path from
  `ServerConfig.SharedMemoryPath`.
- **B3** Verify/adjust `Ets2ProcessHelper` for Linux: process names `eurotrucks2`/
  `amtrucks` already match native binaries; game-root detection via `base.scs`+`bin`
  is path-agnostic. Change only what a Linux run proves wrong.

### Phase C — Runtime & packaging

- **C1** Graceful shutdown verified; single-instance only if a real need surfaces
  (default: drop the Win32 mutex, add nothing — YAGNI).
- **C2** Add a `systemd` unit file and Linux build/run instructions to `Readme.md`.
- **C3** Confirm test-telemetry mode (`TSGPS_USE_TEST_TELEMETRY=true`) and status page.

### Phase D — Verification

- **D1** End-to-end on Linux: `dotnet run`, `curl http://localhost:31377/` and
  `/api/ets2/telemetry` (test-telemetry mode, no game required) → valid JSON.
  Run the `verify` skill. Record result here.

## 4. Review checklist (run every cycle before commit)

1. Principles honoured (Section 0), especially: no comments, single responsibility,
   no speculative generality (YAGNI), simplest thing that works (KISS).
2. Commit is one logical change, message states the *why*, diff is human-reviewable.
3. `dotnet build` green (A5+). No new warnings introduced.
4. No Windows-only API reintroduced.
5. Spec updated if scope/decision changed.

## 5. Status log (append one line per landed commit)

- (none yet — Phase A not started)
