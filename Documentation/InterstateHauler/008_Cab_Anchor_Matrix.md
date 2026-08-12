# Prompt 008 Cab Anchor Matrix

| Anchor ID | Anchor Type | Prefab Path | Cab Location | Expected Content | Occupied By Placeholder | Physics Safe | Camera Safe | Future Save ID | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `IH_CabAnchor_Dashboard01` | Dashboard accessory | `Assets/LWS/InterstateHauler/Vehicles/Prefabs/IH_PlayerTruck_NWH.prefab` runtime instance | Left dashboard surface | hula girl / bobblehead | Yes, `IH_DevHulaGirl_Placeholder` | Yes | Manual visual required | Yes | Primary proof anchor. |
| `IH_CabAnchor_Dashboard02` | Dashboard accessory | Same | Right dashboard surface | souvenir / mini flag / coffee cup | No | Yes | Manual visual required | Yes | Secondary dash object slot. |
| `IH_CabAnchor_Hanging01` | Hanging accessory | Same | Windshield/upper cab area | dice / air freshener | No | Yes | Manual visual required | Yes | No swing physics yet. |
| `IH_CabAnchor_PassengerSeat` | Passenger seat | Same | Passenger seat area | future dog companion / bag | No | Yes | Manual visual required | Yes | Dog AI/content deferred. |
| `IH_CabAnchor_Sleeper` | Sleeper | Same | Sleeper area | future dog/cat companion / bedding | No | Yes | Manual visual required | Yes | Companion systems deferred. |
| `IH_CabAnchor_Memento01` | Personal memento | Same | Driver-side memento surface | photo / postcard / note | No | Yes | Manual visual required | Yes | Family-life/memento content deferred. |

Anchors are created by `LwsCabAccessoryAnchorRegistry` on the spawned LWS truck instance. Decorative accessories are presentation-only by default; attachments reject rigidbodies and disable child colliders to avoid affecting NWH physics or camera collision.
