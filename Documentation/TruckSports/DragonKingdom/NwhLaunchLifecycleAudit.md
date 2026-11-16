# NWH rematch launch lifecycle audit

Read-only source/evidence audit; parent owns GroundMatch and input fixtures. No vehicle mass, engine power curve, clutch, traction setting or vendor source was changed by this audit.

V155 showed identical.7 throttle launching one Heist crew while another.7295 crew cycled around379–443RPM, below automatic clutch engagement. Sampled input, NWH input, gear and brake ownership were correct. TCS was an initial hypothesis because installed TCSPowerLimiter does not use its documented minimum-speed field. V156/V158 cached telemetry falsifies that explanation for these failures: TCS is disabled and the only delegates are TaxiHandling.PowerModifier and TrailerHitch.NoTrailerPowerModifier. Do not add a TCS fix on this evidence.

V158's failed Escape crew0 has user throttle.7, cached power multiplier2.72, automatic clutch engagement0 and real output RPM433.8407. Every held sample after startup reports engine throttle exactly.2 and negative generated power (for example−3.386181kW). It remains stationary. Successful Heist crews reach actual2730RPM/clutch1 and move with the same control class.

The installed EngineComponent source supplies a specific explanation. CalculateICEGeneratedTorqueFromPowerCurve first zeros generatedPower; when revLimiterActive is true it assigns throttle.2 and skips the generated-power calculation. CalculateTorqueICE also quarters loss torque in that branch. Thus the observed.2/loss-only signature far below the limiter RPM is strong evidence of a retained revLimiterActive flag, rather than insufficient assigned throttle.

The flag is an actual public NonSerialized field. RevLimiterCoroutine sets it true, yields, then sets it false. VehicleController.OnDisable calls StopAllCoroutines. Engine.VC_Disable/StopEngine and FlyingStart do not clear revLimiterActive. Disabling a crew during a real limiter cutoff can therefore cancel the only normal clearing step and retain the transient through rematch. This is a source inference tied to the executed signature; direct flag telemetry and the selected lifecycle regression remain parent-owned next evidence.

A narrow proposed correction is clearing that public transient only during ordinary new-attempt preparation after old vehicle coroutines have been cancelled, preserving configured cutoff duration and normal running limiter behavior. Public StartEngine before the vendor fixed step may address cold-start cached state, but FlyingStart alone does not clear the retained limiter flag. Never clear the limiter continuously during normal gameplay.

Telemetry should read cached modifier values, flags, actual clutch and signed wheel slip. Extra-invoking delegates is not read-only: TCSPowerLimiter changes state and invokes events.

- VENDOR ASSETS AUDITED: installed NWH EngineComponent, ClutchComponent, Powertrain, VehicleController, TCS/ESC/trailer modules; Taxi handling adapter.
- VENDOR ASSETS USED: actual cached native telemetry and existing public lifecycle APIs.
- RELEVANT ASSETS NOT USED: TCS mutation; the executed snapshots show it disabled.
- CUSTOM SYSTEMS CREATED: none in this read-only audit.
- VENDOR SOURCE MODIFIED: none.
- DUPLICATE VENDOR FUNCTIONALITY CREATED: none.
