# Git divergence resolved — October20

The Pull origin badge reflected separate local/private and public Sports histories. A normal merge/push would have mixed unrelated private Truck Taxi work into the public repository. The completed Sports payload and delivery evidence are published on both approved branches.

Local main was aligned with public main using an index/pointer-only mixed reset **after preservation was verified**. No working-tree reset, deletion, force push or lost commits occurred.

## Recovery locations

- Full local history and nonignored uncommitted-work snapshot: `codex/local-preservation-2026-10-20`.
- Recovery commit: `f4675ce3d353b42157c942876412bb0731793409`; its parent is original main `2676c93cf7953ecbcdb0856c29036c072ffbddfb`.
- Exact raw-file archive and per-file SHA256 manifest: `.sports-validation/Recovery/GitReconciliation-20261020T073002Z/working-files.zip` and `raw-file-manifest.json`.
- Reconciliation receipt: [git-reconciliation.json](PresentationEvidence/git-reconciliation.json).

All32,512tracked/untracked file sizes and timestamps remained unchanged. All1,101candidate files were archived with verified hashes, and their working bytes remained unchanged. Generated/ignored build/cache files were not touched. The local recovery snapshot is intentionally **not pushed** to the public origin because it contains private/unrelated work.

To inspect the preserved history, use `git log codex/local-preservation-2026-10-20`. To recover raw files, extract the archive into a separate review folder and copy only the desired files after review. Do not force-checkout or hard-reset over ongoing work. The local branch retains every old main commit even when its content is not part of public main.

Unrelated/private files remain visible as local working changes. That is expected preservation, not outstanding Sports work. Publish them only to an explicitly appropriate private destination or after a deliberate scope review. The current public branch heads and final delivery commit are recorded in `Builds/TruckSports/GIT-DELIVERY.txt`.
