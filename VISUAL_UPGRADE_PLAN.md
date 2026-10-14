# First-person racing visual upgrade

Research date: 14 October 2026. Target: standalone Meta Quest 3/3S, Unity 6000.6, URP.

## Implementation progress

Main, pause and controls menus now provide first-person/third-person selection, Start/Resume, whole-race restart and return to Home. Desktop chase view supports mouse orbit and camera obstruction handling. The menu blocks driving and freezes time. Quest controller navigation and an independently tracked chase view/HUD are implemented but await headset validation. UI mouse/keyboard, camera and race/menu transition checks passed, and menu/controls/pause/chase previews were visually inspected.

The updated menu/camera ARM64 Quest APK built successfully with zero errors and seven warnings, approximately 71 MiB. Its archive integrity and VR manifest were checked. Existing acceleration, steering, pause/reset and driving-effect regression checks also passed.

The custom roadster has now been replaced with the downloaded **Car Concept** model by Eric Chadwick / Darmstadt Graphics Group GmbH, from Khronos's glTF Sample Assets library (CC BY 4.0). Its interior, body and wheels are integrated with the existing physics and first-person rig. Original source/license files are included. Unity materials, live instruments, animated steering, wheel poses and dashboard credit are adapted for this project.

The imported model passed the physics/race-rule, road/checkpoint and runtime driving/effect checks. Its Quest APK built successfully with zero errors and seven warnings; archive integrity passed. The cockpit and exterior were rendered from the actual Unity scene and inspected. Hardware performance/comfort validation remains pending.

The first visual pass includes default desktop cockpit view with mouse look, height adjustment and recentering; animated steering wheel and instruments; free CC0 asphalt color/normal/smoothness maps; a revised circuit with gentle elevation, curbs and guardrails; pit buildings, signs, trees and distant hills; bounded skidmarks, contact dust, tire smoke, metal scrape sparks and brake-light emission. Roadside geometry is combined into spatial material groups to reduce rendering submissions. The downloaded car's body geometry is combined from 74 renderers into 15 material groups, while wheels, brake lights and steering remain animated separately.

Scenery remains a custom prototype. No paid asset was acquired. Live mirrors, rain/wipers, driver hands, fuller sound design, authored high-quality scenery and headset comfort/performance validation remain outstanding. The existing custom centerline generator provides the revised track; Unity Splines has not been installed.

Validation: physics and race-rule checks passed; 320 road contact samples and eight checkpoint positions passed. Automated Play mode checked first-person default, acceleration, instruments, steering animation, pause, dust, reset cleanup, bounded skidmarks, smoke and actual metal guardrail impact sparks. Scenery combining reduced 1,119 renderers to 111 spatial material groups. The updated ARM64 Quest development APK built with zero errors and seven warnings, about 71 MiB. Its archive integrity and Android VR manifest were checked. Sustained headset FPS has not been measured.

## Direction

Build a believable daylight racing circuit viewed from the driver's seat. Prioritize the cockpit, road surface, lighting and roadside scale. Use one detailed car and one polished track before adding weather or more vehicles. First-person racing takes priority over the earlier tabletop mixed-reality milestone.

The XR camera uses a seated cockpit position with independent head tracking. Cockpit view is the gameplay default on both platforms; chase view is available for development inspection.

## Implementation sequence

1. **First-person foundation.** Calibrate seat position, adjustable height and recentering. Keep the road visible above the dashboard and preserve head tracking during pauses. Add desktop mouse look. Keep artificial camera shake and speed-driven FOV changes disabled. Revisit the current horizon stabilization when fitting the interior so body roll does not cause cockpit clipping. Exit: comfortable sightlines, readable instruments and correct scale in the editor; headset comfort remains subject to device testing.
2. **Detailed car and cockpit.** Replace the primitive visual shell while retaining the existing physics and race rules. Require a complete interior, separate steering wheel and wheel meshes, correct pivots, working instrument surfaces, URP materials and lower-detail meshes for distance. Animate the steering wheel and speed/RPM instruments; implement RPM/gear telemetry before displaying it as real vehicle data. Add believable rubber, painted metal, plastic and seat materials. Add visible driver hands after the wheel animation and controller mappings work. Exit: inspect from the actual seat and both eyes, not just an exterior asset screenshot.
3. **Realistic road.** Build a spline-based circuit with varied corners, modest elevation changes, curbs, shoulders and runoff. Add tiled asphalt color/normal/smoothness, worn lane paint, repair patches, rubber on the racing line and restrained cracks. Use surface geometry for curbs and changes that affect driving; normal maps only suggest small detail. Convert source roughness to URP smoothness correctly. Keep collision surfaces smooth, checkpoints aligned and respawn points above the new road. Exit: complete laps without seam bumps or missed gates.
4. **Scenery and lighting.** Add guardrails, tire barriers, brake-distance signs, fences, a small pit area and distant landscape. Reuse shared meshes/materials and simplify objects with distance. Bake suitable static lighting; use restrained sun shadows and reflection probes for the moving car. Give surfaces distinct roughness rather than making everything glossy. Exit: good visual scale and road readability from the seat.
5. **Driving effects.** Add the priority effects below using reusable particle systems, bounded skidmark geometry and explicit surface/contact conditions. Exit: effects stop on reset, pause and loss of ground contact; racing logic still passes.
6. **Quest validation.** Build an Android APK and profile sustained laps on real hardware, including smoke, grass excursions and collisions. Target stable 72 FPS initially (about 13.9 ms per frame); try 90 FPS only with measured headroom. Inspect both-eye rendering, text, transparency and thermal behavior. Desktop previews do not establish device performance.

