# Automated coverage and execution evidence

Updated 2 November 2026. All six extended Unity suites now run on PR opened, synchronize, reopened and ready-for-review events alongside the fast Unity/backend jobs. No `SGH_RUN_EXTENDED_TESTS` opt-in is required. Manual dispatch remains a convenient way to rerun one suite. Licensing and dedicated Photon/Chat secrets are required; missing secrets fail explicitly rather than silently skipping a suite. Fork PRs do not receive secrets and need a trusted maintainer branch run.

## Coverage matrix

| Area | Automation | Evidence boundary |
|---|---|---|
| Live multiplayer | Eleven live SDK/PUN tests plus a rendered two-process harness. The rendered host creates a room, guest joins, real GameManager avatars spawn, players alternate all eleven puzzle turns, and actual Photon card ownership is acquired before placements. Both clients traverse two sessions and exit the ending independently. | Live suites and rendered cloud execution require dedicated test App IDs; local builds do not prove server behavior. |
| Disconnect/recovery | Real server master election, orphan lock repair, pending stage/insulin deadline recovery, reset revisions, same-actor rejoin and inactive clue votes. | Dedicated live execution pending. Other multipart durability and historic analytics migration remain implementation limits. |
| Full journey/repeat sessions | Real Lobby/GameScene, every configured puzzle part, rewards, keypad denial/retry/unlock, ending and a second full journey in the same process. Puzzle/score managers are scene-owned; analytics starts fresh on a normal room join and is preserved for same-actor rejoin. | Editor repeat journey passed. Windows standalone repeat journey passed. Rendered two-client cloud run is a separate job. |
| Network races | Concurrent server CAS, stale updates, reset versus pending completion, unauthorized insulin RPC, spoofed analytics sender, four-client state burst/capacity rejection and deferred card ownership. | Server transport tests do not establish atomic exactly-once completion or authoritative server-side game validation. |
| Visual and device/input | Lobby and every authored puzzle part at 16:9, 16:10, ultrawide, portrait tablet and mobile landscape viewports. PNGs plus control-containment assertions. Rendered Linux desktop probes send real OS W/mouse/Q/click input to each process and require measured movement, camera rotation, cursor release and an actual Chat Button listener change. | Viewport checks passed. Native Linux input execution awaits dedicated IDs. This is desktop input and simulated screen sizes, not Android/iOS hardware or touch certification, OCR or approved golden-image comparison. |
| Photon Chat | Two SDK clients deliver/reconnect independently. Rendered clients additionally use the production ChatManager, real scene UI, room subscriptions, hidden-panel unread count, reply delivery and Lobby channel recovery across two sessions. | Dedicated Chat application required. Mocked runner contract tests never count as real message delivery. |
| Analytics | Independently parsed synthetic CSV, exclusions, escaping, early leave, retry/idempotency, cleared per-room rows/timers/date/export latch, preservation on rejoin, and two distinct rapid-session exports. | Synthetic disk tests passed. Historic master-local history transfer and guaranteed quit-time direct SMTP remain unsupported. |
| Backend | HTTP pipeline, health/HTTPS redirect, malformed requests, exact size limit, cancellation, awaited send, limiter/config validation; actual loopback SMTP MIME success, rejection and stalled cancellation. | Twenty tests passed using local fake SMTP only; no external mailbox. Unity direct SMTP remains a separate route. |
| Performance/stability | Popup lifecycle, repeated Chat bindings, actual Lobby layout soak with retained objects/memory/frame smoke budgets, standalone journeys, repeated sessions and real four-client state bursts. | CI smoke coverage does not certify multi-hour endurance, production FPS, GPU vendors or every possible player capacity. |
| Security robustness | Seeded malformed payloads through production handlers, actual sender checks, unauthorized RPC, request limits and external SMTP TLS configuration. Dedicated IDs fence the rendered harness and reconnect fallbacks; no production fallback. | Client room-property write access still exists; this does not create a dedicated authoritative game server. |

## Execution on 2 November 2026

| Check | Result |
|---|---|
| EditMode, including new-room reset and same-actor preservation | 197 passed |
| Isolated PlayMode fixtures | 22 passed |
| Scene journey and same-process second journey | 2 passed |
| Lobby plus every puzzle page at five aspect ratios | 2 passed |
| 2,000-frame Lobby soak | Passed |
| Windows standalone normal and repeated journey | 2 passed |
| Python rendered-orchestrator contracts | 6 passed; mocks validate fail/pass and cleanup rules only |
| Backend HTTP/loopback SMTP integration | 20 passed (unchanged source from prior verification) |
| Production C# compile | Passed, existing warnings, no errors |
| Dedicated rendered automation Windows player | Build verified; cloud execution not yet performed |
| Live Photon/Chat and rendered Linux input | Authored and compiled; pending dedicated test secrets and CI execution |

The former second-session KnownGap tests are normal regressions now; they are no longer excluded to keep CI green. There are no claims of physical mobile-device execution. Local visual verification uses 256-pixel texture import caps; the PR Visual job uses the same cap to bound memory on the 8 GB hosted runner; screenshot viewport dimensions are unchanged. Native desktop input runs under an isolated X display, not against the user's desktop.

## Isolation and failures

The rendered harness exists in test assemblies and is included only by its dedicated automation build. Its minimal RenderedBootstrap scene does not change enabled production scenes. It reads dedicated IDs from a temporary ignored file outside Assets; credentials are never serialized into assets or room/player properties. SMTP credentials are cleared and rendered/scene journeys suppress CSV export. Native runner logs redact dedicated IDs before upload. Rooms/users/messages are randomized and synthetic.

Missing windows, no OS input injection, absent movement/camera/click evidence, wrong stage counts, incomplete sessions, errors, timeouts, stale result directories or failed process exits fail the rendered job. Processes and held W keys are cleaned up even on input failures. Artifacts contain synthetic logs/reports/screenshots only. See Tests/README.md for commands and workflow prerequisites.

The tests exposed and guard mixed global-slot casts, unassigned-view RPC/property writes, highlight shutdown coroutines, final trigger misassignment, CSV retry latching, stale scene managers, per-room analytics carryover and ultrawide canvas cropping. The current canvas uses Expand to keep the authored reference layout within the screen. No Photon IDs or prefab GUIDs were renumbered.
