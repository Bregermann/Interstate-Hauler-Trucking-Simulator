# Prompt 012 - Weather Preset Matrix

| Preset | Condition | Precipitation | Intensity | Cloud | Fog | Wind | Lightning | Transition | Prompt013 Input | Notes |
|---|---|---|---:|---:|---:|---:|---|---|---|---|
| `clear` | Clear | None | 0.00 | 0.05 | 0.00 | 2 m/s | No | Smooth or instant | Dry weather input | Maps to `WeatherMakerProfile_Clear`. |
| `partly_cloudy` | Partly Cloudy | None | 0.00 | 0.35 | 0.02 | 3 m/s | No | Smooth or instant | Dry/cloud input | Maps to `WeatherMakerProfile_LightCloudsScattered`. |
| `cloudy` | Cloudy | None | 0.00 | 0.62 | 0.05 | 4 m/s | No | Smooth or instant | Dry/cloud input | Maps to `WeatherMakerProfile_MediumHeavyClouds`. |
| `overcast` | Overcast | None | 0.00 | 0.90 | 0.12 | 5 m/s | No | Smooth or instant | Dry/low-light input | Maps to `WeatherMakerProfile_OvercastClouds`. |
| `light_rain` | Light Rain | Rain | 0.28 | 0.75 | 0.18 | 6 m/s | No | Smooth or instant | Wetness input potential only | No traction or road wetness applied in Prompt 012. |
| `heavy_rain` | Heavy Rain | Rain | 0.82 | 0.95 | 0.32 | 9 m/s | No | Smooth or instant | Strong wetness input potential only | No hydroplaning or tire friction changes. |
| `thunderstorm` | Thunderstorm | Rain | 0.95 | 1.00 | 0.45 | 14 m/s | Yes | Smooth or instant | Storm/wetness input potential only | Uses `WeatherMakerProfile_Storm`; no lightning damage. |
| `light_snow` | Light Snow | Snow | 0.32 | 0.72 | 0.18 | 4 m/s | No | Smooth or instant | Snow input potential only | Falling snow only; no road accumulation. |
| `heavy_snow` | Heavy Snow | Snow | 0.82 | 0.95 | 0.36 | 8 m/s | No | Smooth or instant | Strong snow input potential only | Falling snow only; no traction changes. |
| `fog` | Fog | None | 0.00 | 0.50 | 0.88 | 2 m/s | No | Smooth or instant | Visibility input | Fog does not affect vehicle physics. |

All project-owned preset assets live in:

`Assets/LWS/InterstateHauler/Weather/Data/`

All presets expose stable IDs for future save/load and Prompt 013 road-condition orchestration.
