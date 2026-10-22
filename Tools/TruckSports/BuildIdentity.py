"""Content identity includes scene/material bundles, not only managed code."""
import hashlib
import json
from pathlib import Path


def snapshot(folder):
    folder = Path(folder).resolve()
    if not (folder / "TruckSports.exe").is_file():
        raise ValueError("Expected the complete Truck Sports build directory")
    files = []
    for path in sorted(folder.rglob("*")):
        if path.is_file():
            resolved = path.resolve()
            if not resolved.is_relative_to(folder):
                raise ValueError("Build dependency escapes its directory: " + str(path))
            with path.open("rb") as stream:
                digest = hashlib.file_digest(stream, "sha256").hexdigest()
            files.append(dict(path=path.relative_to(folder).as_posix(), bytes=path.stat().st_size, sha256=digest))
    encoded = json.dumps(files, sort_keys=True, separators=(",", ":")).encode("utf-8")
    return hashlib.sha256(encoded).hexdigest(), files
