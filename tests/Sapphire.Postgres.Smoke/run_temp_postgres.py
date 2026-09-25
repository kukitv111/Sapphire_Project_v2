"""Run production migrations and DB checks on isolated Compose volumes."""

import os
import subprocess
import time
import uuid
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
COMPOSE = Path(__file__).with_name("docker-compose.smoke.yml")
PROJECT = "sapphire-smoke-" + uuid.uuid4().hex[:10]
PASSWORD = "disposable-smoke-" + uuid.uuid4().hex
RUNTIME_PASSWORD = "runtime-smoke-" + uuid.uuid4().hex
MIGRATION_PASSWORD = "migration-smoke-" + uuid.uuid4().hex
ENV = os.environ.copy()
ENV["SAPPHIRE_SMOKE_DB_PASSWORD"] = PASSWORD
ENV["SAPPHIRE_SMOKE_RUNTIME_PASSWORD"] = RUNTIME_PASSWORD
ENV["SAPPHIRE_SMOKE_MIGRATION_PASSWORD"] = MIGRATION_PASSWORD
ENV["ASPNETCORE_ENVIRONMENT"] = "Development"
ENV["Jwt__SecretKey"] = "DisposableSmokeJwtKeyThatIsAtLeast32CharactersLong"
ENV["Messaging__SharedSecret"] = "DisposableSmokeTransportKeyAtLeast32CharactersLong"


def run(*args, env=ENV):
    result = subprocess.run(args, cwd=ROOT, env=env, text=True, encoding="utf-8",
                            errors="replace", capture_output=True)
    if result.returncode:
        output = result.stdout + result.stderr
        for secret in (PASSWORD, RUNTIME_PASSWORD, MIGRATION_PASSWORD):
            output = output.replace(secret, "[redacted]")
        raise RuntimeError(
            f"{' '.join(args[:3])} failed (exit {result.returncode}): "
            + output[-7000:]
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
            f"Username=sapphire_{name}_runtime;Password={RUNTIME_PASSWORD}"
        )
        migration_env = ENV.copy()
        migration_env["ConnectionStrings__DefaultConnection"] = (
            f"Host=127.0.0.1;Port={port};Database=sapphire_{name}_smoke;"
            f"Username=sapphire_{name}_migrator;Password={MIGRATION_PASSWORD}"
        )
        project = f"src/services/{service}/Sapphire.{service}.Api/Sapphire.{service}.Api.csproj"
        run("dotnet", "run", "--project", project, "-c", "Release", "--", "--migrate", env=migration_env)
        print(f"{service}: --migrate passed on empty Compose PostgreSQL")
        denied_ddl = subprocess.run(
            ["docker", "compose", "-f", str(COMPOSE), "-p", PROJECT, "exec", "-T",
             "-e", f"PGPASSWORD={RUNTIME_PASSWORD}", f"{name}-db", "psql", "-U",
             f"sapphire_{name}_runtime", "-d", f"sapphire_{name}_smoke", "-c",
             "CREATE TABLE public.runtime_must_not_create (id int)"],
            cwd=ROOT, env=ENV, capture_output=True,
        )
        if denied_ddl.returncode == 0:
            raise RuntimeError(f"{service} runtime role unexpectedly has DDL permission")
        print(f"{service}: runtime DDL denied")

    bootstrap_env = ENV.copy()
    auth_port = compose("port", "auth-db", "5432").rsplit(":", 1)[-1]
    bootstrap_env["ConnectionStrings__DefaultConnection"] = (
        f"Host=127.0.0.1;Port={auth_port};Database=sapphire_auth_smoke;"
        f"Username=sapphire_auth_migrator;Password={MIGRATION_PASSWORD}"
    )
    bootstrap_env["Bootstrap__AdminUsername"] = "firstadmin"
    bootstrap_env["Bootstrap__AdminEmail"] = "firstadmin@example.test"
    bootstrap_env["Bootstrap__AdminPassword"] = "ChangeThisOnFirstLogin9!"
    auth_project = "src/services/Auth/Sapphire.Auth.Api/Sapphire.Auth.Api.csproj"
    run("dotnet", "run", "--no-build", "--project", auth_project, "-c", "Release",
        "--", "--bootstrap-admin", env=bootstrap_env)
    repeated = subprocess.run(
        ["dotnet", "run", "--no-build", "--project", auth_project, "-c", "Release",
         "--", "--bootstrap-admin"], cwd=ROOT, env=bootstrap_env,
        text=True, encoding="utf-8", errors="replace", capture_output=True,
    )
    if repeated.returncode == 0:
        raise RuntimeError("Second administrator bootstrap was accepted")
    print("Admin bootstrap: first accepted, repeat rejected")

    smoke_project = "tests/Sapphire.Postgres.Smoke/Sapphire.Postgres.Smoke.csproj"
    print(run("dotnet", "run", "--project", smoke_project, "-c", "Release"))

    writer = subprocess.Popen(
        ["dotnet", "run", "--no-build", "--project", smoke_project, "-c", "Release", "--", "--crash-write"],
        cwd=ROOT, env=ENV, text=True, encoding="utf-8", errors="replace",
        stdout=subprocess.PIPE, stderr=subprocess.PIPE,
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

    # Restore a real custom-format backup into a separate PostgreSQL container.
    compose("up", "-d", "--wait", "restore-db")
    compose("up", "-d", "billing-backup")
    base = ["docker", "compose", "-f", str(COMPOSE), "-p", PROJECT, "exec", "-T"]
    start_restore = time.perf_counter()
    for _ in range(30):
        listing = subprocess.run(base + ["billing-backup", "sh", "-c",
                                         "ls -1 /backups/*.dump"], cwd=ROOT, env=ENV,
                                 capture_output=True, text=True)
        if listing.returncode == 0:
            backup_path = listing.stdout.strip().splitlines()[-1]
            break
        time.sleep(1)
    else:
        raise RuntimeError("Automated billing backup did not create a complete dump")
    backup = subprocess.run(base + ["billing-backup", "cat", backup_path], cwd=ROOT,
                            env=ENV, capture_output=True, check=True).stdout
    restored = subprocess.run(base + ["restore-db", "pg_restore", "-U", "postgres",
                                      "-d", "restored_billing_smoke", "--no-owner", "--no-acl"],
                              cwd=ROOT, env=ENV, input=backup, capture_output=True)
    if restored.returncode:
        raise RuntimeError("pg_restore failed: " + restored.stderr.decode("utf-8", "replace")[-2000:])
    sql = "select (select count(*) from wallets), (select count(*) from tariff_entitlements), " \
          "(select count(*) from promocodes), " \
          "(select count(*) from information_schema.columns where table_schema='public');"
    original_counts = compose("exec", "-T", "billing-db", "psql", "-U", "postgres",
                              "-d", "sapphire_billing_smoke", "-At", "-c", sql)
    restored_counts = compose("exec", "-T", "restore-db", "psql", "-U", "postgres",
                              "-d", "restored_billing_smoke", "-At", "-c", sql)
    if original_counts != restored_counts:
        raise RuntimeError(f"Backup restore mismatch: {original_counts} != {restored_counts}")
    print(f"Automated Billing backup restored on separate instance in "
          f"{time.perf_counter() - start_restore:.1f}s; schema and key row counts match.")
finally:
    subprocess.run(
        ["docker", "compose", "-f", str(COMPOSE), "-p", PROJECT,
         "down", "--volumes", "--remove-orphans"],
        cwd=ROOT, env=ENV, capture_output=True, text=True,
    )
