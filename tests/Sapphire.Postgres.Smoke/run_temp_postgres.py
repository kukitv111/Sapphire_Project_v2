"""Run production migrations and DB checks on isolated Compose volumes."""

import os
import subprocess
import uuid
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
COMPOSE = Path(__file__).with_name("docker-compose.smoke.yml")
PROJECT = "sapphire-smoke-" + uuid.uuid4().hex[:10]
PASSWORD = "disposable-smoke-" + uuid.uuid4().hex
ENV = os.environ.copy()
ENV["SAPPHIRE_SMOKE_DB_PASSWORD"] = PASSWORD
ENV["ASPNETCORE_ENVIRONMENT"] = "Development"
ENV["Jwt__SecretKey"] = "DisposableSmokeJwtKeyThatIsAtLeast32CharactersLong"
ENV["Messaging__SharedSecret"] = "DisposableSmokeTransportKeyAtLeast32CharactersLong"


def run(*args, env=ENV):
    result = subprocess.run(args, cwd=ROOT, env=env, text=True, capture_output=True)
    if result.returncode:
        raise RuntimeError(
            f"{' '.join(args[:3])} failed (exit {result.returncode}): "
            + (result.stdout + result.stderr).replace(PASSWORD, "[redacted]")[-7000:]
        )
    return result.stdout.strip()


def compose(*args):
    return run("docker", "compose", "-f", str(COMPOSE), "-p", PROJECT, *args)


try:
    compose("up", "-d", "--wait", "auth-db", "billing-db", "session-db")
    for service in ("AUTH", "BILLING", "SESSION"):
        name = service.lower()
        port = compose("port", f"{name}-db", "5432").rsplit(":", 1)[-1]
        ENV[f"SAPPHIRE_TEST_{service}_CONNECTION"] = (
            f"Host=127.0.0.1;Port={port};Database=sapphire_{name}_smoke;"
            f"Username=postgres;Password={PASSWORD}"
        )
        migration_env = ENV.copy()
        migration_env["ConnectionStrings__DefaultConnection"] = ENV[
            f"SAPPHIRE_TEST_{service}_CONNECTION"
        ]
        project = f"src/services/{service}/Sapphire.{service}.Api/Sapphire.{service}.Api.csproj"
        run("dotnet", "run", "--project", project, "-c", "Release", "--", "--migrate", env=migration_env)
        print(f"{service}: --migrate passed on empty Compose PostgreSQL")

    smoke_project = "tests/Sapphire.Postgres.Smoke/Sapphire.Postgres.Smoke.csproj"
    print(run("dotnet", "run", "--project", smoke_project, "-c", "Release"))

    writer = subprocess.Popen(
        ["dotnet", "run", "--no-build", "--project", smoke_project, "-c", "Release", "--", "--crash-write"],
        cwd=ROOT, env=ENV, text=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
    )
    try:
        marker = writer.stdout.readline().strip()
        if not marker.startswith("TX_WRITTEN:"):
            raise RuntimeError(f"Crash writer did not reach uncommitted write: {marker}")
        wallet_id = marker.split(":", 1)[1]
    finally:
        writer.kill()
        writer.communicate(timeout=10)
    print(run("dotnet", "run", "--no-build", "--project", smoke_project,
              "-c", "Release", "--", "--verify-crash", wallet_id))
finally:
    subprocess.run(
        ["docker", "compose", "-f", str(COMPOSE), "-p", PROJECT,
         "down", "--volumes", "--remove-orphans"],
        cwd=ROOT, env=ENV, capture_output=True, text=True,
    )
