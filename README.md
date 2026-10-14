# RacingCar

## Current version

Main, pause and controls menus are connected to the playable scene. Choose **First Person** or **Third Person**, then select **Start Race**. Both views support Quest head tracking; desktop third-person view follows the car, supports mouse orbit and moves forward when an obstacle blocks the camera. Desktop menu/input and camera checks passed. Real Quest controller interaction, comfort and sustained frame rate still require headset testing.

Unity project for a Quest 3/3S racing game. The playable prototype uses first-person cockpit view, a downloaded detailed concept-car model, a textured circuit with varied corners and gentle elevation, ordered checkpoints, lap timing, reset, pause and engine audio. The seated XR camera supports independent head tracking.

The visual upgrade adds an animated steering wheel, speed/gear/RPM instruments, brake lights, curbs, guardrails, pit buildings, trees and distant hills. Wheel contact and slip drive bounded skidmarks, tire smoke and grass dust; metal barrier contacts trigger sparks. The car is **Car Concept by Eric Chadwick / Darmstadt Graphics Group GmbH**, downloaded from the [Khronos asset library](https://github.com/KhronosGroup/glTF-Sample-Assets/tree/main/Models/CarConcept) under [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/). Its curved body, interior and wheels replace the procedural roadster. Materials and geometry were adapted for Unity; see `ASSET_CREDITS.md`. Scenery remains a procedural prototype. Mirrors have reflective surfaces but no rear-view cameras. Rain and visible hands are future work.

## Open the project

Use Unity Hub to add this repository folder and open it with **Unity 6000.6.0f1 (Apple Silicon)**. Let Package Manager finish resolving the checked-in manifest and lockfile.

Open **Assets/Scenes/RacingPrototype.unity** and press Play. Click inside the Game view to give it keyboard focus.

Use the mouse or arrow keys + Enter in the menu. On Quest, use the left stick up/down to select, A to confirm and B to go back. **P / Escape** or Quest **X** opens the pause menu during a race. Resume, change camera view, restart the current lap, view controls, or return to the main menu. Returning to the main menu freezes the race; Start Race begins a fresh race while preserving the best lap recorded during the session.

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

Follow the track from the starting position. Crossing the finish line completes a lap only after all seven intermediate checkpoints have been crossed in order. The gate counter in the instrument panel shows progress. Reset preserves completed laps and the best time, and restarts the current lap. The game pauses when application focus is lost; resume with P or X. RPM and gear use a simple virtual automatic drivetrain for presentation; this remains arcade vehicle physics.

Required editor modules: Android Build Support, Android SDK & NDK Tools, and OpenJDK. Unity Hub installs these for the selected editor version.

## Installed package requirements

| Package | Version |
| --- | --- |
| Universal Render Pipeline | 17.6.0 |
| Input System | 1.19.0 |
| XR Plug-in Management | 4.7.0 |
| OpenXR Plugin | 1.18.0 |
| XR Interaction Toolkit | 3.6.1 |
| Unity OpenXR: Meta | 2.6.1 |
| AR Foundation | 6.6.2 |
| Built-in Vehicles module | 1.0.0 |

## Quest configuration

The editor menu **RacingCar > Configure Quest Project** applies the initial Android configuration: OpenXR with Meta Quest support and Touch controller profiles, ARM64/IL2CPP, Vulkan, linear color, the mobile URP asset, and landscape orientation. **RacingCar > Validate Quest Setup** writes a report to `Logs/quest-setup-validation.txt`.

The prototype uses Quest controller input and a seated XR rig. The immediate milestone is first-person visual polish and device validation. Passthrough and a miniature MR track remain planned. There are no visible controller hands or a grab-to-steer wheel yet.

**RacingCar > Run Prototype Physics Checks** tests suspension, forward motion, braking, steering, reset, checkpoint order/direction, lap completion and pause/resume. Save your scene before running it. Results are written to `Logs/prototype-checks.txt`.

The editor batch entry point `RacingMenuPlayChecks.Run` tests real UI mouse selection and keyboard submit, starting/driving, both camera views, camera obstruction, pause input blocking, controls/back, lap restart, return to Home and fresh-race state. Results go to `Logs/menu-play-checks.txt`. Quest menu navigation is mapped through the native controller API and awaits hardware validation. Menu text/panels render above the cockpit so the car cannot hide buttons; the Quest menu stays in place while the player looks around.

Use **RacingCar > Build Quest APK** with the Android build target active to generate `Builds/Quest/RacingCar.apk`. This is a development build for testing. The generated APK and build caches are ignored by Git.

**RacingCar > Validate Visual Upgrade** checks road contact and checkpoint alignment. Automated runtime checks can be launched with the editor batch entry point `RacingVisualPlayChecks.Run`; results go to `Logs/visual-play-checks.txt`. The original scene and car prefab were backed up locally under `Backups/BeforeVisualUpgrade` before upgrading. See `ASSET_CREDITS.md` for the free asphalt source and `VISUAL_UPGRADE_PLAN.md` for the remaining visual milestones. No paid packages were purchased.

## Device connection

1. Sign in to Meta Quest Developer Hub with your Meta developer account.
2. Enable Developer Mode for the headset in the Meta Horizon mobile app.
3. Connect the headset with a USB-C data cable and accept USB debugging inside the headset.
4. Use Unity's Android/Meta Quest Build Profile to build and run once the XR scene is ready.

Quest Link's live headset preview requires Windows. On this Mac, use editor simulation and deploy Android builds to the headset.

Generated caches and local logs are ignored by Git. Keep Unity `.meta` files alongside their assets. See `PROJECT_PLAN.md` for the development milestones and research sources.
