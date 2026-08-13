# Prompt 011 - GPS Test Matrix

| Test | Type | Status | Notes |
|---|---|---|---|
| Route planner finds graph route | EditMode | Automated | `LwsNavigationEditModeTests.RoutePlannerFindsDirectionalRouteThroughGraph`. |
| Route planner respects one-way roads | EditMode | Automated | Reverse route fails on one-way graph. |
| Maneuver angle bands | EditMode | Automated | Straight, slight, turn, sharp, ramp semantics. |
| Route step generation | EditMode | Automated | Start and arrival steps validated. |
| Voice pack slot coverage | EditMode | Automated | Default voice pack must expose all maneuver slots. |
| GPS Voice Guidance default ON | EditMode | Automated | PlayerPrefs default is ON. |
| Voice disabled suppresses prompts | EditMode/PlayMode | Automated | Visual route remains active in PlayMode. |
| Off-route state | EditMode | Automated | Far pose enters OffRoute. |
| No UTS route-planner dependency | EditMode | Automated | Source check rejects UTS/CarAI/WalkPath names. |
| Navigation service initialization | PlayMode | Automated | Service registry initializes navigation stack. |
| Test route starts | PlayMode | Automated | Route active with destination ID. |
| Physical GPS presenter binds | PlayMode | Automated | World-space screen child created under `Cab`. |
| Reroute no duplicate service | Validator/manual | Partial | Validator checks service registration; live reroute manual still required. |
| InterstateCorridorValidation route | Manual Editor | Required | Start route, drive, step advance, recalc, arrival. |
| Cockpit GPS readability | Manual Editor | Required | Check cab view, 16:9 and 16:10. |
| Voice with clips assigned | Manual Editor | Required | Clips are not assigned yet. |
| Traffic regression | Manual Editor | Required | User already passed Prompt 010 traffic; recheck after GPS. |
| G29/18-speed regression | Manual Editor/hardware | Required | Prompt 011 does not touch input/transmission authority. |
| Dashboard/mirror regression | Manual Editor | Required | Prompt 011 should not interfere with Prompt 008. |
| Performance/GC steady driving | Manual Profiler | Required | Code is throttled; live profiler pass remains recommended. |
