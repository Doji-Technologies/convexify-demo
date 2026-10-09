# Release video: intent, script, and production guide

This document records how the Convexify release video (October 2026) was planned and produced: what it was meant to portray, why each shot is there, how it is rendered, and what to change next time. It is written so the same process can be reused for a new version of this video or for the store video of another asset.

- Output: `Recordings/convexify-release-4k.mp4` (3840×2160, 60 fps, 85 s, no audio) and a 1080p copy. `Recordings/` is git-ignored.
- Source: `Assets/Convexify Demo/Video/VideoDirector.cs`, `VideoOverlay.uss`, and the scene `ReleaseVideo.unity`.
- Target: the product page of [Convexify on the Unity Asset Store](https://assetstore.unity.com/packages/tools/physics/convexify-245029).

## 1. Purpose and audience

The video is the first thing many buyers watch on the store page, usually muted and often only for the first 10 to 20 seconds. It has to answer three questions in this order:

1. **Do I have this problem?** Name a pain the viewer already knows.
2. **Does this asset solve it?** Show the fix on the same example, so the before and after are directly comparable.
3. **Can I trust it and use it in my project?** Show speed, control, quality, and how little code the integration needs.

The audience is Unity developers who load or create meshes while the application runs: user-imported models, procedural geometry, downloaded content. Editor-time collider tools cannot help them, because those meshes do not exist at design time. That is the gap Convexify fills, and every shot should serve that story.

## 2. Retrospective: what to keep, what to change

**Keep:**
- The pain → fix → proof structure, with the same model (the torus) in the "before" and the "after" shot. The ball drop is the single most persuasive moment: a viewer understands it without reading any caption.
- Real UI driven by an animated pointer. It reads as a live product, not a slideshow.
- Real numbers only. The status pill shows measured decomposition times.
- Fixed-timestep 4K rendering. It is smooth regardless of how slow capture and encoding are.
- One caption at a time, centered in the free 3D area, short headline plus an optional one-line subline.

**Change next time (feedback from the publisher):** the video showcases the *demo app* more than the *package*. A buyer wants to picture the asset inside their own project. Five of the nine segments are demo UI (sliders, explode, ghost view). They show quality and speed well, but they do not show what a typical user's integration and result look like. The next version should spend more time in game-like contexts and less in the demo panel. See [section 7](#7-next-version-show-the-package-in-a-users-project).

## 3. Narrative and shot list (as shipped)

Times are approximate, from the 85 s render. Captions are quoted exactly.

| # | Time | Chapter | Headline / subline | On screen | Why it is there |
|---|------|---------|--------------------|-----------|-----------------|
| 1 | 0–4 s | (title) | **Convexify**, "Convex compound colliders for any mesh. At runtime." | Logo mark over a dark backdrop, fades to the scene | Brand and one-line promise. "At runtime" is the differentiator against editor tools. |
| 2 | 4–9 s | THE PROBLEM | "Dynamic rigidbodies need convex colliders." / "A non-kinematic Rigidbody can only use convex mesh colliders." | Torus as a single convex hull, slow orbit, no UI | States the Unity constraint the viewer has hit. No panel, so nothing distracts from the problem. |
| 3 | 9–15 s | THE PROBLEM | "A single convex hull fills every hole." / "So objects bounce off space that should be empty." | 60 balls stream onto the hull and pile up on the filled hole | Makes the pain visible and physical. Works without reading. |
| 4 | 15–20 s | CONVEXIFY | "Split any mesh into convex parts." / "V-HACD convex decomposition, computed at runtime." | Panel slides in, pointer drags Max hulls 1 → 24, hulls appear live | The fix on the same model. Introduces the product name and the algorithm credibility (V-HACD). |
| 5 | 20–26 s | CONVEXIFY | "Now physics matches the shape." / "One convex MeshCollider for each hull: a compound collider." | Same ball stream; balls fall through the hole, some rest on the ring | The payoff. Direct visual comparison with shot 3. |
| 6 | 26–40 s | REAL TIME | "Results update as you drag." / "Burst-compiled jobs. No editor bake step." | Teapot; drags of Max hulls and Voxel resolution; status pill shows ms | Speed claim, backed by the visible timings. |
| 7 | 40–49 s | INSPECT | "See how the parts divide the shape." | Explode slider, slow orbit, pointer hidden | Visual depth and polish; shows the decomposition is structured, not noise. |
| 8 | 49–57 s | ACCURACY | "Check the fit against the source mesh." / "Tune resolution, volume error and vertex limits for quality or speed." | Ghost source model on, Volume error dragged up and back | Control and trust: quality is tunable and verifiable. |
| 9 | 57–64 s | RUNTIME CONTENT | "Imported, procedural or downloaded meshes." / "Make colliders for meshes that do not exist at design time." | Clicks through Stairs, Torus, Teapot | Names the target use cases. |
| 10 | 64–71 s | SIMPLE API | "Three lines of code." | Code card: `new VHACD()`, `Compute(mesh, 0)`, `GenerateColliders(hulls, gameObject)`, note on `VHACD.Schedule` | Integration cost is tiny. Only real, compiling API. |
| 11 | 71–85 s | (end card) | **Convexify**, "Runtime convex decomposition for Unity"; pills "Burst-compiled jobs", "Pure C#, no native plugins", "Every Unity platform, even the Web"; "Available on the Unity Asset Store"; "Try the live demo: doji-tech.com/convexify-demo" | Logo, feature pills, call to action, fade out | Summary of the three strongest differentiators and the next step for the viewer. |

### Claims policy

Every statement in the video must be true for the shipped package and checkable in the code or a measurement:

- Wording comes from the package `README.md`, `VISION.md`, `CHANGELOG.md` and the store description, not from marketing guesses.
- No speed multipliers ("10× faster") unless a recorded benchmark backs them. The video shows measured times instead.
- No outdated limitations or features. For example, the old store text says asynchronous computation is not supported on WebGL; that is no longer true, so the video neither repeats it nor claims background threads on the Web.
- Code on screen must compile against the current API.

## 4. Visual and pacing rules

- **Resolution and UI scale.** Render at 3840×2160. Both UI layers (demo panel and video overlay) use `ScaleWithScreenSize` with a 1920×1080 reference, so the layout matches a 1080p screen and every pixel is doubled. Text stays sharp in the 4K master and in the downscaled 1080p copy.
- **Fixed timestep.** `Time.captureFramerate = 60` and `Time.fixedDeltaTime = 1/60`. Every rendered frame is exactly 1/60 s of video time, so the video is smooth no matter how long each frame takes to capture and encode. Physics is stepped once per frame and stays deterministic (`Random.InitState(7)` for the ball positions).
- **Real computation speed.** The decomposition still runs at its real speed on worker threads, so a slow run spans more video frames. The status pill shows the real milliseconds. Do not fake numbers.
- **Captions.** One at a time. Chapter label (small, letter-spaced, accent color), headline (54 px at 1080p reference), optional subline. They are centered in the screen area to the right of the panel. The model is shifted up (`DemoApp.ExtraScreenShift = (0, 0.2)`) so it never sits under the caption. Captions fade and slide in over 0.55 s and fade out over 0.3 s.
- **Reading time.** Hold a caption for at least about 1 s plus 0.3 s per word. Anything shorter will not be read by muted viewers.
- **Pointer.** A white ring with an arc path and ease-in-out motion, a short press animation, and a fade when idle. It moves to the real element positions (`worldBound`), so it always lines up with the UI. Hide it during passive shots (auto-orbit).
- **Motion.** Ease-in-out cubic for all UI and slider motion, ease-out cubic for entries. A slow constant auto-orbit (10°/s, 22°/s during the explode shot) keeps the 3D view alive between actions.
- **Hint text.** When the pointer acts on a control, the panel hint shows that control's explanation. This mirrors the hover behavior of the real demo.
- **Cards.** Title, code and end cards are full-screen overlays on a near-black backdrop (`rgb(11, 12, 15)`), crossfaded with the scene.

## 5. Production pipeline

### Architecture

`VideoDirector` (Editor-only, wrapped in `#if UNITY_EDITOR`, so it is not part of the Web build) runs inside the real demo in Play mode:

- It drives the demo through a small scripting API on `DemoApp` (`LoadSample`, `Decomposition`, `HullObject`, `Orbit`, `ModelBounds`, `IsLoading`, `ExtraScreenShift`), `OrbitCamera` (`SetView`, `AddYaw`, `Frame(..., keepAngles)`), and `ParamSlider.SetValue` (moves the slider like a user, so live updates fire).
- It adds a second `UIDocument` with sorting order 10 for the overlay (captions, cards, pointer), styled by `VideoOverlay.uss`.
- `Script()` is the whole video as one coroutine built from small blocks: `Caption`, `HideCaption`, `ShowCard`, `DragSlider`, `ClickChip`, `ClickSwitch`, `WaitForHulls`, `MakeColliders`, `DropBalls`, `Wait`.
- `CaptureFrames()` grabs each frame at end of frame with `ScreenCapture.CaptureScreenshotAsTexture` and writes the raw RGBA bytes to the standard input of an `ffmpeg` process: `libx264 -preset slow -crf 14 -pix_fmt yuv420p -vf vflip -movflags +faststart`. No frames are written to disk.
- The physics shots call `new VHACD().GenerateColliders(result, hullObject)`, so the balls collide with exactly the hulls on screen, using the package's own public API.

### Steps to record

1. Make sure `ffmpeg` is on `PATH` (or set `VideoDirector.FFmpeg`).
2. Open `Assets/Convexify Demo/Video/ReleaseVideo.unity`.
3. Set the Game view to the output size, for example with `UnityEditor.PlayModeWindow.SetCustomRenderingResolution(3840, 2160, "Video")`.
4. Set `OutputPath` and `Crf` on the `Video Director` object if needed.
5. Enter Play mode. Keep the Editor focused, because an unfocused Editor can stop updating. The director exits Play mode when the video is complete.
6. Make a 1080p copy: `ffmpeg -i convexify-release-4k.mp4 -vf scale=1920:1080:flags=lanczos -c:v libx264 -preset slow -crf 18 -pix_fmt yuv420p -movflags +faststart convexify-release-1080p.mp4`.

Render time on the development machine: about 2.5 minutes at 1080p and 13 minutes at 4K for 85 s of video.

### Review loop

Iterate at 1080p (`Crf` 20) and only render 4K at the end. Review with contact sheets instead of watching the whole video each time:

```bash
ffmpeg -i preview.mp4 -vf "fps=1/2.5,scale=480:-1,tile=4x5" -frames:v 1 sheet.png
```

For a specific moment, extract a short range at a higher rate (for example `-ss 8 -t 16 -vf fps=1,...`). Two review passes caught these problems before the final render:

- The first ball drop was over in about 1.3 s, followed by 4 s with nothing happening. It became a 3.2 s stream of 60 balls.
- Tall models (the stairs) overlapped the captions. Fixed with the upward screen shift.
- The pointer was left sitting in the frame during the auto-orbit. It now fades out and comes back when it is needed.

## 6. Reusing this for another asset

Checklist:

1. **Find the pain.** One sentence a buyer would say about their own project. Show it before the product appears, without UI.
2. **Pick one hero example** that shows the before and after in the same frame and is understandable without text (here, balls through a hole).
3. **List the differentiators** from the asset's own docs (here: runtime, every platform including Web, Burst speed, small API). Each one gets one shot, and every claim follows the claims policy.
4. **Write the shot table first** (time, caption, on-screen action, reason) and get it approved before writing code.
5. **Build a director** in the asset's demo or sample scene: a coroutine script, an overlay `UIDocument`, a pointer, a fixed timestep, and ffmpeg piping. `VideoDirector.cs` can be copied; only `Script()`, the cards and the scripting hooks are specific to Convexify.
6. **Iterate at 1080p with contact sheets**, then render the 4K master.
7. **End with the call to action**: store availability and a link to a live demo or documentation.

## 7. Next version: show the package in a user's project

The current video proves the algorithm. The next one should show the value in a buyer's project: how they would integrate the package and what their players would experience. Suggested changes, keeping the polish and rules above:

- **Lead with a game context instead of a bare model.** For example, a small physics playground in which a user drops their own model into a level at runtime, it gets colliders immediately, and it then rolls, stacks, and collides with other objects. The torus-and-balls comparison can stay as the hero shot, but set in that scene.
- **Show the integration in the Editor.** A short sequence of the actual workflow: import the package, add a script with the three lines to a GameObject, press Play, see the colliders (Physics Debugger or collider gizmos). This answers "what do I have to do" better than a code card alone. Use real, compiling code.
- **Show real use cases instead of naming them.**
  - User-generated content: a model is dragged into a running build and becomes a physics object.
  - Procedural geometry: generated rocks or terrain chunks get colliders as they spawn.
  - Downloaded content: an asset arrives at runtime and gets colliders.
- **Show the frame rate staying smooth.** A frame-time graph while `VHACD.Schedule` runs in the background, compared with a blocking call, demonstrates "Burst jobs, keep your frame rate" better than a caption.
- **Show platforms.** A short montage of the same scene in a browser tab (Web build), on a phone, and on desktop supports "every Unity platform, even the Web" with evidence.
- **Shorten the demo-UI segments.** Keep one compact segment that shows tuning (resolution or Max hulls drag with live hulls). The explode and ghost views can be a single combined shot.

Possible structure for about 75 s: pain in a game scene (10 s) → fix and hero comparison (12 s) → integration in the Editor (15 s) → three runtime use cases (18 s) → smooth frame rate and platforms (10 s) → tuning (5 s) → end card (5 s).
