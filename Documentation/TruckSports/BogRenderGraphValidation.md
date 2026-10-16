# Bog Render Graph validation

## Validation summary

Unity6000.4.10f1 / URP17.4. Compilation and actual native GPU comparisons pass: submerged treatment visibly changes the image, while an above-water camera remains unaffected. Hysteresis and scene-exit pipeline restoration pass. Final Windows mixed-viewport and performance checks remain required at this checkpoint.

## Confirmed issues

The legacy Weather Maker lake integration produced black/invisible surface renders in this scoped URP configuration. Diagnostics separated surface, fog and postprocessing. Installed River Modeler URP water is now used, with green transparency-removal mask, alpha foam mask and flow UV data configured correctly. Real above-water on/off comparison confirms a rendered surface. Vendor source is unchanged.

## Risky assumptions

URP can supply camera-local textures or a larger viewport atlas. The pass derives target dimensions from the active graph source and applies camera-rect masking only to the larger target. Final Windows mixed-depth full-window inspection is still needed; Camera.Render comparisons alone do not certify every display/backend.

## Recommended fixes and wiring

The engine AddBlitPass helper binds source color explicitly to _BlitTexture and owns static execution. ConfigureInput requests depth; UseGlobalTexture declares read access. Destination comes from graph.GetTextureDesc(source), retaining dimensions/format while overriding name/depth/clear behavior. resources.cameraColor advances to the destination. Each camera owns a MaterialPropertyBlock. Underwater tint does not mutate shared fog or another view's material parameters.

## Corrected snippets

No further Render Graph rewrite was justified by the passing compile/GPU evidence. The surface fix reuses installed shader/material functionality and adapts owned mesh data to its contract.

## Missing information

Final Windows screenshot, automatic four-camera render counts and sustained timing. Real-display/controller/audio acceptance remains manual. Per-truck engine filtering feeds one shared listener, not independent headphones per viewport.
