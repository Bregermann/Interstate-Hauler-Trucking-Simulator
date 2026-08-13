| Lane ID | Source Road | Source Segment | Direction | Road Class | Lanes | Speed Limit | Spawn Enabled | UTS Path | Notes |
|---|---|---|---|---|---:|---:|---|---|---|
| `IH_TEST_I000_NB_MAIN_TRAFFIC_L1` | `IH_TEST_I000_NB` | `IH_TEST_I000_NB_MAIN` | Northbound | Interstate | 1 of 2 | 65 MPH | Yes | Generated | Derived from LWS lane offset 1. |
| `IH_TEST_I000_NB_MAIN_TRAFFIC_L2` | `IH_TEST_I000_NB` | `IH_TEST_I000_NB_MAIN` | Northbound | Interstate | 2 of 2 | 65 MPH | Yes | Generated | Derived from LWS lane offset 2. |
| `IH_TEST_I000_SB_MAIN_TRAFFIC_L1` | `IH_TEST_I000_SB` | `IH_TEST_I000_SB_MAIN` | Southbound | Interstate | 1 of 2 | 65 MPH | Yes | Generated | Uses southbound edge direction from Prompt 009 graph. |
| `IH_TEST_I000_SB_MAIN_TRAFFIC_L2` | `IH_TEST_I000_SB` | `IH_TEST_I000_SB_MAIN` | Southbound | Interstate | 2 of 2 | 65 MPH | Yes | Generated | Uses southbound edge direction from Prompt 009 graph. |
| `IH_TEST_I000_NB_ENTRY_RAMP_TRAFFIC_L1` | `IH_TEST_I000_RAMP` | `IH_TEST_I000_NB_ENTRY_RAMP` | Northbound | Ramp | 1 | 35 MPH | Yes by policy | Generated | Included by default as a simple ramp validation lane. |
| `IH_TEST_I000_TURNAROUND_CROSSOVER_TRAFFIC_L1` | `IH_TEST_I000_TURN` | `IH_TEST_I000_TURNAROUND_CROSSOVER` | Bidirectional | Ramp | 1 | 25 MPH | No by default | Not generated | Deferred until bidirectional/turnaround traffic rules exist. |
