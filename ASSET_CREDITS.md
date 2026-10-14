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
