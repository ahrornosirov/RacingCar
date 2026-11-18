# RacingCar

## Current version

Complete three-lap races: Start Race shows **3–2–1–GO**, then starts the lap and total race clocks. After lap three, the results screen shows each lap, total time, saved personal bests, and **Race Again / Main Menu**. Best lap and best race times are saved locally on each device.

Main, pause and controls menus are connected to the playable scene. Choose **First Person** or **Third Person**, then select **Start Race**. Both views support Quest head tracking; desktop third-person view follows the car, supports mouse orbit and moves forward when an obstacle blocks the camera. Desktop menu/input and camera checks passed. Real Quest controller interaction, comfort and sustained frame rate still require headset testing.

Unity project for a Quest 3/3S racing game. The playable prototype uses first-person cockpit view, a downloaded detailed concept-car model, a textured circuit with varied corners and gentle elevation, ordered checkpoints, lap timing, reset, pause and engine audio. The seated XR camera supports independent head tracking.

The visual upgrade adds an animated steering wheel, speed/gear/RPM instruments, brake lights, curbs, guardrails, pit buildings, trees and distant hills. Wheel contact and slip drive bounded skidmarks, tire smoke and grass dust; metal barrier contacts trigger sparks. The car is **Car Concept by Eric Chadwick / Darmstadt Graphics Group GmbH**, downloaded from the [Khronos asset library](https://github.com/KhronosGroup/glTF-Sample-Assets/tree/main/Models/CarConcept) under [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/). Its curved body, interior and wheels replace the procedural roadster. Materials and geometry were adapted for Unity; see `ASSET_CREDITS.md`. Scenery remains a procedural prototype. Mirrors have reflective surfaces but no rear-view cameras. Rain remains future work. The Porsche now has an optional hand-steering prototype with rigged XR hand models; headset validation is pending.

## Quest hand-steering prototype

Use `Builds/Quest/RacingCar-Hands-Test3.apk` for the full game. Look for **HANDS TEST 3** in the Quest menu. Both hand models share the headset tracking space and keep their last valid pose for up to 150 ms. The menu footer reports tracking for each hand. Physical headset confirmation is pending.

Choose **Garage > Porsche 911 Turbo S**, then **Controls > Input: Controllers / Switch to Hands**. Return and start the race. Hand mode uses first person. The setting persists, but controllers remain the default on a new installation. Hand rays can operate menus in either mode: point an open hand at a button, hold briefly, then pinch thumb and index to select. Release before the next click.

- Open your hands near the steering-wheel rim, then curl the **middle and ring fingers** to grab. Turn one or both hands; the thumb/index remain free for pedals.
- **Right thumb-index pinch accelerates**; **left thumb-index pinch brakes**, with braking taking priority. Pinch strength controls demand. Releasing the accelerator pinch coasts while a grip remains.
- Both hands must be tracked for pedals. Losing either hand, invoking a system gesture, or releasing both grips cuts power and brakes. Open/regrab lost grips and release both pinches before accelerating again. Pause, reset, recenter and focus loss also disarm input.
- The dashboard has **D / R / Recenter / Pause**. Open the interacting hand to point/pinch; a curled wheel hand's pinch operates its pedal, never a UI button. Stop below 1.5 km/h before changing gear or recentering. Pause exposes Restart Race and Main Menu.
- Test limits are **60 km/h forward / 12 km/h reverse**, with gradual acceleration and a maximum 110-degree wheel turn each way. Controller mode retains the vehicle's original speed/handling profile.
- Other cars require Controller mode for now. Hand mode cannot start an unsupported car. Desktop keyboard input still works without an active headset.

The implementation uses XR Hands 1.7.3, OpenXR Hand Tracking Subsystem and Meta Hand Tracking Aim, following [Unity's setup documentation](https://docs.unity3d.com/Packages/com.unity.xr.hands@1.7/manual/project-setup/install-xrhands.html). No Meta All-in-One SDK is needed. `QuestHandSetup.Apply` installs scene references and enables the Android features; it is already applied. Android manifests declare hand tracking optional so controllers continue to work.

Validation: `QuestHandChecks.Run` tests two-hand angular wrap/regrabbing, pinch/brake precedence, tracking-loss recovery, limits, world-space UI hit testing and actual Porsche WheelCollider driving/braking. `QuestHandPlayChecks.Run` tests scene/menu bindings, switching modes/cars, cockpit view, gear interlock and restores preferences after testing. Logs are in `Logs/hand-steering-checks.txt` and `Logs/hand-play-checks.txt`. The ray adapter and real finger gestures still need Quest 3/3S hardware validation; desktop tests cannot verify camera occlusion, comfort or frame rate. Use seated hand tracking with hands visible to the headset. There is no physical wheel resistance.

## Project presentation

[Download the project presentation](docs/presentation/RacingCar_Project_Presentation.pptx). The editable PowerPoint contains 13 slides with project screenshots, implementation details, testing status, asset credits, and speaker notes for a classroom presentation.

## Open the project

Use Unity Hub to add this repository folder and open it with **Unity 6000.6.0f1 (Apple Silicon)**. Let Package Manager finish resolving the checked-in manifest and lockfile.

Open **Assets/Scenes/RacingPrototype.unity** and press Play. Click inside the Game view to give it keyboard focus.

Use the mouse or arrow keys + Enter in the menu. On Quest, use the left stick up/down to select, A to confirm and B to go back. **P / Escape** or Quest **X** opens the pause menu during a race. Resume, change camera view, restart the whole race with a fresh countdown, view controls, or return to the main menu. Returning to the main menu freezes the race; Start Race begins a fresh three-lap race while preserving saved records. The countdown can be paused and resumed.

| Action | Desktop | Quest controllers |
| --- | --- | --- |
| Accelerate | W / Up arrow | Right trigger |
| Brake | S / Down arrow | Left trigger |
| Steer | A/D / Left/Right arrows | Left thumbstick left/right |
| Reverse | Q | Left thumbstick down |
| Reset current lap and car | R | B |
| Pause/resume | P / Escape | X |
| Camera view | C switches first/third person | Y |
| Look around | Hold right mouse and move | Move your head |
| Seat height | Page Up / Page Down | Adjust seated position before recentering |
| Recenter desktop look | Home | A |
| Handbrake | Space | — |

Follow the track from the starting position. Crossing the finish line completes a lap only after all seven intermediate checkpoints have been crossed in order. The gate counter in the instrument panel shows progress. R / Quest B resets only the current lap and preserves completed laps. Total race time keeps running through lap resets; the results screen reports how many resets occurred. **Restart Race** in the pause menu clears all lap progress and begins a new countdown. Neither action deletes saved records. The game pauses when application focus is lost; resume with P or X. RPM and gear use a simple virtual automatic drivetrain for presentation; this remains arcade vehicle physics.

Required editor modules: Android Build Support, Android SDK & NDK Tools, and OpenJDK. Unity Hub installs these for the selected editor version.

## Installed package requirements

| Package | Version |
| --- | --- |
| Universal Render Pipeline | 17.6.0 |
| Input System | 1.19.0 |
| XR Plug-in Management | 4.7.0 |
| OpenXR Plugin | 1.18.0 |
| XR Interaction Toolkit | 3.6.1 |
| XR Hands | 1.7.3 |
| Unity OpenXR: Meta | 2.6.1 |
| AR Foundation | 6.6.2 |
| Built-in Vehicles module | 1.0.0 |
| Built-in Terrain / Terrain Physics modules | 1.0.0 |

## Quest configuration

The editor menu **RacingCar > Configure Quest Project** applies the initial Android configuration: OpenXR with Meta Quest support and Touch controller profiles, ARM64/IL2CPP, Vulkan, linear color, the mobile URP asset, and landscape orientation. **RacingCar > Validate Quest Setup** writes a report to `Logs/quest-setup-validation.txt`.

The prototype uses Quest controller input and a seated XR rig. The immediate milestone is first-person visual polish and device validation. Passthrough and a miniature MR track remain planned. Optional Porsche hand steering is described below. Controller mode retains the existing joystick/trigger controls.

**RacingCar > Run Prototype Physics Checks** tests suspension, forward motion, braking, steering, reset, checkpoint order/direction, lap completion and pause/resume. Save your scene before running it. Results are written to `Logs/prototype-checks.txt`.

The editor batch entry point `RacingMenuPlayChecks.Run` tests real UI mouse selection and keyboard submit, starting/driving, both camera views, camera obstruction, pause input blocking, controls/back, whole-race restart, return to Home and fresh-race state. Results go to `Logs/menu-play-checks.txt`. Quest menu navigation is mapped through the native controller API and awaits hardware validation. Menu text/panels render above the cockpit so the car cannot hide buttons; the Quest menu stays in place while the player looks around.

The batch entry point `RacingRaceModePlayChecks.Run` checks countdown input/timer blocking, countdown pause, GO, ordered three-lap completion, results, Race Again, reset accounting and record saving. `RacingRaceModePlayChecks.VerifySavedRecords` verifies saved records in a second editor process and cleans up its isolated test records. Results go to `Logs/race-mode-play-checks.txt`; tests do not overwrite player records.

Use **RacingCar > Build Quest APK** with the Android build target active to generate `Builds/Quest/RacingCar.apk`. This is a development build for testing. The generated APK and build caches are ignored by Git.

**RacingCar > Validate Visual Upgrade** checks road contact and checkpoint alignment. Automated runtime checks can be launched with the editor batch entry point `RacingVisualPlayChecks.Run`; results go to `Logs/visual-play-checks.txt`. The original scene and car prefab were backed up locally under `Backups/BeforeVisualUpgrade` before upgrading. See `ASSET_CREDITS.md` for the free asphalt source and `VISUAL_UPGRADE_PLAN.md` for the remaining visual milestones. No paid packages were purchased.

## Device connection

1. Sign in to Meta Quest Developer Hub with your Meta developer account.
2. Enable Developer Mode for the headset in the Meta Horizon mobile app.
3. Connect the headset with a USB-C data cable and accept USB debugging inside the headset.
4. Use Unity's Android/Meta Quest Build Profile to build and run once the XR scene is ready.

Quest Link's live headset preview requires Windows. On this Mac, use editor simulation and deploy Android builds to the headset.

Generated caches and local logs are ignored by Git. Keep Unity `.meta` files alongside their assets. See `PROJECT_PLAN.md` for the development milestones and research sources.

## Garage development

The main menu now includes Garage with exterior/cockpit previews and saved vehicle selection. The concept roadster, Toyota Corolla E180, Ford Mustang ROUSH 2019 and Porsche 911 Turbo S are selectable. Each registered vehicle can have its own drivetrain, acceleration curve, brakes, steering, grip, suspension and camera positions. Cars can only be changed before starting a race; records are stored per car. See [GARAGE_PLAN.md](GARAGE_PLAN.md) for the current integration status and checks.

### Porsche steering experiment

The Porsche now uses gentler steering on keyboard and Quest. Full low-speed turning lock is retained, but available lock falls with actual road speed (about 4.5 degrees at 100 km/h). Key presses build up gradually, releasing returns the wheels to center, and changing direction unwinds before steering the other way. Quest steering adds a 12% horizontal dead zone and a gentle response near center; full stick still reaches the available lock. The cockpit steering wheel follows the applied steering. Acceleration, brakes, grip and the other cars' handling are unchanged.

For a comparison in Unity, select `Assets/Vehicles/Porsche.asset` and toggle **Assisted Steering** in the Inspector. Enabled is the new tuning; disabled uses the previous steering and raw Quest stick response. Asset edits can persist after leaving Play mode, so leave the checkbox enabled to retain the new default. Test small A/D taps at 60–100 km/h, full steering at parking speeds, release to center and direction changes in both camera views. Headset feel still needs a real Quest test.

`RacingSteeringChecks.RunAll` runs actual WheelCollider comparisons of the old/new 100 km/h steering response, low-speed turns, return-to-center and reset checks, input shaping checks, then acceleration/braking/reverse regression checks for all four cars. Reports are written to `Logs/steering-checks.txt` and `Logs/vehicle-physics-checks.txt`.

### Map selection

Choose **Select Map** from Home, browse with **Previous Map / Next Map**, then choose **Use This Map**. Back cancels an unconfirmed change. The choice is saved locally. Map changes are available before a race; returning Home allows another selection.

- **Circuit 01:** the original circuit, with eight checkpoints and its existing records.
- **Mountain Race Tracks:** the downloaded CRAFTED DIGITALS environment, with a 2.6 km road loop, snowy terrain, barriers, elevation changes, a checkered start/finish line and 16 ordered checkpoints. All four cars and both camera views work on either map. Snow is visual scenery; tire friction uses our existing asphalt handling.

Each map has its own spawn, gates and records per car. Map changes are blocked during a race, including while paused. Only the selected mountain environment is instantiated; the original circuit is disabled while it is selected. Terrain uses URP rendering, instancing and a reduced distant detail budget. Real Quest performance and comfort still require headset testing.

The mountain source is licensed under the Standard Unity Asset Store EULA. Downloaded source and derived terrain/material files are ignored by Git. On another computer, acquire [Mountain race tracks](https://assetstore.unity.com/packages/3d/environments/landscapes/mountain-race-tracks-110408) with the licensed Unity account, import it through My Assets, and run `MountainTrackSetup.Apply` to rebuild its integration. See `ASSET_CREDITS.md`.

`RacingTrackPlayChecks.Run` tests map selection/cancel, saved choice, map/car record scope, driving, race/pause locks and spawn reset with a temporary test surface. `MountainTrackPlayChecks.Run` checks the actual downloaded map, materials, gates, road contact, asphalt effects identification, both views, reset and switching back to Circuit 01. Reports are in `Logs/track-play-checks.txt` and `Logs/mountain-play-checks.txt`. The setup also raycasts the road along its full loop to check collision coverage.
