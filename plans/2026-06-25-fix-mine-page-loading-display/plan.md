# Plan: Fix Loading Display on `/documents/mine`

## Context
Recent commit `8b7e03f feat(mine): add smooth shimmer, fade-in, and pulse-dot animations to upload job progress cards` changed `_UploadJobs.cshtml` to start progress bars at `width: 0%` and animate them to their target value via JavaScript (`requestAnimationFrame` + `setProperty`). Result: bars render at 0% on initial load and stay at 0% if the inline `<script>` in the partial does not execute (e.g. partial re-rendered via SignalR AJAX → `list.innerHTML = html` does not run injected `<script>` tags). User reports the loading display is broken; they want progress shown immediately at the real percentage.

## Approach
Revert the JS-driven 0→target animation. Render the actual progress inline. Keep the pure-CSS shimmer (background gradient) and pulse-dot animations that do not require JS — they still give a "loading" feel without the broken state.

## Files to Modify
- `GUI/Pages/Documents/_UploadJobs.cshtml` — single file change

## Implementation Steps

### 1. Remove `width: 0%` start state and JS animation
- Drop the `width: 0%;` line from the `.job-progress-bar` CSS rule.
- Restore inline `style="width: @barWidth%;"` and `aria-valuenow="@barWidth"` on the progress bar `<div>`.
- Drop `data-target="@barWidth"` from the bar (no longer needed).
- Restore the percent label to show the real value: `<span ...>@barWidth%</span>` (remove `data-target` and `0%` initial text).
- Delete the entire `<script>(function () { ... animateJobBars ... })();</script>` block at the bottom of the file.

### 2. Keep working CSS animations
- Keep `@keyframes shimmer` + `animation: shimmer 2.2s linear infinite` on `.job-progress-bar` (visual loading indicator on the gradient).
- Keep `@keyframes jobFadeIn` + `.upload-job-card` animation.
- Keep `@keyframes pulse-dot` + `.job-status-dot` animation.

## Success Criteria
- Initial GET `/documents/mine` shows each upload job bar at its actual `ProgressPercent` (no 0% flash).
- Shimmer still animates across active bars; pulse dot still pulses on `active` status.
- SignalR-driven refresh (`refreshJobs`) re-renders the partial and bars immediately show real % — no stuck-at-0 state.
- No JS errors in browser console.
- `dotnet build` succeeds.

## Risk
Low. Pure revert of the JS animation layer; only the partial file is touched. No API/DB contract changes. CSS animations retained.

## Status
✅ Completed on 2026-06-25. JS-driven `0% → target` animation reverted in `GUI/Pages/Documents/_UploadJobs.cshtml`; progress bars now render at actual `%` via inline `style="width: @barWidth%;"`. Pure-CSS shimmer, fade-in, and pulse-dot animations preserved. `dotnet build` green (0 errors). Code review approved 9/10 — no issues raised. Working tree limited to the single file `GUI/Pages/Documents/_UploadJobs.cshtml`.

**status: completed**