## VFX priorities

| Effect | Trigger and appearance | Priority |
| --- | --- | --- |
| Skid marks | Grounded tire slip leaves short dark strips; cap their count and fade/recycle old sections | First pass |
| Dust and gravel | Small bursts at tires touching dirt/grass, scaled by speed | First pass |
| Tire smoke | Brief, low-density smoke on meaningful slip; small transparent area | First pass |
| Impact sparks | Short bursts only when scraping an appropriate metal surface, scaled by impact | First pass |
| Instrument and brake lighting | Emissive materials for gauges, shift LEDs and brake lights; avoid a light per indicator | First pass |
| Car reflections | Baked environment reflections for paint and glass, tuned from inside the cockpit | First pass |
| Rain and windshield wipers | Local rain, drops on glass, moving wipers and surface spray; profile separately | Later |
| Working mirror | Prototype one restricted-resolution rear camera; validate stereo comfort and rendering cost | Later |
| Exhaust effects | Subtle occasional bursts if appropriate to the chosen car; little value from the seat | Later |

Use Unity's built-in Particle System for the first effects: its C# control suits vehicle slip and collision events. VFX Graph is not required for this scope. Add engine layers, tire squeal, curb sound and controller vibration alongside VFX to communicate grip and speed. Avoid motion blur, depth-of-field blur, heavy volumetric fog and forced camera jolts in the initial VR design.

## Asset and tool shortlist

- **Road materials:** [ambientCG Asphalt 012](https://ambientcg.com/view?id=Asphalt012) provides downloadable PBR maps. Start with 1K/2K maps and compress for Android. [ambientCG's license page](https://docs.ambientcg.com/license/) identifies its assets as CC0.
- **Road authoring, preferred starting point:** [Unity Splines](https://docs.unity.cn/Packages/com.unity.splines%402.9/manual/extrude.html) supports road-shaped mesh extrusion. Resolve a compatible version through Package Manager. Custom track shoulders, markings, collision and checkpoints still need authoring.
- **Road authoring, optional:** [EasyRoads3D](https://www.easyroads3d.com/v3/html/getting_started.html) offers Free and Pro packages and URP material setup. Evaluate on a test scene with our Unity/URP versions before choosing it over Splines. No purchase is required for the proposed first pass.
- **Car candidate:** [Classic JDM Sports Car — Interior & Rigged](https://assetstore-fallback.unity.com/packages/3d/vehicles/land/classic-jdm-sports-car-interior-rigged-low-poly-362018). The publisher advertises an interior, URP support, three LODs and separate steering wheel/interactive parts; reports 32,190 triangles at LOD0 and 6,792 at LOD2. This is a shortlist candidate, not a verified Quest-ready asset. Inspect cockpit detail, material count, textures, package contents, license and current price before acquisition. Its listed older Unity compatibility does not prove compatibility with our editor.
- **Model preparation:** Blender can be used for mesh cleanup, UVs, pivots and reducing detail. No new external service or editor bridge is required for this phase.

Budget is unspecified. Begin with free textures, existing Unity tools and custom scenery. If spending on one asset, prioritize a high-quality interior because it is always near the player. Purchasing or importing third-party packs is a separate implementation step. Before publishing source, review whether acquired assets may be included in the public repository.

## Research basis and limits

- [Meta best practices](https://developers.meta.com/vr/documentation/unity/unity-best-practices-intro/) recommends mobile-conscious texture sizes and avoiding expensive effects such as motion blur and SSAO.
- [Meta Android rendering guidance](https://developers.meta.com/vr/documentation/unity/unity-mobile-performance-intro/) cautions against overlapping transparent geometry and fullscreen effects. This informs the restrained smoke/dust design.
- [Meta performance guidance](https://developers.meta.com/vr/documentation/unity/unity-perf/) gives a 72 FPS minimum for interactive applications. This is a validation target, not a result already measured for this project.
- [Unity particle-system comparison](https://docs.unity3d.com/6000.0/Documentation/Manual/ChoosingYourParticleSystem.html) documents built-in particle scripting and the alternative GPU-based VFX Graph.

The ordering, effect choices and asset recommendation are project-specific judgments based on these sources. Package compatibility, interior quality and headset performance require hands-on verification.
