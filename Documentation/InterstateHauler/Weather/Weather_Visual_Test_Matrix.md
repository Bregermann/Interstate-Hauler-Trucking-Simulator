# Weather Visual Test Matrix

Automated status verifies architecture only. Visible status must be confirmed in the normal Unity Editor Game view.

| Test | Scene | Expected Visible Result | Automated Coverage | Visible Status | Notes |
|---|---|---|---|---|---|
| Clear | InterstateCorridorValidation | no precipitation, clear Weather Maker sky | preset asset and adapter mapping | UNVERIFIED | Select Clear in Development Control Center. |
| Partly Cloudy | InterstateCorridorValidation | more clouds than Clear | preset asset mapping | UNVERIFIED | Uses WeatherMakerProfile_LightCloudsScattered. |
| Overcast | InterstateCorridorValidation | dense cloud cover and dimmer atmosphere | preset asset mapping | UNVERIFIED | Uses WeatherMakerProfile_OvercastClouds. |
| Light Rain | InterstateCorridorValidation | visible rain, wetting road starts | adapter/material architecture | UNVERIFIED | Road material must report compatible in Road Conditions tab. |
| Heavy Rain | InterstateCorridorValidation | obvious heavy rain and wet road | adapter/material architecture | UNVERIFIED | Must look stronger than Light Rain. |
| Thunderstorm | InterstateCorridorValidation | storm clouds, heavy rain, lightning/thunder where vendor supports | storm profile includes LightningProfile | UNVERIFIED | Uses WeatherMakerProfile_Storm. |
| Fog | InterstateCorridorValidation | visibly reduced road visibility | fog profile mapping | UNVERIFIED | Uses WeatherMakerProfile_MediumFog. |
| Light Snow | InterstateCorridorValidation | visible snow particles and early surface snow | adapter/material architecture | UNVERIFIED | Uses WeatherMakerProfile_LightSnow and Weatherade SnowCoverage. |
| Heavy Snow | InterstateCorridorValidation | heavier snow and visible Weatherade snow coverage | adapter/material architecture | UNVERIFIED | Must look stronger than Light Snow. |
| Noon | InterstateCorridorValidation | daytime sun/sky/lighting | game clock adapter test | UNVERIFIED | Set Noon from Weather tab. |
| Midnight | InterstateCorridorValidation | night sky/lighting | game clock adapter test | UNVERIFIED | Set Midnight from Weather tab. |
| Camera switch | InterstateCorridorValidation | weather persists cockpit/exterior/cockpit | camera binding architecture | UNVERIFIED | Mirror cameras must not become Weather Maker authority. |
| Streaming | StreamingHighwayValidation | Weather Maker runtime persists, new road chunks show current road state | generated material factory checks | UNVERIFIED | Use Road Conditions tab diagnostics. |
| Floating origin | IH_50MileFloatingOriginValidation | weather/road visual state remains continuous after origin shift | origin-shift adapter hook | UNVERIFIED | Weatherade reconfigures coverage follow target. |