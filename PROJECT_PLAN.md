# RacingCar — Quest 3/3S project plan

Prepared 13 October 2026. Target confirmed: Meta Quest 3/3S.

## Progress — 15 October 2026

- Complete race mode implemented: 3–2–1–GO countdown, three-lap finish, results with splits/total time, Race Again, and device-local saved lap/race records. Current-lap reset keeps total race time; Pause > Restart Race begins a fresh countdown. Race records are separated by race distance. Play mode and persistence across editor restarts passed. Existing menu/camera, physics and driving-effect checks also passed. Updated ARM64 Quest APK built successfully with zero errors and seven warnings (71.3 MiB); archive integrity passed. Headset testing remains pending.

## Earlier progress — 14 October 2026

- Menu/third-person milestone completed: Home/Pause/Controls UI, persisted mouse/keyboard actions, Quest joystick/button mapping, switchable cockpit/chase views, chase orbit/obstacle avoidance and Quest chase telemetry are connected. Desktop input, race/menu state and camera checks passed; Quest hardware validation remains pending.
- Menu/camera Quest development APK built successfully with zero errors and seven warnings (approximately 71 MiB). Archive integrity and ARM64/VR manifest checks passed; this replaces the downloaded-car APK.

- Downloaded Khronos Car Concept (Eric Chadwick / DGG, CC BY 4.0) and integrated its curved body, interior and wheels. The procedural roadster is replaced in the playable scene; first-person camera, wheel physics, instruments and steering animation are adapted to the imported model. Attribution and original source/license files are included.

- First-person visual pass implemented: custom roadster cockpit/bodywork, animated instruments/steering wheel, desktop mouse look/seat adjustment, textured revised circuit, curbs, guardrails, scenery and contact-driven driving effects. See `VISUAL_UPGRADE_PLAN.md` for scope and remaining asset polish.
- Visual-pass physics, track-contact and runtime effect checks passed. Updated Quest APK built successfully with zero errors and seven warnings (approximately 71 MiB); actual headset testing is still deferred.

- Android Build Support, bundled SDK, NDK r27c and OpenJDK 17 are installed for Unity 6000.6.0f1.
- Meta Quest Developer Hub 6.5.2 is installed on the Mac.
- Unity packages are resolved and recorded in the manifest and lockfile; the built-in Vehicles module is also enabled.
- Quest OpenXR settings are enabled and validated with no blocking validation errors. One optional latency optimization recommendation remains.
- The generated RacingPrototype scene includes the car, circuit, desktop and seated XR cameras, keyboard/Quest input, checkpoints, lap timing, pause/reset and procedural engine audio.
- Editor physics checks passed for suspension, acceleration, braking, steering, reset, checkpoint order/direction, lap completion and pause/resume.
- Cockpit font resources are included in the project and the chase/cockpit previews were visually checked.
- Unity Play mode checks confirmed keyboard acceleration, reset, camera switching and pause. The APK was rebuilt after the cockpit font fix.
- Initial Quest development APK built successfully with zero build errors (approximately 54 MB); the newer visual-pass APK replaces it at `Builds/Quest/RacingCar.apk`. Its manifest declares VR head tracking and ARM64 code.
- Headset testing is deferred at the user's request. XR comfort, tracking alignment and sustained device frame rate still require a real Quest.
- MR passthrough and tabletop track placement remain planned work.

The starting-point notes below describe the original inspection, before setup was performed.

## Verified starting point

- Repository: https://github.com/ahrornosirov/RacingCar — public and empty; remote reports no refs. No existing code or research documents to review.
- Local workspace was empty before this plan was added.
- Computer: Apple Silicon Mac.
- Unity Hub and Unity Editor 6000.6.0f1 are installed.
- Installed editor build modules: Mac and WebGL. Android Build Support is missing.
- GitHub connector successfully accessed the repository.
- Plugin directory searches did not return a relevant Unity editor integration. An editor bridge is optional, not a project prerequisite.
- No Unity project, package installation, headset deployment, or remote commit has been performed.

## Proposed first playable

Single-player, seated VR racing: one car, one simple closed circuit, steering/throttle/brake on Quest controllers, checkpoints, lap timer, reset, pause and engine audio. Use keyboard controls in the editor for rapid gameplay iteration. Add a separate mixed-reality tabletop track over passthrough after the VR build works. This scope is a proposal; the two modes share race rules and assets, while vehicle scale and camera behavior need separate tuning.

## Software and packages

