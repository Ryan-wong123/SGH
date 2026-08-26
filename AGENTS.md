# AGENTS.md

## Repository purpose

This is the SGH cooperative educational puzzle game. It is a Unity 6 URP project using Photon PUN/Realtime and Photon Chat, with an optional ASP.NET Core CSV-email service in `backend/SghCsvEmailApi`.

Read [PRD.md](PRD.md) for expected game behavior and [architecture.md](architecture.md) for runtime ownership, state, and integration boundaries before changing multiplayer flow.

## Working conventions

- Make project code changes in `Assets/Scripts`; treat `Assets/Photon`, `Assets/LeanTween`, and `Assets/TextMesh Pro` as third-party code unless a task explicitly requires a vendor change.
- Unity scene and prefab YAML are source-controlled assets. Preserve serialization, object references, and `.meta` files. Do not hand-edit serialized YAML unless the task genuinely requires it and the exact target has been inspected.
- Match the surrounding style in files you touch. Existing scripts use explicit null checks and extensive inline comments; avoid reformatting unrelated code.
- Keep a change narrowly scoped. A scene/prefab assignment often matters as much as the C# code.
- Do not modify generated folders or files: `Library/`, `Temp/`, `Logs/`, `obj/`, `.vs/`, generated DLLs/PDBs, or Unity-generated build output.
- Do not discard or overwrite existing user changes. Inspect `git status` and `git diff` before and after work.

## Build and verification

Run from the repository root on Windows PowerShell:

```powershell
dotnet build Assembly-CSharp.csproj --no-restore
```

The compile can update tracked `obj/` artifacts. Restore only those generated artifacts after a successful build:

```powershell
git restore -- obj
```

For changes that affect a puzzle, interactable, clue UI, room flow, or Photon state, compilation is necessary but not sufficient. Verify in Unity Play Mode with the affected scene. For shared-state changes, use two clients in the same room.

Minimum multiplayer regression checks:

1. Create/join/start a room and confirm all clients load `GameScene`.
2. Verify a non-master player can interact and a master-client switch does not block progress.
3. Verify a temporary disconnect does not leave the puzzle lock, delayed stage advance, or clue close gate stuck.
4. Verify active players see the same puzzle result/stage.
5. Verify a regular clue after a slideshow has a visible Close button and retains its waiting-player message after a vote.

## Photon rules

- `PuzzleStateManager` is the shared puzzle-flow authority. Register a new puzzle's panel, `IPuzzle` manager, reset behavior, stage completion, clue reward, and story configuration there and in `GameScene`.
- Do not use a local coroutine as the only source of truth for a stage transition. Use Photon room properties for recovery-critical state; the current pending-transition contract is `PendingAdvanceStage` plus `PendingAdvanceAt`.
- Only the master should decide globally shared transitions, timer starts, lock repair, and final CSV generation. Other clients request/synchronize rather than independently advancing state.
- Treat `Player.IsInactive` as absent when determining required puzzle turns and close votes. A disconnected player must not block the remaining team.
- Preserve Photon event code assignments. Gameplay CSV logging uses `41`; clue waiting-count updates use `42`.
- Use buffered RPCs only for effects late joiners need. Avoid buffering transient clicks, animation states, or repeated UI updates.
- Do not reuse a room/property key for unrelated systems. Namespace new properties and document their writer/reader/recovery behavior.

## Clue and UI rules

- `ClueUI` and `SlideShowUI` share a physical Close button under the `Clue` root in `GameScene`.
- A regular clue must call the standard close visibility path and must deactivate active slideshow controllers before taking the shared button. Never add independent handlers that let both controllers respond to one click.
- The multiplayer close gate must leave the Close button visible after a local vote, update its remaining-player text, and close only once all active players have voted.
- Check all new/changed screen-space UI at multiple aspect ratios. In particular, test 16:9, 16:10, ultrawide, tablet portrait, and mobile landscape; do not assume the reference resolution is the player's resolution.
- Lobby room entries are instantiated under `contentPanel`. Ensure the `Lobby` content object has an appropriate layout component or explicitly position entries; otherwise clones overlap and only the last rendered room appears selectable.

## Security and data rules

- Never commit real SMTP passwords, access tokens, API keys, or personal recipient addresses. Use local/user secrets or deployed environment variables.
- Never place credentials in Photon room/player custom properties, Unity `Resources`, ScriptableObject assets, scenes, prefabs, logs, or documentation.
- Treat gameplay CSV output as sensitive. Do not log its contents or transmit it to an unverified endpoint.
- The Unity direct-SMTP path and the .NET email API are currently separate. Do not extend both paths for a feature without explicitly deciding which production route is authoritative.

## Backend rules

- `backend/SghCsvEmailApi` targets .NET 10 and exposes `GET /health` and `POST /api/gameplay-csv`.
- If changing the API contract, update the Unity caller in the same change only after confirming that Unity has a real HTTP caller. `SmtpEmailSettings` currently validates backend configuration but is not used by `GameSessionCsvLogger`.
- Keep request limits, file-size validation, and HTTPS requirements intact or strengthen them.

## Before handing off

- Run `git diff --check`.
- Run the relevant compile/test command.
- Confirm `git status --short` contains only intentional source/asset/documentation changes.
- State what was tested and what requires Unity/two-client manual verification.
