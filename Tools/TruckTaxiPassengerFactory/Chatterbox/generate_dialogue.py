"""Truck Taxi job adapter for the existing WorkHere engine; no Chatterbox install."""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
import re
import sys

sys.dont_write_bytecode = True
ADAPTER_VERSION = 1


def digest(value):
    return hashlib.sha256(json.dumps(value, sort_keys=True, ensure_ascii=False).encode("utf-8")).hexdigest()


def safe_id(value):
    if not isinstance(value, str) or not re.fullmatch(r"[A-Za-z0-9][A-Za-z0-9_.-]{0,99}", value) or value.endswith("."):
        raise ValueError("Passenger/line IDs must be nonempty ASCII identifiers, not paths.")
    if value.split(".")[0].upper() in {"CON", "PRN", "AUX", "NUL", *(f"COM{i}" for i in range(1, 10)), *(f"LPT{i}" for i in range(1, 10))}:
        raise ValueError("Passenger/line ID is reserved by Windows.")
    return value


def normalize_trim(audio, sample_rate):
    import numpy as np
    samples = np.asarray(audio, dtype=np.float32).squeeze()
    if samples.ndim != 1 or not samples.size or not np.isfinite(samples).all():
        raise ValueError("Invalid generated waveform")
    active = np.flatnonzero(np.abs(samples) > .0032)
    if not len(active):
        raise ValueError("Generated waveform is silent")
    # Retain 80ms breathing room; trim ends only, never gaps inside dialogue.
    padding = round(sample_rate * .08)
    samples = samples[max(0, int(active[0]) - padding):min(len(samples), int(active[-1]) + padding + 1)]
    samples = samples * (0.89 / float(np.max(np.abs(samples))))
    return samples


def run(job_path, workhere, project, plan_only=False):
    job_path, workhere, project = job_path.resolve(), workhere.resolve(), project.resolve()
    if not job_path.is_relative_to(project):
        raise ValueError("Queue must be inside the Unity project")
    job = json.loads(job_path.read_text(encoding="utf-8-sig"))
    output = (project / "Assets/LWS/TruckTaxi/Passengers/GeneratedAudio").resolve()
    if not output.is_relative_to(project):
        raise ValueError("Generated audio must remain inside the Unity project")
    state_path = job_path.with_suffix(".result.json")
    cancel = job_path.with_suffix(".cancel")
    if not (workhere / "app/engine.py").is_file():
        raise ValueError("Working WorkHere installation not found; nothing will be installed")
    sys.path.insert(0, str(workhere))
    # Reuse WorkHere's source discovery, caches, pinned models, and engine unchanged.
    from app.engine import Engine, source_identity
    from app.config import parameters
    from app.audio import validate_reference, atomic_wav
    from app.storage import sha256, operation_lock, atomic_json
    source = source_identity()
    model_lock = workhere / "config/model-lock.json"
    source["weights_lock_sha256"] = sha256(model_lock) if model_lock.exists() else None
    engine = Engine()
    result = {"schema": 1, "adapter_version": ADAPTER_VERSION, "status": "running", "items": []}
    output.mkdir(parents=True, exist_ok=True)
    try:
        # Share the existing operation lock: do not compete with the narration GUI's GPU job.
        with operation_lock():
            for line in job.get("items", []):
                if cancel.exists():
                    result["status"] = "cancelled"
                    break
                item = {"passengerId": line.get("passengerId"), "lineId": line.get("lineId"),
                        "requestHash": line.get("requestHash", ""), "status": "failed"}
                try:
                    passenger, identifier = safe_id(line["passengerId"]), safe_id(line["lineId"])
                    text = line["text"].strip()
                    if not text:
                        raise ValueError("Dialogue text is empty")
                    model, preset = line.get("model", "turbo"), line.get("preset", "Lecture Neutral")
                    reference = validate_reference(Path(line["referencePath"]).resolve(), model)
                    options = {"model": model, "preset": preset, "parameters": parameters(model, preset),
                               "voice_reference": reference, "device": line.get("device", "auto")}
                    seed = int(line.get("seed", 42))
                    fingerprint = digest({"text": text, "voice": line.get("voiceProfileId"),
                        "emotion": line.get("emotion"), "intensity": line.get("intensity", 1),
                        "options": options, "seed": seed, "source": source, "adapter": ADAPTER_VERSION})
                    target = output / passenger / f"{identifier}_{fingerprint[:24]}.wav"
                    manifest = target.with_suffix(".json")
                    try:
                        cached = json.loads(manifest.read_text(encoding="utf-8")) if manifest.exists() else {}
                    except (ValueError, OSError):
                        cached = {}
                    valid = target.exists() and cached.get("hash") == fingerprint and cached.get("wavSha256") == sha256(target)
                    item.update({"hash": fingerprint, "outputPath": str(target), "referencePath": reference["path"]})
                    if valid and not line.get("regenerate", False):
                        item["status"] = "cached"
                    elif plan_only:
                        item["status"] = "planned"
                    else:
                        wav, rate, provenance = engine.generate(text, options, seed, progress=lambda message: print(message, flush=True))
                        wav = normalize_trim(wav, rate)
                        atomic_wav(target, wav, rate, replace=target.exists())
                        atomic_json(manifest, {"hash": fingerprint, "wavSha256": sha256(target),
                            "text": text, "voiceProfileId": line.get("voiceProfileId"), "reference": reference,
                            "emotion": line.get("emotion"), "options": options, "provenance": provenance,
                            "adapterVersion": ADAPTER_VERSION})
                        item["status"] = "complete"
                except Exception as exc:
                    item["error"] = f"{type(exc).__name__}: {exc}"
                result["items"].append(item)
                atomic_json(state_path, result)
                print(json.dumps(item, ensure_ascii=False), flush=True)
            else:
                result["status"] = "complete" if all(i["status"] != "failed" for i in result["items"]) else "needs_attention"
    except Exception as exc:
        result["status"] = "needs_attention"
        result["error"] = f"{type(exc).__name__}: {exc}"
    finally:
        try:
            if engine.model is not None:
                engine.unload()
        finally:
            atomic_json(state_path, result)
    return 0 if result["status"] in {"complete", "cancelled"} else 1


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--job", type=Path, required=True)
    parser.add_argument("--workhere", type=Path, required=True)
    parser.add_argument("--project", type=Path, required=True)
    parser.add_argument("--plan", action="store_true")
    args = parser.parse_args()
    return run(args.job, args.workhere, args.project, args.plan)


if __name__ == "__main__":
    raise SystemExit(main())
