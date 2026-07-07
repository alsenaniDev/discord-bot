# Roulette Phase 2 testing checklist

## Setup

- Run the `AddRoulettePowerUpsAndTurns` migration.
- Enable Games Hub for the guild.
- Enable Roulette for the guild/plan.
- In server owner settings, confirm power-ups are visible:
  - الدرع
  - عكس الهجمة
  - إعادة اللف

## Store

- Open the Activity store.
- Confirm the balance is shown as virtual coins only.
- Purchase a power-up with enough balance.
- Confirm the wallet decreases and owned quantity increases.
- Try purchasing without enough balance and confirm the Arabic insufficient-balance message appears.

## Room and turns

- Create a Roulette room with at least two players.
- Start the room.
- Confirm the first current turn is shown.
- Confirm only the current-turn player can spin.
- Spin once.
- Confirm no player is eliminated immediately.
- Confirm a pending target, countdown, and event log entry are shown.

## Power-ups

- As the pending target, use الدرع.
  - Confirm the target survives and turn advances.
- As the pending target, use عكس الهجمة.
  - Confirm the spinner is eliminated.
- As the pending target, use إعادة اللف.
  - Confirm a new pending target is selected where possible.
- Confirm duplicate/extra uses beyond `MaxUsesPerGame` are rejected.
- Confirm inventory quantity decreases after a successful use.

## Timeout resolution

- Spin and do not use a power-up.
- Wait until the countdown reaches zero.
- Confirm `resolve-pending-action` eliminates only the pending target.
- Confirm multiple polling clients do not eliminate twice.

## Completion

- Continue until one player remains.
- Confirm winner coins/second-place/participation rewards are awarded once.
- Confirm result publishing still follows the guild game setting.

## Regression

- Join/leave rooms before start.
- Open `/games` on desktop and mobile Discord Activity.
- Confirm Quiz gameplay is unchanged.

## Navigation and already-joined behavior

- Create a Roulette room.
- Open the store from inside the room.
- Buy a power-up.
- Click `العودة للعبة`.
  - Expected: returns to the same room.
- From the room, navigate back to the Roulette main page.
  - Expected: redirects back to the active room if the room is `Waiting` or `InProgress`.
- Refresh/reopen the Activity while in an active room.
  - Expected: returns to the active room.
- Click join on an open room where the current user is already joined.
  - Expected: opens the room with no error.
- Complete the room.
  - Expected: Roulette main page opens normally and does not redirect to the completed room.
- Cancel or expire a room.
  - Expected: Roulette main page opens normally and does not redirect to that room.
