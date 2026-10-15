# Bounded Destruction Handoff

## Scope and verification

Owned changes: TruckTaxiBreakawayProp.cs, TruckTaxiImpactTarget.cs, and
Tests/EditMode/TruckTaxiBreakawayPropTests.cs (19 parameter-expanded cases).
Static review and scoped git diff whitespace checks only. No Unity, tests, build,
scene regeneration, vendor edits, commit, or push performed for this checkpoint.
The earlier 170/170 result predates these destruction tests.

## Projectile integration (parent-owned)

- Existing `target.Hit()` now reaches an attached breakaway adapter even when
  `target.Damaged` is already true. It applies nominal `damagePerHit` (default 4),
  with no fabricated impulse. Keep the parent's direct/blast actor deduplication.
- Optional measured overload: `target.Hit(projectileBody, contact, speed)`.
  This applies measured durability damage and a bounded prop kick. Do not also
  apply the projectile's generic scenery kick to that adapted prop when using
  this overload; keep vehicle and pedestrian impulse authorities unchanged.
- Both overloads return first-property-damage scoring eligibility, not whether
  repeated durability damage occurred. Do not prefilter calls by `Damaged`.
  Accepted hits have a configurable 0.1-2 second cooldown (default 0.5).
- Dormant soft props have trigger colliders. The inspected projectile blast
  query uses `QueryTriggerInteraction.Ignore` and therefore misses them. Include
  trigger contacts with actor filtering if blasts should affect dormant props.
  Direct projectile contact needs a filtered trigger path too; collision-only
  callbacks cannot strike these dormant triggers. Preserve the same actor dedup.
- Keep ImpactTarget and BreakawayProp on the same independent piece root.
  Never set its body nonkinematic externally: budget refusal retires it as a
  nonblocking visual. Legacy properties without an adapter retain their old
  behavior and are outside this managed budget.

## Authoring hooks (not applied to scenes/prefabs)

- Author portable restroom pieces as sibling roots under a static facility
  transform, not nested breakaway roots or children of another rigidbody.
- Set `stableId`, `partRole`, category, mass and durability before activation.
  IDs must be authored, unique by convention, and 1-128 characters for portable
  pieces. Call `ValidateAuthoring(out error)` in the parent authoring validator;
  runtime `InitializationFailureReason` explains refused soft-body setup.
- Roles: PortableRestroomShell -> Destructible; PortableRestroomDoor -> Breakaway;
  PortableRestroomFixture -> Lightweight; LoadBearing -> Structural. Explicit
  Structural, HeavyPushable and LargeVegetation categories override soft roles.
- Keep load-bearing walls/foundations and their collision on separate protected
  roots. Soft pieces require 1-16 primitive or authored convex colliders. Mixed
  nested physics ownership and nonconvex soft meshes are rejected without edits.
- Bootstrap's existing Configure(category, mass) entry point is retained.
  Its name-based assignment does not author portable piece geometry or IDs.
- `ResetTarget()` delegates to `ResetProp()` and clears scoring state. Ordinary
  retirement preserves the current pose with collision disabled; reset explicitly
  returns home. Disable releases owned physics/debris; reactivation requires an
  explicit reset if the parent wants the prop usable again.

## Bounds and limits

- Shared scenery budget: 48 total active managed bodies, including <=24 debris.
  Up to four cosmetic chunks per destroyed prop; chunks have no collision.
- Prop active lifetime defaults to 30 seconds (1-120); sleeping retirement defaults
  to 2 seconds (0.5-10). Debris defaults to 8 seconds (1-30). Collision-ignore
  pairs are bounded at 256 and restore prior states after separation/retirement.
- Protected heavy/vegetation/structure physics remains authored and is not counted
  as managed debris. NWH/UTS, pedestrians and unrelated scene bodies are not capped.
- No mesh fracture, art-authored debris, placement migration, persistent damage
  serialization, or new global gameplay manager. Parent owns persistence and
  any editor placement changes. Placeholder cosmetic chunks can fall through
  terrain until expiry; they cannot block vehicles or cause secondary damage.

## Vendor audit

- VENDOR ASSETS AUDITED: NWH VehiclePhysics2 DamageHandler, UTS CarAIController,
  Pixel Crushers DestructibleSaver, plus existing LWS collision/destruction adapters.
- VENDOR ASSETS USED: existing NWH/UTS vehicle authorities remain authoritative;
  the LWS adapter identifies impacts without replacing vendor vehicle physics.
- RELEVANT ASSETS NOT USED: Pixel Crushers DestructibleSaver for transient debris;
  persistence is parent-owned. Vendor vehicle damage is not a scenery debris budget.
- CUSTOM SYSTEMS CREATED: bounded LWS scenery budget helper, repeated-hit and
  retirement behavior, stable portable-piece classification, focused tests.
- VENDOR SOURCE MODIFIED: none.
- DUPLICATE VENDOR FUNCTIONALITY CREATED: none identified in the audited scope.
