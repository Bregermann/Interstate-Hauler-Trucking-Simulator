# Prompt 009 Road Graph Matrix

| Road ID | Segment ID | Start Node | End Node | Direction | Road Class | Lane Count | Speed Limit | Length | EasyRoads Source | Traffic Ready | GPS Ready | Notes |
|---|---|---|---|---|---|---:|---:|---:|---|---|---|---|
| `IH_TEST_I000_NB` | `IH_TEST_I000_NB_MAIN` | `IH_TEST_I000_NB_MAIN_START` | `IH_TEST_I000_NB_MAIN_END` | Northbound | Interstate | 2 | 65 MPH | ~3.4 km | `IH_TEST_I000_NB_MAIN` | Partial | Partial | Primary outbound highway test direction. |
| `IH_TEST_I000_SB` | `IH_TEST_I000_SB_MAIN` | `IH_TEST_I000_SB_MAIN_START` | `IH_TEST_I000_SB_MAIN_END` | Southbound | Interstate | 2 | 65 MPH | ~3.4 km | `IH_TEST_I000_SB_MAIN` | Partial | Partial | Return carriageway, reversed sample order. |
| `IH_TEST_I000_RAMP` | `IH_TEST_I000_NB_ENTRY_RAMP` | `IH_TEST_I000_NB_ENTRY_RAMP_START` | `IH_TEST_I000_NB_ENTRY_RAMP_END` | Northbound | Ramp | 1 | 35 MPH | ~0.3 km | `IH_TEST_I000_NB_ENTRY_RAMP` | Partial | Partial | Development merge/entry area from service pad. |
| `IH_TEST_I000_TURN` | `IH_TEST_I000_TURNAROUND_CROSSOVER` | `IH_TEST_I000_TURNAROUND_CROSSOVER_START` | `IH_TEST_I000_TURNAROUND_CROSSOVER_END` | Bidirectional | Ramp | 1 | 25 MPH | ~0.1 km | `IH_TEST_I000_TURNAROUND_CROSSOVER` | No | Partial | Oversized development turnaround/crossover. |

Traffic Ready means lane metadata exists for Prompt 010, not that UTS traffic has been configured.

GPS Ready means LWS topology and samples exist for Prompt 011/Compass presentation, not that production route computation exists.
