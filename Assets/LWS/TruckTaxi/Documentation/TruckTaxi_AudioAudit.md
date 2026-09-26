# Truck Taxi Audio Audit

## Verified Baseline

Live Editor inventory in TruckTaxi_DemoCity found 20 AudioSources at startup. Evidence:
`Builds/TruckTaxiDemo/Validation/AudioSourcesBefore.txt`. No generated passenger clips were reassigned or created.

| Category | Existing mixer/group | Exposed gain | Sources | Player control before | Action |
| --- | --- | --- | --- | --- | --- |
| Vehicle engine/turbo/start/fan | NWH VehicleAudioMixer / Engine | No independent volume | NWH EngineAudioSources | No | Preserve internal mix; route a project-owned NWH mixer copy into Vehicle |
| Transmission | NWH / Transmission | No independent volume | Whine and gear-change sources | No | Preserve internal DSP/source mix under Vehicle |
| Tires/surface | NWH / SurfaceNoise | No independent volume | Skid/tire noise sources | No | Preserve under Vehicle |
| Horn/brakes/impacts/signals/suspension | NWH / Other | No independent volume | Existing NWH component sources | No | Preserve under Vehicle |
| Passenger dialogue | None | None | TruckTaxiPixelCrushersBarkAdapter, authored source volume 0.9 | No | Route into Voices; subtitles are independent |
| Dispatch/objective notifications | None | None | TruckTaxiBootstrap, source volume 1 | No | Route into UI |
| Compass UI | None, no assigned clips at baseline | None | Existing HUD/cab Compass sources | No audible content | Keep no empty GPS slider; route any enabled UI sounds into UI |
| Weather | No Taxi Weather Maker instance at baseline | Vendor source-level mix | Rain/thunder/wind when new environment is active | No | Route Weather Maker's known runtime subtree into World |
| Music | No Taxi music source | Heat has unused Music gain | None in this scene | Not applicable | No empty Music control |
| Traffic/pedestrians | No AudioSources in baseline UTS population | None | None | Not applicable | No empty traffic/pedestrian sliders |

Mixers actually returned by Unity's public API:

- `Assets/Heat - Complete Modern UI/Audio/_Mixer.mixer`: Master, Music, SFX, UI. Exposed Master/Music/SFX/UI all read 0 dB.
- `Assets/NWH/Vehicle Physics 2/Resources/NWH Vehicle Physics 2/Defaults/Sound/VehicleAudioMixer.mixer`: Master, Engine, Transmission, SurfaceNoise, Other. Exposed engineDistortion, attenuation, lowPassQ, lowPassFrequency are vehicle DSP controls, not independent player volume sliders. Do not overwrite them with player volume values. Some orphan serialized group names are not reachable groups returned by FindMatchingGroups and are not advertised as functional controls.

## Required Mixer Authoring

**Configured and verified in the expansion integration pass.** The designer-provided project mixer is present. Unity's public API resolved all five groups and all exposed gains at 0 dB before runtime preferences. No vendor `.mixer` source was modified and no mixer YAML/private authoring API was used.

`Assets/LWS/TruckTaxi/Audio/TruckTaxi.mixer` has Master with Voices, Vehicle, World, UI children. The Volume controls are exposed as `MasterVolume`, `VoicesVolume`, `VehicleVolume`, `WorldVolume`, `UIVolume`. `Truck Taxi/Configure Presentation Assets` has been run; the routing asset and project-owned NWH mixer copy are assigned.

The setup validates every group and exposed parameter, copies NWH's mixer through AssetDatabase.CopyAsset to `TruckTaxiVehicle.mixer`, and creates `TruckTaxiAudioRouting.asset`. Re-running does not reset authored defaults. The runtime routes the project NWH mixer's output to Vehicle and retains Engine/Transmission/SurfaceNoise/Other grouping, filters, source volumes, and engineDistortion control. Individual vehicle submix volume parameters can be exposed in a future audio-mixing pass; they are not silently invented here.

## Settings Behavior

Existing HUD canvas and Heat sliders; accessible through Pause > Audio Settings. Existing UI Input System navigation supports arrows/WASD, gamepad D-pad/stick, Enter/South and Escape/East. Each valid authored category uses 0-100 display values and `20 * log10(linearVolume)` with -80 dB at zero. Namespaced preferences are `LWS.TruckTaxi.Audio.v1.<category>`. Defaults restore each bus's captured authored dB, not an arbitrary all-100 mix. Audio preferences are not career save data.

Passenger voice mute only changes mixer gain. Pixel Crushers bark timing and subtitles remain untouched. Weather routing checks only its registered runtime subtree periodically, not the entire scene each frame. Source routes and NWH references are restored when the Taxi audio component is removed.

The integrated Play Mode check resolved five working buses. The liquid loop was actually played, paused, resumed, stopped and released in a focused PlayMode test. This is engineering playback/routing evidence, not subjective listening approval. New impact, fuel and container effects use World; no passenger voices were generated.

## Notification

The baseline configuration's offerSound was null. `TruckTaxiPresentationSetup` fills it only when unassigned, using Heat's existing `Audio/Notification.wav`. One offer model can claim its notification once; redraws/debug timer changes do not re-claim it. New offers receive fresh IDs. Notification routing uses UI, under Master once the required mixer is configured.
