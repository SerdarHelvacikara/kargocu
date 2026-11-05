Review a small existing Unity 6000.6.7f1 courier game, read-only. The user wants Codex and Claude to collaborate directly. Codex owns integration, builds and tests. Your role is an independent review; do not modify files or implement features.

Scope: Assets/Scripts/CourierGame.cs, GameText.cs, ParcelVisual.cs, CustomerVisual.cs, CourierHUD.cs, CourierSmokeTest.cs. Read only these project files. Do not inspect credentials, account data, other projects or unrelated files. Do not run commands, open network URLs or call external services.

v0.2 Windows build succeeds. A runtime smoke test has 52 passing checks and zero errors: real collisions, three deliveries, different package fragility/acceleration, plant leaf loss, Turkish/English switching without lost state, nine localized customer reactions, bounded rewards, and non-duplicate paint purchases. Menu, gameplay and result screenshots were visually checked. No ads or real money purchases are implemented.

Look for concrete bugs those tests may miss: state changes around result/shop/menu, persistent progress, user input, localized strings, package/customer state resets, and object lifetimes. Keep the game small and offline. Do not recommend new services, frameworks, accounts or broad rewrites. The garbage truck is deliberately deferred.

Return at most 5 actionable findings, ordered by severity, with exact file/method reference and a minimal fix. Distinguish actual bugs from optional polish. If there are no important bugs, say so. End with at most 2 low-cost playtest suggestions. Reply in Turkish.
