# Current combined Windows delivery — October16,2026

Production update: tighter low-speed steering, understandable7m attach eligibility/highlight/source prompt, nearest-body rope anchor, short attach grace and previous-carrier reclaim delay, modest fresh main-body ram stealing. Shared arena remains selectable; four human cab views use actual Taxi steering/free look and Bowling per-camera visibility, preventing the owning exterior from blocking the cab. Full physical match, focused lifecycle/scoring and sustained four-view Windows sample passed. See [collection validation](../TruckSports/VALIDATION.md) for version-labelled evidence, performance limits and remaining human checks. Earlier native-stage statements below are historical.

---

# Buzkashi validation

Eight native rule fixtures cover whole cargo containment, partial entry, team mapping, opposing-contact qualification and regulation/sudden-death decisions. The scoring bounds include both physical floppy tassels, not just the root.

Latest complete six-AI five-minute PhysX match:

```
PASS six AI full five-minute match starts without human devices
OWNER 1 time=2.619998 cargo=(0.00, 1.25, -0.01)
PASS pause preserves actual tow cargo score and clock
OWNER -1 time=17.36021 cargo=(3.34, 1.49, -42.79)
DELIVERY count=1 score=1,0 time=17.36021
OWNER 1 time=19.96027 cargo=(0.00, 1.25, -0.01)
OWNER -1 time=26.86042 cargo=(0.39, 1.25, -43.24)
DELIVERY count=2 score=2,0 time=26.86042
OWNER 1 time=29.46048 cargo=(0.00, 1.25, -0.01)
OWNER -1 time=32.06054 cargo=(4.36, 1.33, -2.83)
OWNER 1 time=32.86056 cargo=(5.79, 1.26, -3.75)
OWNER -1 time=55.94109 cargo=(-52.32, 2.46, -61.95)
OWNER 1 time=58.54115 cargo=(0.00, 1.25, -0.01)
OWNER -1 time=65.621 cargo=(0.71, 1.26, -43.25)
DELIVERY count=3 score=3,0 time=65.621
OWNER 1 time=68.24056 cargo=(0.00, 1.25, -0.01)
OWNER -1 time=69.90028 cargo=(1.41, 1.33, -1.22)
OWNER 1 time=70.72015 cargo=(1.38, 1.27, -1.52)
OWNER -1 time=83.518 cargo=(-6.93, 1.28, -32.90)
OWNER 3 time=84.33786 cargo=(-6.81, 1.25, -35.62)
OWNER -1 time=112.0332 cargo=(-40.35, 1.25, -57.60)
OWNER 0 time=112.8531 cargo=(-40.21, 1.25, -57.30)
OWNER -1 time=117.6323 cargo=(-40.17, 1.27, -53.19)
OWNER 0 time=118.4521 cargo=(-39.20, 1.31, -49.91)
OWNER -1 time=120.8317 cargo=(-27.94, 1.25, -46.16)
OWNER 4 time=121.6516 cargo=(-27.13, 1.25, -47.09)
OWNER -1 time=123.0514 cargo=(-26.84, 1.30, -46.23)
OWNER 0 time=123.8712 cargo=(-26.30, 1.27, -44.24)
OWNER -1 time=126.9307 cargo=(-9.35, 1.25, -46.28)
DELIVERY count=4 score=4,0 time=126.9307
OWNER 1 time=129.5309 cargo=(0.00, 1.25, -0.01)
OWNER -1 time=131.1112 cargo=(2.51, 1.26, -0.48)
OWNER 1 time=131.9114 cargo=(5.35, 1.25, 2.10)
OWNER -1 time=134.3519 cargo=(16.14, 1.26, 0.49)
OWNER 0 time=135.1521 cargo=(16.74, 1.25, -3.14)
OWNER -1 time=177.3411 cargo=(28.17, 1.38, -18.57)
OWNER 5 time=178.1412 cargo=(29.61, 1.33, -18.80)
OWNER -1 time=179.2615 cargo=(31.40, 1.25, -18.90)
OWNER 5 time=180.0616 cargo=(34.57, 1.25, -19.43)
OWNER -1 time=206.2272 cargo=(51.37, 1.25, -18.94)
OWNER 1 time=208.8278 cargo=(0.00, 1.25, -0.01)
OWNER -1 time=214.409 cargo=(25.34, 1.29, -12.79)
OWNER 2 time=215.2092 cargo=(25.40, 1.25, -12.96)
OWNER -1 time=238.5341 cargo=(26.88, 1.25, -6.25)
OWNER 2 time=239.3343 cargo=(26.94, 1.25, -5.82)
OWNER -1 time=253.6174 cargo=(51.46, 1.25, 3.97)
OWNER 1 time=254.4175 cargo=(51.82, 1.25, 4.50)
PASS full regulation match resolves a winner with real sudden-death if tied
PASS actual whole cargo physical delivery scores once
PASS repeated legal AI tow attachments without cargo teleport
PASS real opposing impacts or bounded rope force produce legal breaks
PASS oversized readable cargo and bounded floppy bodies present
MATCH score=4,0 deliveries=4 attachments=21 breaks=14 cargoRecoveries=2 truckRecoveries=0 clock=300.0137
ERRORS=0. Native NWH/PhysX six-truck five-minute play, physical cargo constraint and complete match; no human/hardware validation.
```

Earlier full play exposed two real recovery defects: genuine lateral wall pins could persist, and cargo could perch just outside an end wall inside an overly generous recovery cutoff. Recovery now requires contact/progress evidence for in-field pins and uses the actual field edge plus an independent out-of-bounds timer. A separate focused fixture reproduces the end-wall perch. A stale render Transform also falsely broke a valid stationary tow in a controlled fixture; the physical rope limit now uses the authoritative Rigidbody anchor, and shared truck resets synchronize the render pose.

Focused acceptance evidence is recorded separately in Logs/TruckSports/TerrierUpdate/BuzFocusedEvidence/result.txt: actual hauling/release, real friendly/opposing impacts, obstructed grab rejection, one owner, whole/partial/wrong-corral scoring, duplicate lock, accessible stationary cargo, real wall trap, end-wall perch, rollover, pause/disconnect/replacement, neutral action gating, whole/shared ownership, rematch and collection return. Native camera-only captures at1280x720,1920x1080,1280x800,1024x768 and2560x1080 check both full corrals and boundaries; long-name TMP fit is asserted separately. These are not full-window screenshots.

Development flags: -tsBuzkashi and -tsBuzFocused. Automated PhysX simulation, controlled fixtures, injected devices and camera renders are distinct from human playtesting or physical hardware. Final combined Windows execution and sustained performance evidence remain pending at this milestone. Human checks: subjective tow feel, audio balance, real device ergonomics and four-human team coordination.

Final focused rerun ERRORS0 also asserts the reproduced stationary out-of-field perch recovery and all six pooled screen-aligned participant labels. Tiny duplicate world identities are disabled. Projected captions retain fixed22–24 reference-pixel TMP sizes, bounded width and overlap avoidance; full-window Windows inspection remains pending.
