# Existing WorkHere Adapter

No Chatterbox clone, package installation, or model download is required here.
Use `WorkHere/.venv/Scripts/python.exe`, not its unconfigured base interpreter.

Audited installation: `F:/Codexprojects/MyVoiceForRecording/WorkHere`.
Its launchers call `tools/run.ps1 -> .venv/Scripts/python.exe -B narrate.py`.
The CLI accepts lecture IDs and restricts references to `voice_refs`' top level.
Taxi dialogue instead imports its existing `app.engine.Engine.generate`,
`app.config.parameters`, `app.audio.validate_reference/atomic_wav`, source identity,
and its atomic JSON writer/operation lock. This accepts explicit per-character reference paths without
changing the working lecture application's files, settings, scripts, or masters.

The queue lives inside the Unity project. Each line resolves its reference,
hashes text/voice/emotion/reference content/model/preset/seed/source and pinned
weights, generates only changed/missing audio, trims excessive end silence,
normalizes to -1 dBFS peak, and writes a deterministic WAV plus provenance.
Source references are read-only. No voice is inferred from race or personality.

`--plan` validates references and computes the cache without inference.
A sibling `.cancel` file stops between lines. Remove it and rerun to resume;
completed valid WAVs are reused. Explicit regeneration replaces only that
generated line's output, never references or lecture audio. Invalid lines are
reported individually and do not stop the rest of the queue.

Set `HF_HUB_OFFLINE=1` to guarantee cached models only. The installed engine keeps
its existing cache/log policy under WorkHere. Runtime player builds do not ship
this Python adapter or its dependencies.

Unity entry point: **Truck Taxi > Passenger Factory**. Configure external WAV
references on the selected voice profile, then generate a line/passenger or all
missing audio. The editor queue imports/assigns finished clips and rejects stale
results if the text/reference was edited while inference was running.
See `Assets/LWS/TruckTaxi/Documentation/TruckTaxi_Chatterbox_WorkHere_Adapter.md`.
