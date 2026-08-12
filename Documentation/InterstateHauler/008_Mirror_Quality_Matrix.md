# Prompt 008 Mirror Quality Matrix

| Quality | Enabled | Resolution | Update Rate | Culling | Shadows | Post Processing | Expected Platform | Measured Cost | Notes |
| --- | --- | ---: | ---: | --- | --- | --- | --- | --- | --- |
| Off | No | 0 | 0 | N/A | Off | Off | Extreme fallback/debug | Not measured | Cameras disabled; not normal cockpit default. |
| Low | Yes | 768 | every 3 frames | UI layer excluded where present | Off | Off | Low PC / Steam Deck | Manual required | Useful but low-cost starting point. |
| Medium | Yes | 1024 | every 2 frames | UI layer excluded where present | Off | Off | Main scalability tier | Manual required | Balanced cost/visibility. |
| High | Yes | 1536 | every frame | UI layer excluded where present | Off | Off | Desktop PC default | Manual required | Primary normal PC profile. |
| Ultra | Yes | 2048 | every frame | UI layer excluded where present | Allowed by preset | Off | High-end PC | Manual required | Visual ceiling without full-screen RT waste. |

`Assets/LWS/InterstateHauler/Rendering/Data/IH_RenderingSettings.asset` remains the active rendering-quality source. The mirror controller uses the active rendering profile's mirror quality, resolution, and update interval where available.

Weather, shadow, and post-processing cost inside mirrors still require normal Editor profiling. Steam Deck physical performance verification is required later.
