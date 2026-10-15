# Prompt 022 - Next Prompt Handoff

## Completed Delivery State

The next results/rewards/economy-owning prompt receives ILwsActiveJobService with an active job that has not been cleared.

Expected semantic state:

- LwsActiveJob.status = LwsJobStatus.Completed
- LwsGameplayState = LwsGameplayState.DeliveryResults
- LwsActiveJob.deliveryCompletion.IsValid = true

The active job retains stable active-job and source-definition IDs, origin and destination, assigned trailer and type, cargo semantics, estimated distance, quoted gross pay in cents, accepted timestamp, and Prompt 021 trailer assignment/pickup data.

The compact completion snapshot provides:

- completed-successfully flag
- active job ID
- destination ID
- assigned trailer ID
- UTC completion ticks
- containment ratio
- heading error
- lateral and longitudinal offsets
- continuous valid hold duration
- delivery-context duration
- validation mode: assigned-trailer-coupled-auto-hold

## Authority Boundaries

The later results owner may calculate and present final payout, bonuses/penalties, XP, reputation, statistics, and post-job cleanup only when its numbered prompt owns those features.

It must not reconstruct LwsActiveJob, create another delivery authority, persist raw gameplay state, replace Pixel Crushers, modify NWH trailer physics directly, reinterpret another trailer as delivered, or clear the active job before results are consumed and saved.

## Prompt Plan

Prompt 023 remains the Scriptable Sheets high-volume job and destination authoring pipeline established by Prompt 020. It is not the rewards/economy owner and should not consume or clear completed jobs.

Rewards, economy, XP, reputation, statistics, results presentation, post-job trailer disposition, and final active-job cleanup remain for the appropriate later prompt.

## Resume Behavior

A save made after parking completion restores:

    LwsActiveJob.Status = Completed
            |
            v
    LwsGameplayState = DeliveryResults

The completed active job and delivery snapshot remain available after Pixel Crushers applies the semantic snapshot. No raw LwsGameplayState value is persisted.
