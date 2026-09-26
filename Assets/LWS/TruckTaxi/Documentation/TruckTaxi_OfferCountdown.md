# Ride Offer Countdown

`TruckTaxiSession` owns the offer deadline. It snapshots `offerDuration` when a new offer is created. Remaining seconds, normalized remaining time, expiration and acceptance eligibility all derive from the same elapsed state. Changing the configuration mid-offer cannot make the visual and gameplay deadlines disagree.

`TruckTaxiOfferCountdown` is only a view. The existing Heat radial outline sprite uses a Radial360 filled Image, with the gap advancing clockwise from the top. It has a fixed 98x98 reference-pixel frame and integer seconds in the center. It changes color in the last three seconds, without a full-screen flash or repeated beep. Accept, decline and expiration close the ring with the offer panel.

Taxi's existing policy is preserved: offers continue counting down in unscaled time while their modal pauses driving. Debug Pause Offer Timer freezes the whole authoritative timer, including the ring and expiration. Presentation tools expose 3-second and 10-second test durations plus pause; diagnostics show ID, total/remaining duration, fill and notification claim.

Tests cover duration snapshotting, monotonic fill, expiry, response close, pause consistency, fresh-offer reset and one notification per offer. Runtime keyboard/gamepad and screenshot validation is recorded in the systems validation report, not inferred from these unit tests.
