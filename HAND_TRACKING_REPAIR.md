# Hand tracking repair: step 1

Status: isolated Unity sample test prepared. Physical Quest acceptance is pending.

Install `Builds/Quest/RacingCar-Hands-Check.apk`. It appears as **RacingCar Hands Check**, package `com.ahrornosirov.racingcar.handscheck`, alongside the racing app.

The test scene is `Assets/Scenes/QuestHandsBaseline.unity`. It uses the official XR Interaction Toolkit 3.6.1 Hands Interaction Demo rig, Starter Assets and XR Hands 1.7.3 HandVisualizer. Our racing hand input, custom hand renderer, menu-ray adapter and vehicle physics are absent. `QuestHandsBaseline` only places the status panel, reads provider status and counts button callbacks.

## Headset test

1. Launch RacingCar Hands Check with hand tracking enabled.
2. Put the controllers down and raise both hands in front of the headset.
3. Check whether both hands appear and whether LEFT and RIGHT say TRACKED.
4. Point at BUTTON A and pinch thumb/index five times, releasing each time; repeat with BUTTON B. Poke also uses the official rig.
5. Open and close the Quest system menu; repeat hand detection and a button click.

Report: hands visible yes/no; exact status text; button A/B counters; whether returning from the Quest menu changes anything.

Acceptance: both hands visible, both counters reach five using actual hand input, and tracking/UI recover after the system menu. These are hardware checks; a successful APK build does not establish them.

## Local checks

- `QuestHandsBaselineSetup.ImportSamples`: imports the official samples through Unity Package Manager's Sample API.
- `QuestHandsBaselineSetup.CreateScene`: creates the isolated test scene, preserving Unity's rig/input setup.
- `QuestHandsBaselineChecks.Run`: checks missing scripts, official XR rig/UI presence, unpaused startup, button callback wiring and panel text fit; renders `Previews/quest-hands-baseline.png`.
- `QuestHandsBaselineSetup.Build`: builds only this scene under a separate Android application ID, then restores the racing app's product name and ID.

Logs: `Logs/hands-baseline-validation.txt`, `Logs/hands-baseline-play-checks.txt`, `Logs/hands-baseline-build-summary.txt`.

The built-in Video module was enabled because the imported official sample includes video-related scripts. Test-room scene objects do not use video.

## Next steps

After hardware acceptance, integrate the official hand/UI setup into the racing menus (step 2), connect the Porsche wheel (step 3), repair grip/pedal/UI interactions (step 4), and verify the complete headset flow (step 5). Do not claim these steps complete before their headset checks pass.
