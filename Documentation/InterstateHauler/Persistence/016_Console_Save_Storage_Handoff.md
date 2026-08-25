# Prompt 016 - Console Save Storage Handoff

## Current PC Storage Backend

- Authority: Pixel Crushers Save System
- Storage abstraction: `PixelCrushers.SavedGameDataStorer`
- Current Windows/Steam backend: `PixelCrushers.DiskSavedGameDataStorer`
- Current serializer: `PixelCrushers.JsonDataSerializer`
- Profile directory slot: `LwsSaveSchema.ProfileDirectoryVendorSlot` (`16000`)
- Profile manual slots: deterministic mapping from stable profile ID plus visible slot number to Pixel Crushers integer slot.

## Platform Storage Seam

Gameplay systems do not write files and do not depend on the current PC storage path. They provide semantic payloads through `ILwsSaveParticipant` and request save/load/delete through `ILwsSaveService`.

The console port should replace or configure the Pixel Crushers `SavedGameDataStorer` backend. It should not rewrite truck, transmission, navigation, weather, road condition, profile, or dialogue savers.

## Switch

Replace the Pixel Crushers storer with the approved Nintendo platform storage implementation when SDK access exists. Keep LWS semantic participants and profile slot mapping unchanged unless Nintendo certification requires slot-layout adjustments.

## PlayStation

Replace the Pixel Crushers storer with the approved PlayStation platform storage implementation when SDK access exists. Keep gameplay payloads independent from the physical storage API.

## Xbox

Replace the Pixel Crushers storer with the approved Xbox platform storage implementation when SDK access exists. Keep gameplay payloads independent from the physical storage API.

## Do Not Add

Do not invent proprietary console SDK wrappers before devkit access. Do not add platform file calls to LWS gameplay systems. Do not create a second LWS storage framework beside Pixel Crushers.
