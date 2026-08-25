# Prompt 008 Cab Anchor Matrix

| Anchor ID | Anchor Type | Prefab Path | Cab Location | Expected Content | Occupied By Placeholder | Physics Safe | Camera Safe | Future Save ID | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `IH_CabAnchor_Dashboard01` | Dashboard accessory | `Assets/LWS/InterstateHauler/Vehicles/Prefabs/IH_PlayerTruck_NWH.prefab` runtime instance | Left dashboard surface | dashboard bobble accessory | No | Yes | Manual visual required | Yes | General dash accessory slot. |
| `IH_CabAnchor_Dashboard02` | Dashboard accessory | Same | Right dashboard surface | souvenir / mini flag / coffee cup | No | Yes | Manual visual required | Yes | Secondary dash object slot. |
| `IH_CabAnchor_GpsMount` | Dashboard accessory | Same | Center-right dashboard GPS mount | Compass Navigator Pro world-space GPS | Yes, `IH Cab GPS Compass Navigator Pro` at runtime when Compass is available | Yes | Manual visual required | Yes | Stable cockpit GPS mount. |
| `IH_CabAnchor_DashDecoration` | Dashboard accessory | Same | Left/center dashboard surface | small dashboard decoration placeholder | Yes, `IH_DashDecoration_Placeholder` | Yes | Manual visual required | Yes | Non-text development placeholder replacing the old temporary text label. |
| `IH_CabAnchor_Hanging01` | Hanging accessory | Same | Windshield/upper cab area | dice / air freshener | No | Yes | Manual visual required | Yes | No swing physics yet. |
| `IH_CabAnchor_PassengerSeat` | Passenger seat | Same | Passenger seat area | future dog companion / bag | No | Yes | Manual visual required | Yes | Dog AI/content deferred. |
| `IH_CabAnchor_Sleeper` | Sleeper | Same | Sleeper area | bedding and sleeper storage | Yes, `IH_SleeperInterior_Placeholder` when no authored sleeper art is present | Yes | Manual visual required | Yes | Sleeper-cab readability recovery. |
| `IH_CabAnchor_Memento01` | Personal memento | Same | Driver-side memento surface | photo / postcard / note | No | Yes | Manual visual required | Yes | Family-life/memento content deferred. |

Anchors are created by `LwsCabAccessoryAnchorRegistry` on the spawned LWS truck instance. Decorative and sleeper placeholders are presentation-only by default; attachments reject rigidbodies and disable child colliders to avoid affecting NWH physics or camera collision.
