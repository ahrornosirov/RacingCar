# Asset sources

- Asphalt 012, ambientCG: https://ambientcg.com/view?id=Asphalt012
  - Download: Asphalt012_1K-JPG.zip, retrieved 14 October 2026.
  - Color, OpenGL normal and roughness maps are included. The Unity upgrade tool converts roughness to a packed metallic/smoothness texture.
  - License: CC0 1.0 Universal, https://docs.ambientcg.com/license/
- **Car Concept**, model and textures by **Eric Chadwick**, copyright 2024 **Darmstadt Graphics Group GmbH**.
  - Source: https://github.com/KhronosGroup/glTF-Sample-Assets/tree/main/Models/CarConcept
  - License: Creative Commons Attribution 4.0 International, https://creativecommons.org/licenses/by/4.0/
  - Original GLB, metadata and license are preserved under `Assets/ThirdParty/CarConcept`. The source asset contains Khronos trademarks; their notice is included separately. No endorsement is implied.
  - Adaptations: converted to FBX in Blender; replaced glTF materials with mobile URP materials; compressed texture imports for Android; adapted glass; removed source-brand plates/emblem from the gameplay variant; replaced static instruments with game telemetry; attached wheel and steering animation; combined body renderers. The original download is unchanged.
  - Creator/license credit also appears on the cockpit dashboard in the game.
- The previous roadster body/cockpit, circuit, roadside props and driving effects are custom procedural assets created for this project. The downloaded concept car now replaces the roadster's visible body, cockpit and wheels. No paid car model or road package was purchased.
- TextMesh Pro resources came from Unity's bundled essential resources and retain their included license notices.
  - The bundled mobile overlay shader adds an instancing variant for the menu's Quest rendering. Menu materials copy its font atlas; the gameplay font asset remains unchanged.

## Toyota Corolla E180

- **2014 Toyota Corolla E180 EU (with interior)** by **Armored Wave**.
- Source: https://sketchfab.com/3d-models/2014-toyota-corolla-e180-eu-with-interior-36f95efb0585464cae43a25a3b3392e8
- License: Creative Commons Attribution 4.0 International, https://creativecommons.org/licenses/by/4.0/
- Original archive downloaded from the author's Sketchfab listing. Source FBX preserved alongside the adapted FBX; full original textures are retained locally in ignored `Backups/CorollaSource` and are available through the source link.
- Changes: wheels separated for WheelCollider animation; steering pivot fitted; textures reduced to 1K; URP materials adapted; source plates with missing textures removed; cockpit telemetry, collision shape, camera positions and custom game handling added.
- Some texture references in the original FBX were absent from its archive; those surfaces use material colors. The source listing credits the tire texture to TextureCan: https://www.texturecan.com/details/575/ .
- Vehicle names describe the artwork. Physics values are gameplay tuning, not manufacturer specifications.

## Ford Mustang ROUSH 2019 – Stage 3

- Model by **_the.render.box_**, downloaded from the author's Sketchfab listing:
  https://sketchfab.com/3d-models/ford-mustang-roush-2019-stage-3-c3be292e50774be98bac9de749579d80
- Creative Commons Attribution 4.0 International: https://creativecommons.org/licenses/by/4.0/
- Original downloaded glTF and license are preserved under `Assets/ThirdParty/Mustang/Source` and `CREDITS.txt`.
- Changes: material-based static batches, separate animated wheels and steering, URP paint/glass/brake-light materials, fitted cockpit/chase cameras, live telemetry, collision shape and custom rear-wheel-drive handling. Scale adapted to the game.
- The downloaded variant has material colors and modeled details; it includes no texture images. Mirrors remain nonfunctional reflective surfaces. Instrument telemetry is supplied by the game's additional dashboard display.
- Handling and speed limits are game tuning, not manufacturer specifications. Quest performance and comfort still require device validation.

## Porsche 911 Turbo S

- **Porsche 911 with interior** by **n.brizitskaya**.
- Source: https://sketchfab.com/3d-models/porsche-911-with-interior-877b1bc1739f4a2bb65d62fd7ffd9f75
- License: Creative Commons Attribution 4.0 International, https://creativecommons.org/licenses/by/4.0/
- Original downloaded glTF, textures and license are preserved under `Assets/ThirdParty/Porsche/Source` and `CREDITS.txt`.
- Changes: closed the source's open door; normalized placement/orientation; static material batches; animated wheels/steering; 1K gameplay textures; packed metallic/smoothness maps for URP; fitted cockpit/chase cameras, live telemetry, collision shape, rear brake-light emission and custom AWD handling.
- Handling values are game tuning, not manufacturer-certified performance. Mirrors do not render a rear-facing camera view. Device performance and comfort remain pending Quest hardware testing.
# Mountain Race Tracks

- Creator: CRAFTED DIGITALS.
- Official source: https://assetstore.unity.com/packages/3d/environments/landscapes/mountain-race-tracks-110408
- Version: 1.1, acquired through Unity My Assets.
- License: Standard Unity Asset Store EULA, https://unity.com/legal/as-terms . This asset is not CC0 or CC BY.
- Adaptations: URP road and terrain materials, terrain settings, road collision checks, 16 ordered checkpoint triggers, start/finish paint, map selection and race integration.
- Downloaded source and derived terrain/material assets stay local and are ignored by Git. To restore them on another computer, acquire/import the official package into the licensed Unity account and run `MountainTrackSetup.Apply` in the Editor. No author email was sent.
