#!/usr/bin/env python3
"""Normalize one completed FluxIA SUMO replay for POST /api/v1/run-imports."""

from __future__ import annotations

import argparse
import hashlib
import json
import math
import mimetypes
import os
import re
import sys
import urllib.error
import urllib.request
from pathlib import Path
from typing import Any


PROJECT_ROOT = Path(__file__).resolve().parents[1]
OUTPUTS_ROOT = (PROJECT_ROOT / "docs" / "execucao-luna" / "outputs").resolve()
SCENARIO_ID = "fluxia-stage6-c2-8-signals"
RUN_ID_PATTERN = re.compile(r"^rl_(\d{8}T\d{6}Z)_[A-Za-z0-9]+$")
SHA256_PATTERN = re.compile(r"^[a-fA-F0-9]{64}$")
ARTIFACTS = (
    "execution_header.json",
    "episode_summary.json",
    "panel_session_summary.json",
    "panel_telemetry.jsonl",
    "phase_trace.jsonl",
    "collisions.xml",
    "sumo_summary.xml",
    "tripinfo.xml",
    "rewards.jsonl",
    "actions.jsonl",
)


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def resolve_within(path_value: str, allowed_root: Path, label: str) -> Path:
    candidate = Path(path_value).expanduser()
    if not candidate.is_absolute():
        candidate = PROJECT_ROOT / candidate
    resolved = candidate.resolve(strict=True)
    try:
        resolved.relative_to(allowed_root.resolve())
    except ValueError as error:
        raise ValueError(f"{label} aponta para fora do diretório permitido.") from error
    if not resolved.is_file():
        raise ValueError(f"{label} não é um arquivo.")
    return resolved


def verify_source_file(
    item: dict[str, Any],
    *,
    expected_hash: str,
    allowed_root: Path,
    label: str,
) -> Path:
    path = resolve_within(str(item.get("path", "")), allowed_root, label)
    declared_hash = str(item.get("sha256", ""))
    if not SHA256_PATTERN.fullmatch(declared_hash) or declared_hash.lower() != expected_hash.lower():
        raise ValueError(f"{label}: o hash declarado no cabeçalho não corresponde ao manifesto.")
    if sha256(path) != declared_hash.lower():
        raise ValueError(f"{label}: o arquivo atual não corresponde ao SHA-256 registrado.")
    return path


def read_json(path: Path) -> dict[str, Any]:
    with path.open("r", encoding="utf-8") as stream:
        value = json.load(stream)
    if not isinstance(value, dict):
        raise ValueError(f"{path.name} deve conter um objeto JSON.")
    return value


def finite_number(value: Any, label: str) -> float:
    if isinstance(value, bool) or not isinstance(value, (int, float)) or not math.isfinite(value):
        raise ValueError(f"{label} precisa ser um número finito registrado na origem.")
    return float(value)


def metric(name: str, value: int | float, unit: str, source_key: str, *, signal_id: str | None = None, time_s: float | None = None) -> dict[str, Any]:
    scope: dict[str, Any] = {"type": "signalController" if signal_id else "network"}
    if signal_id:
        scope["signalControllerId"] = signal_id
    point: dict[str, Any] = {
        "metricName": name,
        "value": value,
        "unit": unit,
        "scope": scope,
        "sourceKey": source_key,
    }
    if time_s is not None:
        point["simulationTimeSeconds"] = time_s
    return point


