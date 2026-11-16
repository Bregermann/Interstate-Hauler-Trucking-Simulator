# Owned wing audio audit and explicit integration

Scope: Alpha0.8 §29.4, source-only discovery/intake and actual-animation-linked playback. Unity authoring, native playback, audible quality, Windows audio and human listening are **not verified by this audit**. Parent owns the serialized Editor and native acceptance.

## Actual owned recordings

The owner-installed `../House-Cat-Simulator/Assets/Malbers Animations/Common/Audio/Wings/` contains five dedicated vendor wing WAVs. All five audio payloads are byte-equal to the owner's cached Animal Controller package. Installed metadata differs, so intake preserves **original cached metadata/GUIDs**. This is exact inspected owner-cache provenance, not a claim that the entire package is pristine.

Cache: `C:/Users/Jeremy/AppData/Roaming/Unity/Asset Store-5.x/MalberS Animations/Editor ExtensionsAnimation/Animal Controller Malbers Character Controller.unitypackage`, SHA256 `390f0b3a0ba495aa18ff9dd7ba6ad0ccaa063dbfd1d4f0e2d5e3e9b7e1505b8e`, 71,338,399 bytes. Individual WAV/metadata SHA256 values, sizes, original pathnames and GUIDs: `DragonWingAudioIntake.json`.

| Original file in Common/Audio/Wings | Actual encoding | Duration | Use |
|---|---|---:|---|
| wing-flap-Big.wav | IEEE float32, stereo44.1kHz | 0.88290s | Adult dragon take |
| Wing Flap Big1.wav | IEEE float32, stereo48kHz | 0.88979s | Adult variation |
| Flap Wing 1.wav | PCM16, stereo48kHz | 0.40800s | Smaller variation, retained but unassigned |
| Flap Wing 2.wav | PCM16, stereo48kHz | 0.48000s | Smaller variation, retained but unassigned |
| Wing Flap 02.wav | PCM24, stereo48kHz | 0.60694s | Smaller variation, retained but unassigned |

Dedicated vendor directory, file identity and cache provenance establish actual wing-recording assets; in-game gain/range and sonic suitability still require listening. Raw stereo WAVs and vendor importer bytes remain exact. Original metadata already enables forceToMono/normalize and background loading; its Standalone override disables preload. Unity imports spatial mono using those existing settings. No new downmix/import-setting edit. Native waveform inspection waits for actual imported background LoadAudioData before GetData.

Bounded filename audits covered ROOT Assets including ignored private folders; known sibling House-Cat, Spirit-Bloom, Assets, Third Party Assets and Asset Playground/Assets collections; and `../UnityAssetPackages/Unity Asset Packages/Assets/Plugins`. ROOT had Malbers Fire audio and NWH TurboFlutter recordings, but no animal wing takes. Emerald `Ambience/Birds Loop.wav` is ambience, not a wingstroke; weapon swings and NWH induction flutter are inappropriate substitutes. No unbounded drive scan or entitlement/download claim. Cached inventory contains79packages; unrelated large packages were not opened after finding authentic wing takes.

## Vendor-first API audit

Installed Malbers sources are under `Assets/TruckDragonKingdom/PrivateLicensedAnimalController/Malbers Animations/Common/Scripts/`.

- `Effects/AnimatorEventSounds.cs`: public `PlaySound(string)` resolves active named EventSound, selects an actual random clip, uses an explicitly assigned reusable AudioSource, sets 3D blend/pitch/gain and plays. **Used unchanged.** Vendor Start zeroes source gain; the adapter applies current shared SFX gain after Start and before emission. Dedicated source prevents interference with mouth/fire sources.
- `Behaviors/SoundBehavior.cs`: one play per state entry or normalized threshold; `played` remains true during a looping state. It cannot observe repeated strokes inside flight loops, and fallback source lookup can search a global name. Not selected.
- `Effects/StepsManager.cs`, `StepTrigger.cs`, `SoundByMaterial.cs`: foot/surface-contact/material audio, unsuitable for airborne wing timing.
- `Scriptables/AudioClipListVar.cs`, `RunTime Collections/AudioClipCollection.cs`, `Animal Controller/AI Brain/Tasks/PlayAudioTask.cs`: collections/task-triggered playback, not real repeated wing-motion sensing. No second AI/audio manager introduced.

