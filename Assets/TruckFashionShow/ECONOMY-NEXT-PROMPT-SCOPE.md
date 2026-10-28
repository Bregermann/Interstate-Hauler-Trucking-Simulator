# Deferred Truck Sports economy — next prompt only

Alpha 0.7 Fashion uses the existing free catalog/customizer. This document authorizes no currency, charges, prerequisites, rewards or unlock migration now.

1. Currency architecture: define one shared reward/spend service behind the existing participant registry/save coordinator; modes submit accepted match receipts rather than mutate balances.
2. Profile wallet: attach a versioned wallet to the stable named participant ID, independently of seat, display name, input source and current outfit. Decide denominations/names only in the next scope.
3. Sport rewards: define participation/completion and winning eligibility from accepted original results. Handle ties, AI eligibility, practice and abandoned matches explicitly; balance amounts separately.
4. Inventory: store permanent owned cosmetic content IDs and unlock receipts beside the profile; preserve unknown or temporarily unavailable IDs and existing equipped selections.
5. Pricing: centralize future catalog price/availability metadata in a project-owned layer. Keep presentation previews distinct from purchase authorization. Determine real prices later.
6. Migration: back up previous schema bytes, preserve existing participants/roster/outfits/results, support idempotent version upgrades and reject future versions without data loss. Specify treatment of all previously free test items before rollout.
7. Duplicate rewards: use stable match ID + participant ID + reward kind as an idempotency key; purchases require an atomic wallet/inventory/receipt transaction and safe interrupted-save recovery.
8. UI: extend the existing Heat customizer/shop instead of creating another storefront. Add balance, ownership, price and confirmation only after the transaction API is tested. Keep free previews/cancel isolated from wallet writes.
9. Tests: duplicate receipt, ties, rematch, repeated load, profile swap/duplicate names, malformed/future data, crash boundary, insufficient funds, unavailable content, split controllers and cross-sport consistency; real vendor disk roundtrip required.
10. Staging: first schema/backups and read-only migration; then pure transaction/idempotency tests; vendor storage; opt-in sandbox rewards; inventory/purchase service; UI; selected sport integration; full regression and explicit economy activation. No live economy until that separately authorized pass.
