# Shared race starting lights

The original Terrier Heat radial medallion, font assets, DOTween punch, tension sweep and3–2–1–GO GO GO cues are reused by Mattress Racing, Wife Carrying, Bog Snorkelling and Sky Tow. Cheese retains its existing matching medallion and earned-boost indicator. Turn-based and other unrelated sports receive no race overlay.

`SportsEventSession` exposes read-only countdown/start presentation values. Each race keeps its existing physics preparation, countdown timer, release, input gate and scoring clock. `TerrierCountdown` retains its original serialized Terrier reference and also accepts a common event session. It displays one correctly anchored panel per active viewport and one shared audio cue per beat. Pause hides the display and stops audio; resume does not repeat the same beat. Rematches and subsequent rounds restart the sequence. There are no new forces or input handlers.

The explicit **Truck Sports > Upgrade shared race starting lights** command adds only the missing display to the four saved scenes. Existing authored displays are preserved. Normal builds do not regenerate scenes. Existing creation defaults install the display when a supported race is first authored.

The native PlayMode regression passes1/1 at00:07:39UTC October20. It covers all five display users, real stationary authoritative bodies during countdown,3–2–1–GO, pause with unchanged timer/no repeated cue, and four-human rematches with correct per-camera anchors.720p/1080p renders were captured for every mode; Mattress/Wife/Sky Tow720p images were visually inspected. The553 existing physics blocks in the four new display scenes plus Minefield are unchanged. Receipt: `GoddessEvidence/20261020-race-countdown.txt`. These are Editor/injected-input checks, not physical controllers, human playtesting or Windows acceptance.

- VENDOR ASSETS AUDITED: Heat/uGUI, DOTween and the existing Terrier/Cheese starting presentation.
- VENDOR ASSETS USED: existing Heat radial sprites/font, DOTween and shared audio/settings.
- RELEVANT ASSETS NOT USED: a replacement countdown/UI framework; the existing display is reused directly.
- CUSTOM SYSTEMS CREATED: read-only race presentation adapters and targeted scene integration/regression checks.
- VENDOR SOURCE MODIFIED: none.
- DUPLICATE VENDOR FUNCTIONALITY CREATED: none.
