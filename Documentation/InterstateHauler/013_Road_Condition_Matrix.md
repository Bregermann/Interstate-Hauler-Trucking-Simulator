# Prompt 013 - Road Condition Matrix

| Condition | Major State | Typical Cause | Weatherade Visual | Longitudinal | Lateral | Braking | Rolling | Notes |
| --- | --- | --- | --- | ---: | ---: | ---: | ---: | --- |
| Dry | DRY | Clear/dry weather | None or cleared rain coverage | 1.00 | 1.00 | 1.00 | 1.00 | Restores cached NWH dry baseline. |
| Damp | DRY | Light rain residue or drying wet road | Rain wetness only | 0.92 | 0.95 | 0.90 | 1.03 | Minor handling change, still considered dry gameplay bucket. |
| Wet | WET | Rain accumulation | Rain wetness/spots/drips | 0.72 | 0.78 | 0.68 | 1.08 | Noticeable longer stopping distance expected. |
| StandingWater | WET | Heavy rain above wetness threshold | Puddles and ripples | 0.50 | 0.58 | 0.45 | 1.18 | Adds hydroplaning risk at speed. |
| LightSnow | SNOW | Snowfall at cold surface temperature | Snow coverage | 0.42 | 0.50 | 0.38 | 1.22 | Fresh snow; compacts over time. |
| PackedSnow | SNOW | Accumulated snow compaction | Snow coverage | 0.30 | 0.36 | 0.27 | 1.28 | Low grip, high rolling resistance. |
| Ice | ICE | Moisture plus freezing surface temperature | Partial snow/ice visual coverage | 0.14 | 0.18 | 0.12 | 1.10 | Most severe grip loss; needs careful NWH tuning. |
| Mixed | ICE | Snow and ice overlap | Snow coverage | 0.32 | 0.40 | 0.30 | 1.24 | Mixed bucket is treated as ICE for gameplay caution. |

## Default Profile

Asset:

`Assets/LWS/InterstateHauler/Roads/Conditions/Data/IH_RoadConditionPhysics_Default.asset`

The default values are intentionally conservative starting points. Production tuning should use measured stopping-distance and trailer-stability tests under representative cargo loads.
