# FUSE (working title) — Game Design Document
*GMTK Jam — Theme: Countdown*

Placeholder title ideas: **Fuse**, **Hot Potato**, **Last Pass**, **Detonation Tag**. Pick whichever sounds best out loud.

---

## 1. One-Line Pitch
A fast, mobile 1v1 where you and one enemy pass a live bomb back and forth — catch it, deflect it with junk off the ground, or run out the clock — and the fuse itself is the only referee.

## 2. Theme Tie-In
The bomb's fuse is a literal, visible, escalating countdown — big numbers or a shrinking bar on screen, audio ticking that speeds up as it nears zero. This should be the most visually dominant UI element in the game. It's also the mechanical enforcer of the whole design (see Rule 6.4) — the theme isn't decoration, it's the reason the game can't stalemate.

## 3. Core Loop
```
Bomb lands in your hand
  → hold briefly (fuse keeps ticking) or throw immediately
  → enemy catches / deflects / gets hit
  → repeat, fuse timer persists and shrinks each pass
  → someone eventually eats it
```

## 4. Player Movement (Ultrakill-flavored, NOT full Ultrakill)
**Scope deliberately reduced** — full wall-riding/momentum-chaining tech takes far longer than 2 days to feel good and isn't needed for this loop to work.

- Sprint (default fast base speed, not a toggle)
- Jump + short air control
- Dash (short burst, brief cooldown, small i-frame optional if time allows — cuttable)
- Slide (press while sprinting; mostly for feel/juice, low priority)

**Design intent:** movement is for *repositioning and dodging* — lining up a catch, ducking behind cover, closing distance to intercept a deflect — not for pure escape. The bomb should be slightly faster than player movement so running is never a complete answer (see Rule 5.3). This resolves Contradiction #1 above.

**Third attack option — herding:** because the bomb chases faster than either player can outrun it, closing distance toward your opponent while being chased is a legitimate offensive play, not just evasion — get the bomb close enough to the enemy and it retargets onto them (Section 5.4). This gives the player three distinct ways to attack: throw the bomb directly, throw an item to deflect it, or lead/herd it into the opponent using movement alone. Worth calling out explicitly in any tutorial/onboarding text, since it's the least obvious of the three.

## 5. The Bomb
### 5.1 Throw
- Aimed roughly at the enemy (screen-forward / crosshair direction), not pixel-precise — see homing below.
- One universal `Throwable` component (see Section 8) handles physics launch for the bomb AND every item.

### 5.2 Homing (mid-air only — no ground-rolling)
- While airborne, gently steer the bomb's velocity toward its current target each frame (soft steering, not a hard lock — should still feel throwable, not autoaimed).
- **Cut the "rolls over to him if it misses" ground-pathing idea.** If a throw would miss, just let the homing curve correct it in the air before it lands. If it still lands short, let it sit — the fuse keeps ticking either way, so a bad throw is still a threat, just to whoever's near it now. Simpler system, same tension.

### 5.3 Speed
- Bomb travel speed > player/enemy max move speed, always. This is what stops indefinite kiting — eventually it catches up.

### 5.4 Target-Switching (proximity retarget)
- Bomb tracks a "current target." If any entity (including the thrower) enters a small radius around the bomb, retarget to whoever's closest.
- **Must be telegraphed**: a color flash, a tone, or a UI ping the moment it retargets — this is a fairness fix for Contradiction #3. Silent retargeting will feel like a bug, not a mechanic.
- **Self-retarget bug to design around:** the instant a bomb leaves someone's hand, it's essentially standing on top of the thrower — a naive "closest entity in radius" check would retarget it back onto them before it's traveled anywhere, cancelling every throw immediately.
  - **Fix:** whoever just released the bomb is *ineligible* as a retarget candidate until they've left the retarget radius at least once. After that they're a normal candidate again, same as anyone else — which is exactly what allows the herding play (Section 4) to work: chase the bomb back toward the original thrower and it can retarget onto them again once you've both re-entered range together.
  - **Simpler fallback** if the zone-exit state ends up fiddly to debug: a flat 0.3–0.5s "just thrown, can't retarget onto the thrower yet" timer after each release. Slightly less precise at point-blank re-throws, but one line of code instead of tracked state — fine to start with and upgrade later if it doesn't feel right.
- This mechanic is the game's main skill ceiling — worth the extra care to get the exclusion logic right rather than shipping the naive version and hoping the radius is small enough to hide it.

### 5.5 Catch
- Generous input window (press interact while bomb is within a short range in front of you), not frame-perfect. Big fuse-tick audio cue helps players time it by ear/rhythm, reinforcing the countdown theme.
- On catch: fuse keeps counting from where it was — catching doesn't reset or extend it.