Both species' raw `@Fly Forward In Place.FBX.meta` and `@Fly Idle.FBX.meta` contain `events: []` and no named events. No raw FBX/import metadata or original clip/event edits. Existing DragonCharacter.BindAudio controls mouth, fire and breath-subtree sources only; the wing emitter is outside that subtree and does not receive double gain.

## Exact intake

`Tools/TruckSports/ImportDragonWingAudio.py` is dry-run by default. `--apply` writes only ten exact cached files under ignored `Assets/TruckDragonKingdom/PrivateLicensedAudioVfx/MalbersWings/`. It pins package/payload/meta hashes, checks GUIDs/ignore rules/destination containment, refuses differing existing files and scans ROOT Assets metadata for GUID collisions with CRLF-aware matching. No arbitrary archive extraction or source edits. Initial apply copied10; repeat copied0 and verified10. No Unity import performed.

## Explicit native integration

After exact private audio import and configured animals exist, invoke while stopped:

```csharp
LWS.TruckDragonKingdom.Editor.DragonWingAudioAuthoring.InstallConfiguredWingAudio();
```

The installer samples actual Fly/FlyIdle/available Takeoff clips on temporary real models. Both wings must demonstrate meaningful motion (minimum normalized bilateral excursion0.12). Genuine forward Fly must pass; other clips register only when their real motion supports a strong stroke. Native V77 measured Fire FlyIdle excursion0.02620411, so that near-static/glide-like clip deliberately stays silent with the same stroke gate. No arbitrary minimum of two clips or synthetic idle flap. Inferno uses actual terminal Wing04 chains, Fire actual terminal Wing03 chains. Measured excursions determine fractional crest/rise thresholds. No source animation is rewritten. It adds one non-looping AudioSource, unchanged vendor component/event configuration and observer to each flying configured prefab. Existing valid customizations are preserved without resaving. Actor nested prefab references inherit the configured asset change. Spark receives no wings/audio.

Authoring receipt: `-tsDragonOutput/WingAudio/configured-wing-audio.json`, falling back to `Logs/TruckSports/DragonKingdom/WingAudio/configured-wing-audio.json`. Sampling proves binding/motion readiness, not native Animator playback.

Parent has wired the original `DragonCharacter.BindAudio(settings)` to bind its existing child `DragonWingAudio` components. Ground/Rider/Mayhem hosts already call that original API before ordinary Prepare, so **no separate host call is required**. An optional batch equivalent remains available:

```csharp
LWS.TruckDragonKingdom.DragonWingAudio.BindAll(dragons, settings);
```

No core Character/Motor, mode, Foundation or shared authoring source was edited by this subtask; the one Character binding line is parent-owned integration. Without bound SportsSettings the observer is silent. Bind stops/releases prior sound and clears stroke state, including synchronous reset+prepare within one frame.

## Runtime behavior

Actual current/incoming mapped base flight clips must be weighted at least0.4. Both final animated wing tips must rise relative to their real hinges, then depart their crests. Hinge-relative Animator-up sampling removes body travel/roll. Static held wings, IsFlying alone and elapsed time cannot emit. Glide/Bank/Dive clips are not registered as flapping sources; underlying weighted flight can only emit when final bilateral geometry actually strokes. Source playback is nonlooping, cooldown0.4s, and existing takes are not truncated.

Unchanged Malbers owns random selection/playback. One fixed actor emitter, logarithmic6–110m attenuation, Doppler0.15, at most3concurrent wing voices; no spawned emitters, coroutines or per-frame scene searches. Finished/destroyed/disabled sources release bounded reservations. Public counters/last actual mapped clip+weight support observation without event injection.

Source gain: `gain * settings.sfx * observedEmissionWeight`. SportsSettings owns AudioListener master exactly once. Live SFX applies to playing takes. Actor/motor pause freezes sound/stroke tracking and retains its bounded reservation; resume invents no skipped stroke. Unprepared state, explicit bind/reset, disable and destruction stop/release the source. One runtime lifecycle authority; no separate time system.

## Validation limits

`DragonWingAudio.SourceCheck.json`: zero-diagnostic standalone Roslyn runtime/editor and native-fixture compilation against frozen Unity references. No Editor process or CLEAN writes.

