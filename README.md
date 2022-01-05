# Unity Graphics

A collection of URP shaders and renderer features.

> Developed and tested with Unity 2020.3.16f1 and URP 10.5.1.

## Fog Skybox

![Fog Skybox](Documentation/fog_skybox.jpg)

A skybox shader that blends with fog.

## Blob Shadows

![Blob Shadows](Documentation/blob_shadows.jpg)

A render feature that adds support for blob shadows:

- Either circle or box shape
- Can be used as primitive AO
- Includes an example shader that supports receiving blob shadows (an extension of [Toon Shader Lite](https://github.com/Delt06/urp-toon-shader#toon-shader-lite-capabilities))

### Performance
- Measured on Snapdragon 845 via Unity's GPU Profiler (OpenGL ES)
- Setup: ~20 capsules
- **Blob Shadows**: 
  - All circles 
  - Resolution per unit = 8 (further increase does not improve visual quality)
  - Shadow Distance = 15
- **Shadow Maps**:
  - Shadow Resolution = 256
  - Shadow Distance = 20
  - Soft Shadows On
  - 1 Cascade

> Note: sampling shadow maps when rendering shadow receivers is **NOT** taken into account. 

Results (in ms):
- **Blob**: 0.08 (Submit) + 0.21 (Render) = **0.29**
- **Shadow Maps**: 0.05 (Setup) + 0.21 (PrepareDrawShadows) + 0.21 (Submit) + 0.13 (Render) = **0.6**