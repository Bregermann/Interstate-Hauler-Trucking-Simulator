# Needs Presentation Assets

Generated for Truck Taxi's hunger/thirst consequence presentation on 2026-10-09.
These are transparent bitmap overlays, not replacement world or vehicle art.

## Assets

- `Assets/LWS/TruckTaxi/Resources/TruckTaxi/Needs/Hunger.png`: burger.
- `Assets/LWS/TruckTaxi/Resources/TruckTaxi/Needs/Thirst.png`: glass of ice water.
- Generator: built-in image-generation tool; transparent background enabled.
- Source outputs: `C:/Users/Jeremy/.codex/generated_images/019fefac-6230-7731-8406-588128ebbbfe/exec-63d6afbc-32ee-4eb9-b566-97aa3029a6da.png`
  and `exec-54012b76-ea35-4641-9a25-dd3386985287.png` in the same directory.
- Brief: single isolated burger / ice-water glass, stylized realistic 3D,
  no logos, text, scenery or hands, transparent padding, readable at small HUD
  size as a semitransparent driving overlay. The source images were visually
  inspected before import; in-game readability still requires rendered checks.

## Runtime

`TruckTaxiStatusBars` reuses the existing HUD canvas. It creates at most two
noninteractive RawImages once, preserves the source aspect ratio, and moves
them within small bounded areas away from screen center. The existing 0.15s
status refresh controls alpha; no texture or material is created per frame.

`TruckTaxiEnvironmentSettings` exposes the threshold (0.9), maximum opacity
(0.5), optional replacement textures, optional sound clips and sound interval
(30 seconds). Relieving the relevant need clears its image. Pausing or hiding
the normal HUD clears both images and stops any playing need sound.

No suitable installed gulp/growl clip was identified. Both audio fields remain
optional and unassigned; no voice generation, borrowed unrelated clip or fake
audio validation is claimed. `TruckTaxiNeedsImageImporter` limits only these
textures to 512px, alpha transparency, no mipmaps and no CPU-readable copy.