Use the installed Unity 6.6 editor for the initial compatibility check. Resolve supported, non-preview package versions through its Package Manager, record the editor version, and commit the package lockfile. If a necessary SDK does not support this editor, select a documented compatible Unity release before building gameplay.

Install through Unity Hub: Android Build Support, Android SDK & NDK Tools, and OpenJDK for the selected editor.

Start from Universal 3D (URP). Core package set:

| Package | Purpose | Stage |
| --- | --- | --- |
| Universal Render Pipeline | Mobile-friendly rendering | Initial project |
| XR Plug-in Management (`com.unity.xr.management`) | Configure XR providers | Initial project |
| OpenXR Plugin (`com.unity.xr.openxr`) | Headset and controller runtime | Initial project |
| Input System (`com.unity.inputsystem`) | Controller and keyboard input | Initial project |
| XR Interaction Toolkit (`com.unity.xr.interaction.toolkit`) | XR rig, UI and interactions | Initial project |
| Unity OpenXR: Meta (`com.unity.xr.meta-openxr`) | Quest-specific OpenXR features | Quest setup / MR |
| AR Foundation (`com.unity.xr.arfoundation`) | MR session and supported environment features through Meta provider | MR milestone |

Enable OpenXR for the Quest/Android build target, the appropriate Quest feature group and controller interaction profile, then resolve XR Project Validation issues. Set up ARM64/IL2CPP and validate Android settings against the selected SDK's documentation.

Optional later: XR Hands for hand tracking; Meta XR Core SDK and MR Utility Kit if the chosen room-understanding features require them. Evaluate these against the existing Unity XR stack before adding them. A paid car-controller asset, networking service and backend are not needed for the first prototype.

## Accounts, tools and connections

- Unity account and an activated editor license.
- GitHub connection is available; configure the local Git repository when creating the project. Track Assets (including .meta files), Packages and ProjectSettings; ignore generated Library, Temp, Logs and builds. Add Git LFS if large source assets require it.
- Meta developer account, headset Developer Mode, USB-C data cable, and acceptance of the headset's USB debugging prompt.
- Meta Quest Developer Hub for macOS is recommended for device management, deployment and logs. ADB can also deploy builds.
- On macOS, use editor simulation for iteration and Android builds on the headset for validation. Quest Link's live headset preview requires Windows.
- A Unity editor MCP bridge could later help automate scene editing, but no relevant integration was found in the plugin search. Investigate compatibility before adding any third-party bridge.

## Delivery milestones

1. **Project and device setup.** Create the URP project, configure Git, install required modules and packages, and create a Quest build profile. Exit: a basic scene launches on the headset with head tracking and controller input.
2. **Driving prototype.** Build a simple arcade car controller and track with primitive assets. Exit: accelerate, steer, brake and reset reliably in editor and on device.
3. **VR racing loop.** Add seated cockpit view, independent head tracking, recentering, checkpoints, lap timer, pause and audio. Avoid camera shake and forced head rotation. Exit: complete and restart a timed lap in the headset.
4. **Mixed reality.** Add passthrough, tabletop/floor track placement and scaling, and session reset. Exit: place and race the miniature track in a real room. Validate supported surface/anchor features on hardware before expanding room mapping.
5. **Performance and polish.** Profile on Quest 3/3S, simplify geometry and materials, bake suitable lighting, and test pause/resume and repeated laps. Initial sustained target: 72 FPS; evaluate 90 FPS after profiling. Exit: a reproducible APK and documented device test results.

## Skills needed

Unity scene/prefab authoring; C# gameplay and physics; OpenXR controller input and XR rig setup; URP mobile optimization; Android builds and device debugging; Git asset management; and basic 3D asset preparation. These are development capabilities, not paid plugins. Optional image generation can help concept art and textures; car and track geometry still require actual 3D assets or procedural geometry.

## Research sources

- Unity Quest packages: https://docs.unity.com/en-us/engine/6000.3/manual/xr/configuring-project-for/meta-quest-develop/packages
- Unity VR/MR architecture: https://docs.unity.com/en-us/engine/6000.5/manual/xr/support/vroverview
- Meta Quest Developer Hub setup: https://developers.meta.com/vr/documentation/unity/ts-mqdh-getting-started/
- Meta device setup: https://developers.meta.com/vr/documentation/unity/unity-env-device-setup/
- Quest Link platform limitation: https://developers.meta.com/vr/documentation/unity/unity-link/
- Quest profiling workflow: https://developers.meta.com/vr/documentation/unity/po-perf-opt-mobile/

Exact package versions remain to be resolved and tested with the installed editor. Source documentation alone does not establish that the project compiles or runs on the headset.