`Tests/Native/DragonWingAudioNativeTests.cs`: three parent-run reflection fixtures covering Inferno actual takeoff/flight lifecycle, Fire actual flight lifecycle, and four genuinely flying Fire rigs with at most3sources. Saved configured prefabs and ordinary MoveWorld/Prepare/Pause only; no Animator.Play, test SampleAnimation, pose/velocity injection or fabricated sound callbacks. A read-only probe follows production LateUpdate and records actual bilateral wing-height history/current-incoming clip identity and weight per emission. The corrected history retains4game-seconds at real Fixed Animator evaluations, so high native render rates cannot discard the real preceding upstroke. It preserves all rise/crest/weight/cooldown gates. A single isolated listener follows the observed rig or four-rig mean; original listener enabled states, global gain/pause are restored on cleanup. Exact filter: `DragonWingAudioNativeTests`.

### Actual parent native V89 evidence

`Logs/TruckSports/DragonKingdom/NativeWingCameraV89/tests.xml` passes4/4 in82.8481634s: three wing fixtures plus the separate Heist normal broadcast fixture. The wing fixtures pass21.274817s Fire,11.749121s four-flight,24.158850s Inferno. Retained real CSVs are under that run's `rendered-evidence/WingAudio/`.

| Actual observation | Fire | Inferno |
|---|---:|---:|
| Lifecycle emissions across initial play/rematch | 8 | 6 |
| Frames with source sample position advancement | 198 | 144 |
| Maximum observed software source/listener mixed peak | 0.07176478 | 0.06692415 |
| Recorded inter-emission intervals, including pause/mute stages | 1.260–4.080s | 1.216–5.100s |

Actual variable cadence follows real supported strokes/cooldown and silence stages, not a fabricated constant loop. Four actual flying rigs emit17takes total, maximum3simultaneous; individual emitted counts5/3/4/5. Their actual aggregate software mixed peak reaches0.04618686. Fire pause holds positive source sample26978 unchanged, SFX stage holds volume0/no new emission, master0 remains original AudioListener authority, ordinary bind/Prepare rematch resets counters and reuses emitters. Existing source snapshots prove decoded nonzero owned audio and imported mono data. Software GetOutputData/DSP proof is **not** hardware, listening, Windows, human, normal-speed performance or full-game acceptance.

V86's original13product-motion passes and three witness failures remain retained. The false history failures came from256render-frame history spanning only≈0.15–0.2game-seconds at1000–1700render frames/s. Offline inspection of21actual V86emissions shows9old-window failures and0with identical thresholds over four game-seconds. V89 corrects the witness/listener and genuinely passes all three native cases; production runtime is unchanged.

V89 warned when the diagnostic read timeSamples before the source had a clip. The narrow current test-source follow-up guards that read with `source.clip != null`; actual sample-advancement/DSP/stroke/cadence assertions and counters remain. That guarded source compiles0 but is not natively rerun by this agent; the passed native baseline SHA726230287bc841a1ae8ee8d6a0b319750082a3e84796028956aa2d4bba081392 remains explicit in `DragonWingAudio.SourceCheck.json`. No native Editor was launched by this agent.

VENDOR ASSETS AUDITED: Malbers Common audio/event/state/step/collection/task APIs and real flight metadata; owned cache and bounded installed audio inventories; original SportsSettings/DragonCharacter audio.

VENDOR ASSETS USED: five exact cached Malbers wing recordings/metadata, two adult takes configured; unchanged AnimatorEventSounds/EventSound; actual Meshtint wing animation/Malbers controller; original SportsSettings master/SFX.

RELEVANT ASSETS NOT USED: state-entry SoundBehavior for repeated cycles; foot/material/task audio for wing timing; NWH TurboFlutter, weapon swings and bird ambience as substitute strokes; smaller three wing recordings retained but unassigned.

CUSTOM SYSTEMS CREATED: narrow actual-wing/shared-lifecycle adapter, explicit configuration/sample audit, exact private intake, provenance and three native fixtures.

VENDOR SOURCE MODIFIED: None.

DUPLICATE VENDOR FUNCTIONALITY CREATED: None; vendor public API owns clip randomization and actual spatial playback.
