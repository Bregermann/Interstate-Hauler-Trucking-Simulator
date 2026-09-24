# Truck Taxi Chatterbox / WorkHere Adapter

## Existing Installation, Not A New TTS Stack

- WorkHere: `F:/Codexprojects/MyVoiceForRecording/WorkHere`
- Chatterbox source: `F:/Codexprojects/MyVoiceForRecording/chatterbox`
- Interpreter: `WorkHere/.venv/Scripts/python.exe` (uses the supplied Python 3.11.15 installation).
- Inspected launchers: `run.bat`, `generate_all.bat`, `launch_gui.bat`, `setup.bat` and `tools/run.ps1` / `environment.ps1`.
- Existing launchers delegate to `narrate.py`, whose batch interface is lecture-oriented.
- The GUI on port 7865 is not required for Truck Taxi jobs.
- No clone, pip installation, second model cache, or Unity runtime Python dependency.

The project adapter imports the actual installed `app.engine.Engine.generate`,
`app.config.parameters`, `app.audio.validate_reference`, `app.audio.atomic_wav`,
`app.storage.atomic_json`, `app.storage.operation_lock` and `source_identity`. It does not create fake lecture
IDs or alter lecture scripts, output masters, settings, or source recordings.
WorkHere's normal cache, log and lock behavior remains intact. If the GUI is busy,
the queue reports that condition; it does not compete for the GPU.

Verified source commit: `5de7a54aa4e5e2baadb0182dde554908b48b85c2`.
Verified Turbo weights: `749d1c1a46eb10492095d68fbcf55691ccf137cd`.

## Unity Workflow

1. Open **Truck Taxi > Passenger Factory**.
2. Select a passenger. **Create Missing Voice / Dialogue Assets** preserves existing
   authored assets; the batch equivalent adds missing assets to the original roster.
3. Expand **Voice**. Assign absolute WAV paths for Neutral and any supplied emotions.
   **Choose Neutral Reference WAV (external)** picks a file without copying it.
4. Expand **Dialogue**. Edit stable line ID, text, subtitle, category and emotion.
   A line can override the passenger voice profile and reference path.
5. Choose **Generate Selected Line**, **Generate Selected Passenger**, or a missing-audio
   action. Completed WAVs are synchronously imported as AudioClips and assigned to the
   corresponding `TruckTaxiDialogueLine.generatedAudio`.
6. **Regenerate Selected Line** explicitly confirms replacement of generated output.
   Ordinary generation reuses verified cached WAVs.
7. **Cancel Queue** finishes the current line and stops between lines. **Resume Queue**
   skips completed hash-verified audio. If text/reference changed since a queued job,
   generate a fresh job instead; stale results are not assigned.

WorkHere path and offline mode are local settings in `UserSettings/TruckTaxiChatterbox.asset`.
Offline is enabled by default: only already-cached model weights are used.
Supported model names/presets remain the existing WorkHere names, not a separate preset system.
Emotion selects the exact reference, then an authored related emotion, then Neutral.
A specified but invalid/missing file is an actionable error, not silent replacement.
Intensity is retained as authoring/cache metadata; Turbo has no invented intensity API.
Its actual performance is controlled by the selected recording and WorkHere preset.

## Data And Output

- Existing `PassengerProfile` is extended, not replaced: stable ID, voice and authored dialogue references.
- Original passenger string dialogue and reaction fields are preserved.
- Voice assets: `Assets/LWS/TruckTaxi/Passengers/VoiceProfiles/`.
- Dialogue assets: `Assets/LWS/TruckTaxi/Passengers/Dialogue/`.
- Generated WAVs: `Assets/LWS/TruckTaxi/Passengers/GeneratedAudio/<passengerId>/<lineId>_<hash>.wav`.
- WAV provenance manifests sit beside the WAVs. They are not Resources or runtime dependencies.
- Project queues: `Tools/TruckTaxiPassengerFactory/Jobs/` (ignored local generated data).
- Logs: `Tools/TruckTaxiPassengerFactory/Logs/` (ignored).
- Missing-reference report: `TruckTaxi_MissingVoiceSamples.md` beside this document.

The cache includes text, voice identity, emotion/intensity, reference content hash,
actual generation parameters, seed, Chatterbox source and pinned weights. Cached
WAV content is also checked. Corrupt output/metadata regenerates that line; other
line failures do not stop the batch. Final audio is mono PCM WAV, end-silence trimmed
with 80ms padding and peak-normalized to 0.89 (about -1 dBFS). Interior pauses remain.
Unity imports speech as mono Vorbis, quality 0.75, Decompress On Load.

Reference paths and generation fields are Editor-only fields. No reference audio
is copied under Assets. Runtime assets retain the generated clips and subtitles.
No voice is automatically inferred from casting, personality or race.

## Verification

2026-09-23 / 2026-09-24:
- Python adapter unit suite: 11/11 passed (cache, cancellation/resume, reference changes,
  corrupt manifests/WAVs, per-line failure continuation, busy WorkHere lock, safe paths, trimming).
- Unity Truck Taxi EditMode suite: 36/36 passed, including the opt-in real external queue test,
  factory-window construction and original-roster idempotency checks.
- Ten original passengers now reference ten voice profiles and ten dialogue sets.
  Each original profile changed only by adding its stable ID and those two references.
  Character references remain deliberately unassigned until explicitly cast.
- Fresh Turbo/CUDA inference produced the `pipeline-validation/greeting` WAV, using
  `voice_refs/neutralvoicesample.wav`, with networking disabled.
- Unity launched the same adapter, reused the valid WAV, imported it, assigned it to
  a temporary passenger dialogue asset and confirmed the next missing-audio job was empty.
- Changing the line text made it eligible again. Source recording SHA remained unchanged.
- Test fixture assets were removed after the test; the generated validation WAV remains.
- Final adapter rerun returned `cached` through the real WorkHere atomic JSON/WAV helpers.
- Truck Taxi regression PlayMode suite: 3/3 passed in 40.40 seconds, covering the
  existing teleport-assisted three-ride flow, presentation and actual NWH low-speed turn.
  This does not test generated passenger voice playback in a ride.
- No claim of passenger voice quality, live ride playback, factory-window visual validation,
  or completion of the remaining model/animation/ejection factory work is made by these tests.

Reports: `Builds/TruckTaxiDemo/Validation/FactoryVoiceEditMode.xml`,
`FactoryVoiceRegressionPlayMode.xml` and adapter test output.
To rerun the opt-in integration test, set `TRUCK_TAXI_RUN_VOICE_INTEGRATION=1` for Unity's
EditMode test process; otherwise it is skipped on machines without this installation.

## Vendor / Ownership Check

- Audited and used: existing Chatterbox/WorkHere engine, validation, preset and WAV APIs.
- Unity AssetDatabase/AudioImporter and UI Toolkit provide editor import/presentation.
- Relevant assets not used for offline inference: Pixel Crushers handles runtime narrative,
  not Python voice generation; Heat remains the runtime HUD foundation, not an editor framework.
- Custom systems: thin Truck Taxi job transport, dialogue/voice semantics, editor controls and tests.
- Vendor source modified: NO.
- Duplicate inference/model-loading functionality created: NO.

This is the voice-generation/import portion of Passenger Factory. It does not mark
the larger passenger-model/animation/runtime mechanics milestone complete.
