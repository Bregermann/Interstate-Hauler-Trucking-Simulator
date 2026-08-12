# Prompt 006 - 18-Speed Gear Map

This table is the current LWS development mapping for the G29 six-speed shifter. It is intended to prove range/splitter architecture and NWH 18-forward-gear routing. Ratio values are development seed values and must be tuned/verified before production.

| Physical Gate | Range | Splitter | Logical Gear | Display | Logical Ratio Index | NWH Gear | Development Ratio |
|---|---|---|---|---|---:|---:|---:|
| Gate 2 | Low | Low | LowLow | LO-L | 1 | 1 | 14.40 |
| Gate 2 | Low | High | LowHigh | LO-H | 2 | 2 | 12.29 |
| Gate 3 | Low | Low | Gear1Low | 1L | 3 | 3 | 8.56 |
| Gate 3 | Low | High | Gear1High | 1H | 4 | 4 | 7.30 |
| Gate 4 | Low | Low | Gear2Low | 2L | 5 | 5 | 6.05 |
| Gate 4 | Low | High | Gear2High | 2H | 6 | 6 | 5.16 |
| Gate 5 | Low | Low | Gear3Low | 3L | 7 | 7 | 4.38 |
| Gate 5 | Low | High | Gear3High | 3H | 8 | 8 | 3.74 |
| Gate 6 | Low | Low | Gear4Low | 4L | 9 | 9 | 3.20 |
| Gate 6 | Low | High | Gear4High | 4H | 10 | 10 | 2.73 |
| Gate 3 | High | Low | Gear5Low | 5L | 11 | 11 | 2.29 |
| Gate 3 | High | High | Gear5High | 5H | 12 | 12 | 1.95 |
| Gate 4 | High | Low | Gear6Low | 6L | 13 | 13 | 1.62 |
| Gate 4 | High | High | Gear6High | 6H | 14 | 14 | 1.38 |
| Gate 5 | High | Low | Gear7Low | 7L | 15 | 15 | 1.17 |
| Gate 5 | High | High | Gear7High | 7H | 16 | 16 | 1.00 |
| Gate 6 | High | Low | Gear8Low | 8L | 17 | 17 | 0.86 |
| Gate 6 | High | High | Gear8High | 8H | 18 | 18 | 0.73 |

## Reserved And Invalid Positions

| Physical State | Result |
|---|---|
| Neutral | LWS neutral, NWH gear `0` |
| Reverse | LWS reverse, NWH gear `-1`, development ratio `-12.85` |
| Gate 1 | Reserved/unused in Prompt 006 |
| Gate 2 + High Range | Invalid range/gate combination |

## Runtime Gear List

The spawned NWH `TransmissionComponent.gears` list is configured as:

1. reverse ratio
2. neutral
3. 18 forward ratios in NWH gear-index order

LWS resolves the logical state first, then asks NWH to shift with:

`VehicleController.powertrain.transmission.ShiftInto(targetGear, instant: true)`