def build_payload(run_dir_value: str, scenario_id: str) -> tuple[dict[str, Any], str]:
    run_dir_candidate = Path(run_dir_value).expanduser()
    if not run_dir_candidate.is_absolute():
        run_dir_candidate = PROJECT_ROOT / run_dir_candidate
    run_dir = run_dir_candidate.resolve(strict=True)
    try:
        run_dir.relative_to(OUTPUTS_ROOT)
    except ValueError as error:
        raise ValueError("O diretório da execução precisa estar dentro de docs/execucao-luna/outputs.") from error
    if not run_dir.is_dir():
        raise ValueError("--run-dir deve apontar para a pasta de uma execução concluída.")

    run_id = run_dir.name
    match = RUN_ID_PATTERN.fullmatch(run_id)
    if match is None:
        raise ValueError("O nome da pasta precisa conter o run_id estável rl_YYYYMMDDTHHMMSSZ_xxxxxx.")
    started_at = f"{match.group(1)[:4]}-{match.group(1)[4:6]}-{match.group(1)[6:8]}T{match.group(1)[9:11]}:{match.group(1)[11:13]}:{match.group(1)[13:15]}Z"

    header = read_json(run_dir / "execution_header.json")
    episode = read_json(run_dir / "episode_summary.json")
    panel = read_json(run_dir / "panel_session_summary.json")
    if header.get("run_id") != run_id or episode.get("run_id") != run_id:
        raise ValueError("run_id divergente entre a pasta, execution_header e episode_summary.")
    if panel.get("status") != "Concluída no horizonte":
        raise ValueError("A execução não consta como concluída no horizonte.")
    if scenario_id != SCENARIO_ID:
        raise ValueError(f"Esta versão do adaptador só reconhece o cenário {SCENARIO_ID}.")

    network = verify_source_file(
        header["network"], expected_hash=header["network"]["sha256"],
        allowed_root=PROJECT_ROOT / "sumo" / "scenarios" / "fluxia_stage6_c2", label="Rede SUMO"
    )
    catalog = verify_source_file(
        header["catalog"], expected_hash=header["catalog"]["sha256"],
        allowed_root=PROJECT_ROOT / "sumo" / "scenarios" / "fluxia_stage6_c2", label="Catálogo de semáforos"
    )
    routes = verify_source_file(
        header["routes"], expected_hash=header["routes"]["sha256"],
        allowed_root=PROJECT_ROOT / "docs" / "execucao-luna" / "inputs", label="Demanda de rotas"
    )
    timing = verify_source_file(
        header["timing_profile"], expected_hash=header["timing_profile"]["sha256"],
        allowed_root=PROJECT_ROOT / "sumo" / "scenarios" / "fluxia_stage6_c2", label="Perfil de tempos"
    )
    del network, catalog, routes, timing

    expected_signal_ids = set(header.get("signals", []))
    catalog_data = read_json(Path(header["catalog"]["path"]))
    catalog_signal_ids = {str(signal["controller_id"]) for signal in catalog_data.get("signals", [])}
    if not expected_signal_ids or expected_signal_ids != catalog_signal_ids:
        raise ValueError("Os controladores do cabeçalho não correspondem ao catálogo da rede.")

    horizon = finite_number(panel.get("horizon_s"), "panel_session_summary.horizon_s")
    if finite_number(episode.get("simulation_time_s"), "episode_summary.simulation_time_s") != horizon:
        raise ValueError("O horizonte simulado diverge entre os resumos.")
    if finite_number(header.get("end_time_s"), "execution_header.end_time_s") != horizon:
        raise ValueError("O horizonte do cabeçalho diverge dos resultados.")
    finished_at = str(panel.get("created_at", ""))
    if not finished_at.endswith("Z"):
        raise ValueError("panel_session_summary.created_at precisa registrar um instante UTC.")

    summary_fields = {
        "departed": episode.get("departed"),
        "arrived": panel.get("arrived"),
        "active_at_end": panel.get("active_at_end"),
        "due_pending_at_end": panel.get("due_pending_at_end"),
        "collision_vehicle_steps": episode.get("collision_vehicle_steps"),
        "teleported_vehicles": len(episode.get("teleport_ids", [])),
        "collision_events_confirmed": panel.get("collision_events"),
    }
    for key, value in summary_fields.items():
        if isinstance(value, bool) or not isinstance(value, int) or value < 0:
            raise ValueError(f"A origem não registrou uma contagem válida para {key}.")
    for key in ("arrived", "active_at_end", "due_pending_at_end"):
        if summary_fields[key] != panel.get(key):
            raise ValueError(f"A contagem {key} diverge do painel.")
    if summary_fields["arrived"] != episode.get("arrived"):
        raise ValueError("A contagem de chegadas diverge entre o painel e episode_summary.")
    if summary_fields["teleported_vehicles"] != panel.get("teleport_starts"):
        raise ValueError("A contagem de teletransportes diverge entre os resumos.")

    metrics = [
        metric("vehicles.departed", summary_fields["departed"], "vehicle", "summary:vehicles.departed"),
        metric("vehicles.arrived", summary_fields["arrived"], "vehicle", "summary:vehicles.arrived"),
        metric("vehicles.active_at_end", summary_fields["active_at_end"], "vehicle", "summary:vehicles.active_at_end"),
        metric("vehicles.due_pending_at_end", summary_fields["due_pending_at_end"], "vehicle", "summary:vehicles.due_pending_at_end"),
        metric("collision.events_confirmed", summary_fields["collision_events_confirmed"], "event", "summary:collision.events_confirmed"),
        metric("collision.vehicle_steps", summary_fields["collision_vehicle_steps"], "vehicle_step", "summary:collision.vehicle_steps"),
        metric("teleport.vehicles", summary_fields["teleported_vehicles"], "vehicle", "summary:teleport.vehicles"),
    ]
    observed = 0
    telemetry_path = run_dir / "panel_telemetry.jsonl"
    with telemetry_path.open("r", encoding="utf-8") as stream:
        for line_number, line in enumerate(stream, 1):
            if not line.strip():
                continue
            try:
                sample = json.loads(line)
            except json.JSONDecodeError as error:
                raise ValueError(f"panel_telemetry.jsonl: JSON inválido na linha {line_number}.") from error
            simulation_time = finite_number(sample.get("time_s"), f"panel_telemetry.jsonl[{line_number}].time_s")
            if not 0 <= simulation_time <= horizon:
                raise ValueError(f"panel_telemetry.jsonl: tempo fora do horizonte na linha {line_number}.")
            rows = sample.get("rows")
            if not isinstance(rows, list):
                raise ValueError(f"panel_telemetry.jsonl: rows inválido na linha {line_number}.")
            seen_ids: set[str] = set()
            for row in rows:
                signal_id = str(row.get("signal_id", ""))
                if signal_id not in expected_signal_ids or signal_id in seen_ids:
                    raise ValueError(f"panel_telemetry.jsonl: controlador desconhecido ou repetido na linha {line_number}.")
                seen_ids.add(signal_id)
                queue = row.get("queue")
                if queue is None:
                    continue
                if isinstance(queue, bool) or not isinstance(queue, int) or queue < 0:
                    raise ValueError(f"panel_telemetry.jsonl: fila inválida na linha {line_number}.")
                metrics.append(metric(
                    "queue.vehicles", queue, "vehicle",
                    f"queue:{signal_id}:{simulation_time:.6f}",
                    signal_id=signal_id, time_s=simulation_time,
                ))
                observed += 1

    decision_period = finite_number(header.get("decision_period_s"), "execution_header.decision_period_s")
    if decision_period <= 0:
        raise ValueError("decision_period_s precisa ser maior que zero.")
    expected_timesteps = math.ceil(horizon / decision_period)
    expected_samples = expected_timesteps * len(expected_signal_ids)
    coverage = {
        "expectedSamples": expected_samples,
        "observedSamples": observed,
        "complete": observed == expected_samples,
        "note": f"Fila por controlador em passos de {decision_period:g} s; valores nulos foram omitidos.",
    }

    artifacts: list[dict[str, Any]] = []
    for name in ARTIFACTS:
        artifact_path = (run_dir / name).resolve(strict=True)
        artifact_path.relative_to(run_dir)
        if not artifact_path.is_file():
            raise ValueError(f"Artefato obrigatório ausente: {name}.")
        content_type = mimetypes.guess_type(name)[0] or "application/octet-stream"
        artifacts.append({
            "logicalName": name,
            "sha256": sha256(artifact_path),
            "sizeBytes": artifact_path.stat().st_size,
            "contentType": content_type,
        })

    checkpoint_path_value = str(panel.get("checkpoint", ""))
    checkpoint_root = PROJECT_ROOT / "docs" / "execucao-luna" / "outputs"
    checkpoint_path = resolve_within(checkpoint_path_value, checkpoint_root, "Checkpoint do modelo")
    checkpoint_hash = sha256(checkpoint_path)
    if checkpoint_hash != str(panel.get("checkpoint_sha256", "")).lower():
        raise ValueError("O checkpoint do painel não corresponde ao hash registrado.")
    artifacts.append({
        "logicalName": "checkpoint/final_checkpoint.zip",
        "sha256": checkpoint_hash,
        "sizeBytes": checkpoint_path.stat().st_size,
        "contentType": "application/zip",
    })

    metrics.sort(key=lambda item: (item["metricName"], item["scope"].get("signalControllerId", ""), item.get("simulationTimeSeconds", -1), item["sourceKey"]))
    payload = {
        "sourceRunId": run_id,
        "scenarioId": scenario_id,
        "runType": "replay",
        "environment": "replay",
        "seed": int(panel["seed"]),
        "startedAtUtc": started_at,
        "finishedAtUtc": finished_at,
        "horizonSeconds": horizon,
        "scenarioFingerprint": {
            "networkSha256": header["network"]["sha256"].lower(),
            "catalogSha256": header["catalog"]["sha256"].lower(),
            "routeDemandSha256": header["routes"]["sha256"].lower(),
            "timingProfileSha256": header["timing_profile"]["sha256"].lower(),
        },
        "model": {
            "algorithm": "PPO policy replay",
            "artifactId": "checkpoint/final_checkpoint.zip",
            "sha256": checkpoint_hash,
        },
        "effectiveParameters": {
            "schemaVersion": 2,
            "controlProfileId": "fluxia_stage6_c2",
            "profileSha256": header["timing_profile"]["sha256"].lower(),
        },
        "trafficSummary": {
            "departed": summary_fields["departed"],
            "arrived": summary_fields["arrived"],
            "activeAtEnd": summary_fields["active_at_end"],
            "duePendingAtEnd": summary_fields["due_pending_at_end"],
            "collisionEventsConfirmed": summary_fields["collision_events_confirmed"],
            "collisionVehicleSteps": summary_fields["collision_vehicle_steps"],
            "teleportedVehicles": summary_fields["teleported_vehicles"],
        },
        "metrics": metrics,
        "coverage": coverage,
        "artifacts": artifacts,
        "limitations": [
            "Replay de uma seed (20261301), correspondente a uma linha da validação pareada; não é amostra independente.",
            "A simulação foi encerrada no horizonte de 600 s com veículos ainda ativos e pendências.",
            "A telemetria por controlador contém fila; duração e espera médias de viagens concluídas não foram inferidas.",
        ],
    }
    key_digest = hashlib.sha256(f"{scenario_id}:{run_id}".encode("utf-8")).hexdigest()
    idempotency_key = f"fluxia-import-{key_digest}"
    return payload, idempotency_key


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--run-dir", required=True, help="Pasta de uma execução em docs/execucao-luna/outputs.")
    parser.add_argument("--scenario-id", default=SCENARIO_ID)
    parser.add_argument("--output", help="Salva o JSON normalizado neste arquivo; por padrão imprime no stdout.")
    parser.add_argument("--post", action="store_true", help="Envia o payload ao POST /api/v1/run-imports.")
    parser.add_argument("--api-url", default="http://localhost:5000", help="Endereço base da API, sem /api/v1.")
    args = parser.parse_args()
    worker_token = (os.environ.get("FLUXAI_WORKER_TOKEN") or os.environ.get("FLUXIA_WORKER_TOKEN", "")).strip()
    if args.post and not worker_token:
        print("Defina FLUXAI_WORKER_TOKEN com o token configurado para o worker.", file=sys.stderr)
        return 2

    try:
        payload, key = build_payload(args.run_dir, args.scenario_id)
        encoded = json.dumps(payload, ensure_ascii=False, indent=2).encode("utf-8")
        if args.output:
            output_path = Path(args.output).expanduser().resolve()
            output_path.write_bytes(encoded + b"\n")
            print(f"Payload normalizado salvo: {output_path}")
        elif not args.post:
            sys.stdout.buffer.write(encoded + b"\n")

        if args.post:
            url = args.api_url.rstrip("/") + "/api/v1/run-imports"
            request = urllib.request.Request(url, data=encoded, method="POST", headers={
                "Content-Type": "application/json",
                "Idempotency-Key": key,
                "Authorization": f"Bearer {worker_token}",
            })
            try:
                with urllib.request.urlopen(request, timeout=60) as response:
                    body = response.read().decode("utf-8")
                    print(f"HTTP {response.status}")
                    print(body)
            except urllib.error.HTTPError as error:
                print(f"HTTP {error.code}", file=sys.stderr)
                print(error.read().decode("utf-8"), file=sys.stderr)
                return 1
    except (OSError, KeyError, TypeError, ValueError, json.JSONDecodeError) as error:
        print(f"Erro ao normalizar execução: {error}", file=sys.stderr)
        return 2
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