### 5.6 Deflect (via ground items)
- Any picked-up item can be thrown into the bomb's flight path; on collision, bomb's direction/target rebounds off the item's throw vector, back toward whoever threw the item (or wherever physics sends it — keep this simple/physical rather than another homing calc).

### 5.7 Detonation Rule (new — resolves the kiting stalemate)
When the fuse hits zero:
- If someone is holding it → they take the hit.
- If it's mid-air or on the ground → whoever's nearest takes it (or an AoE if both are close).
- **This is what forces engagement.** You cannot indefinitely avoid the bomb by refusing to interact with it — the countdown will find someone. Make this rule legible to the player early (tutorial text, or just let them find out once in a safe first encounter).

## 6. Ground Items
**Cut to 2 item types for the jam.** More items = more untested combinations = more late-night bugs. Two solid, clearly-different effects beat five half-working ones.

| Item | Effect on hit | Combo with bomb |
|---|---|---|
| Ice cream (or any "cold" item) | Briefly roots/slows target | Bomb hitting a frozen target = guaranteed hit / bonus (can't dodge or catch while rooted) |
| Oil / grease (or similar) | Extends knockback / makes target slide uncontrollably | Bomb detonating near an oiled target = larger blast radius (oil is "flammable") |

Both items reuse the same generic `Throwable` component as the bomb — only the on-hit effect differs (see Section 8).

## 7. Enemy AI (keep it a state machine, not "smart")
A fully adaptive opponent that decides catch-vs-deflect-vs-flee intelligently is a multi-day AI problem on its own. Scope it down:

- **Flee**: default state, keeps distance from bomb while bomb isn't an immediate threat.
- **Panic/Intercept**: bomb is close/incoming → attempt a catch (simple range check + input-equivalent trigger) with some chance of success, not perfect play.
- **Item-grab**: occasionally pathfinds to a nearby item and throws it at the player on a cooldown — gives the arena some offense without needing full decision-making AI.
- No need for the enemy to "understand" combos — random item usage is fine and still reads as intentional to a player.

## 8. Architecture Note (do this first, saves the most time)
Build **one** `Throwable` / `Catchable` component with:
- Pickup, hold, throw, mid-air state, collision handling
- An `OnHitEffect` hook (empty by default)

Then:
- Bomb = Throwable + fuse timer + homing + detonation logic in its `OnHitEffect`
- Ice cream = Throwable + slow/root in its `OnHitEffect`
- Oil = Throwable + knockback in its `OnHitEffect`

This is the highest-leverage engineering decision available to you — everything else (items, bomb, deflects) becomes configuration on top of one system instead of three separate ones.

## 9. Win / Lose
- Round ends when the fuse detonates on someone. That's a "point." Best of 3, or just a single round with a restart — pick based on how much time is left on Day 2 (see below); don't build a scoring/match system unless the core loop is already solid.

## 10. Feedback / Juice (cheap, high-impact, don't skip)
- Big/loud fuse countdown UI (numbers or a shrinking ring)
- Ticking sound that increases in pitch/rate as it nears zero
- Screen flash + hitstop (even 2-3 frames) on catch and on detonation
- Retarget audio/visual cue (Section 5.4)

## 11. Build Order

### Day 1 (today)
- [ ] Reduced Ultrakill-flavored controller (sprint, jump, dash)
- [ ] Generic `Throwable`/`Catchable` component (Section 8)
- [ ] Bomb: throw, mid-air homing, fuse timer + UI, detonation rule
- [ ] Greybox arena (small — big open arenas make fleeing too viable)
- [ ] Enemy: Flee state only, functional and dumb is fine for now

### Day 2 (tomorrow)
- [ ] Catch input + generous window + feedback
- [ ] Target-switch proximity logic + telegraph
- [ ] Deflect via items (item throwing reuses Throwable already)
- [ ] 2 items with effects + the one bomb combo each
- [ ] Enemy: Panic/Intercept + Item-grab states
- [ ] Juice pass (Section 10)
- [ ] **Reserve your last 2-3 hours for playtesting and bug fixing only — no new features.** This is the rule most jam teams break and regret.

## 12. If You're Ahead of Schedule (stretch, do not start early)
- Ground-rolling fallback for missed throws (only after mid-air homing feels good)
- Slide movement tech, dash i-frames
- A 3rd item type
- Best-of-3 scoring / round transitions
- Enemy uses combos intentionally (e.g. throws ice cream, then bomb)

## 13. Open Questions to Settle Before You Start Coding
- Arena size/shape — small enough that fleeing can't go on forever?
- Does holding the bomb too long (without throwing) carry any risk beyond the fuse itself, or is the fuse the only pressure? (Recommend: fuse is the only pressure — simpler, and it's the theme.)
- Single round or best-of-3 — decide based on time remaining after Day 1, not now.
