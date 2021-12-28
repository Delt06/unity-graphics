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
- Cheap soft shadows
- Can be used as primitive AO
- Includes an example shader that supports receiving blob shadows (an extension of [Toon Shader Lite](https://github.com/Delt06/urp-toon-shader#toon-shader-lite-capabilities))
- Performance: **to be compared with built-in shadow maps**