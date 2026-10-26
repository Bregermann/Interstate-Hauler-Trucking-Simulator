# Quaternius animated birds — CC0 source record

Artist: Quaternius. Original artist uploads declare **CC0**:

- [Animated Animales Low Poly — Animal Pack Vol.2](https://opengameart.org/content/animated-animales-low-poly)
- [5 Low Poly Animals — Animals Pack](https://opengameart.org/content/5-low-poly-animals)
- [CC0 1.0 public-domain dedication](https://creativecommons.org/publicdomain/zero/1.0/)

Intake reviewed October 25, 2026. Saved original source/license HTML, archive member review and complete archives are retained under `.sports-validation/AssetIntake/Quaternius`. No executable, script, Blender file, unrelated animal or archive code was imported or executed.

| Retained original asset | Archive member | SHA256 of retained bytes |
| --- | --- | --- |
| Eagle.fbx | Animal Pack Vol.2 by @Quaternius/FBX/Eagle.fbx | c3504c8e0904eab7b14e695609a861417f089cf724b94bda821ea4a3bbc6f505 |
| bird.fbx | Animals Pack by Quaternius/FBX/bird.fbx | a6a75edc894ae4d9d78cecb10d9587f4bf99107e1c737222ca9b82a78a0af493 |

Archive SHA256:

- animated-vol2.zip: `6d8f6789d65dbdb352ebad98a4209f26b41b384e87d81bc05e9ca76139db1248`
- animals-vol1.zip: `b3268015beed5d1e1d745e004a37ca2ec215e6ac1bf37541f91c1908c58a2e39`

Read-only binary FBX 7400 inspection found an actual eagle skeleton, `EagleArmature|Flying` and idle takes; bird.fbx contains an actual skeleton and `Armature|ArmatureAction`. Both have embedded material slots; the reviewed bird/eagle material descriptions have no external texture maps. The older bird file also contains camera/light nodes, disabled by the explicit importer.

Original FBX bytes are retained unchanged. LWS authoring creates owned Generic import settings, looping animation controllers, URP material palettes, fitted visual prefabs, physical body proxies and course coordination. These are derivatives/configuration, not original artist animation. The native author reports actual imported clips; native tests must verify wing/skinned-pose motion and bounded contact before runtime acceptance. This source record alone is not runtime verification.
