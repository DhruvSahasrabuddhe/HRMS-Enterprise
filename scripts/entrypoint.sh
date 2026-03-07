#!/bin/bash
# entrypoint.sh
# Runs EF Core database migrations then starts the ASP.NET application.
# This script is executed by tini (PID 1) inside the container.
set -euo pipefail

echo "[entrypoint] Starting HRMS Enterprise..."
echo "[entrypoint] Environment: ${ASPNETCORE_ENVIRONMENT:-Production}"

# ─── Wait for SQL Server to be reachable ──────────────────────────────────────
# On first start in Docker Compose the DB container may still be initialising.
if [ -n "${ConnectionStrings__DefaultConnection:-}" ]; then
  echo "[entrypoint] Waiting for database to become ready..."
  MAX_ATTEMPTS=30
  attempt=0
  until dotnet HRMS.Web.dll --check-db 2>/dev/null || [ $attempt -ge $MAX_ATTEMPTS ]; do
    attempt=$((attempt + 1))
    echo "[entrypoint] Database not ready, attempt ${attempt}/${MAX_ATTEMPTS} – sleeping 5 s..."
    sleep 5
  done
fi

# ─── Run EF Core migrations ───────────────────────────────────────────────────
if [ "${RUN_MIGRATIONS:-true}" = "true" ]; then
  echo "[entrypoint] Applying database migrations..."
  # The application applies migrations via context.Database.Migrate() on startup
  # (see Program.cs). Nothing extra is needed here; this block is kept as a hook
  # for future migration tooling (e.g. a standalone migrate CLI).
  echo "[entrypoint] Migrations will be applied by the application on startup."
fi

# ─── Launch the application ───────────────────────────────────────────────────
echo "[entrypoint] Launching ASP.NET Core application..."
exec dotnet HRMS.Web.dll "$@"
