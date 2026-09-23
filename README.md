# Easy Outline

Easy Outline draws clean screen-space silhouettes around selected 3D objects in Unity URP.

## Setup

1. Install the package.
2. Run `Tools > Outline > Setup`.
3. Add `OutlineTarget` to an object.
4. The object and its child renderers receive one shared outline.

No material, replacement shader, gameplay layer, extra camera, or persistent render texture is required.

## Runtime

```csharp
using Natteens.Outline;

OutlineTarget target = GetComponent<OutlineTarget>();
target.Outlined = true;
target.ColorOverride = true;
target.Color = Color.yellow;
```

Set `target.Outlined = false` to turn the outline off.

`RefreshRenderers()` is only needed after changing the renderer hierarchy at runtime.

## Bulk selection

Set the profile selection mode to `Layers` or `Targets + Layers`, then choose normal Unity GameObject Layers in the LayerMask field. The default mode is `Targets`; an `OutlineTarget` needs no GameObject layer change.

Disable **Outline Occlusion Edges** for an object-silhouette contour. Enable it to outline the complete boundary of each currently visible fragment, including cuts made by foreground geometry.

## Rendering

The renderer feature resolves visible object pixels, expands their 2D silhouette to the profile thickness, and draws one uniform border. It runs after the skybox so outlines remain visible in Game and Scene View. Depth only decides which selected pixels are visible before expansion.

Renderers resolved by one `OutlineTarget` share a group ID, so a character assembled from body, hair, armor, and weapons receives one combined silhouette without seams. Independent targets remain distinguishable where they touch.

## Limitations

- Opaque and alpha-clipped URP Lit/Unlit materials are supported.
- True blended transparent objects are not supported in 0.1.0.
- Custom shaders need a compatible `DepthOnly`, `DepthNormalsOnly`, `UniversalForward`, `UniversalForwardOnly`, or `SRPDefaultUnlit` pass.
- XR architecture is texture-array aware but is not validated for this release.
- The package targets the Universal 3D Renderer, not the URP 2D Renderer.
