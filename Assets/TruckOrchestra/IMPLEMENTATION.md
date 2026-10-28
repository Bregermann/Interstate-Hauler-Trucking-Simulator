# Truck Orchestra — source checkpoint

Status: source implemented and cached C# compilation passed; the Unity scene, actual native tests, menu integration and Windows acceptance are pending. This file does not claim a playable build.

The new `OrchestraMatch` derives from the existing `DanceMatch`. Dance-Off keeps authority over its DSP clock, timestamped device inputs, calibration, AI input events, note/hold judgments, combos, pause/resume and complete song/series flow. Small default-preserving extension points supply separate instrumental charts and preparation callbacks. A presentation-only note-head event plays a held note immediately; its score still waits for the original tail judgment.

The explicit new-scene author creates seven original musical arrangements and four separate instrument parts at four difficulties. Piano keys, percussion/cymbals, brass horns and marimba/bells respond to accepted input. The original harmonic backing contains none of these playable sections. Wrong notes produce a bounded quiet mistake. The existing concert Cinemachine shot selector is reused. An original UTS rig conducts through explicitly bound generic bones; existing crowd animation and original NWH bodies are retained. The secondary suspended cab follows the established stage choreography as a mode-local assembly adapter, without changing NWH or driving modes.

`OrchestraAudioAuthoring` saves real original WAV clips and editable score/library assets. It is an explicit authoring operation, not an automatic startup or build regeneration step. Existing authored scores refuse replacement. The main source entry is `LWS.TruckOrchestra.Editor.OrchestraAuthoring.Create`; it refuses to overwrite an existing scene.

Targeted verification authored: eight actual shared-rule cases for note-head/hold-tail separation, chords, misses and independent owners; actual complete four-part AI concert with conductor/instrument animation, pause/rematch; actual timestamped keyboard ownership with one through four participants. Controlled captures are labeled separately from Windows rendering. Listening, physical controllers, human playtest and profiling remain unperformed.

## VENDOR ASSETS AUDITED

Installed Dance-Off runtime, chart rules, DSP/calibration, pooled lane HUD, original concert camera/pipeline; NWH full Fare tractor and cab; UTS generic people rigs/clips; Heat UI; existing maple/brass materials. Scoped installed-asset searches found no suitable piano, drum, brass, marimba or conductor model/animation.

## VENDOR ASSETS USED

Original NWH assembly; existing Cinemachine adapter and concert shots; Heat UI and directional sprites; DOTween choreography through the existing Dance rig; UTS people and existing crowd controller; Unity audio playback/import, original project settings/profile/crew architecture.

## RELEVANT ASSETS NOT USED

Weather Maker and Weatherade are not a second weather authority inside this enclosed concert hall. Existing rhythm rules, calibration and device routing are reused directly. There is no installed suitable instrument asset to reference; original instrument art and scores fill that gap.

## CUSTOM SYSTEMS CREATED

Instrumental-part data, original arrangement/audio authoring, concert hall/instrument geometry, note-driven instrument presentation, specific conductor routine, full-cab stage-pose coordination and Orchestra match semantics. These are mode adapters and missing content, not replacement frameworks.

## VENDOR SOURCE MODIFIED

None. Three existing project-owned Dance files receive small backward-compatible extension points; targeted Dance regression is required before delivery.

## DUPLICATE VENDOR FUNCTIONALITY CREATED

None intended. No new rhythm judgment, input backend, camera backend, UI framework, vehicle controller, save backend or audio engine.
