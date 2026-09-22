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

`RefreshRenderers()` is only needed after changing the renderer hierarchy at runtime.

## Bulk selection

Set the profile selection mode to `Layers` or `Targets + Layers`, then choose a normal Layer Mask. The default mode is `Targets`, so installing the package never outlines the entire scene.

## Rendering

The renderer feature runs after opaque rendering. It captures alpha-clipped selected depth, writes group ID and optional target color metadata, builds a binary visible-silhouette mask, expands it to the requested thickness, and composites the outline once. Depth is used only to reject occluded pixels and resolve ownership; it never changes line color, opacity, or thickness.

Renderers resolved by one `OutlineTarget` share a group ID, so a character assembled from body, hair, armor, and weapons receives one combined silhouette without seams. Independent targets remain distinguishable where they touch.

## Limitations

- Opaque and alpha-clipped URP Lit/Unlit materials are supported.
- True blended transparent objects are not supported in 0.1.0.
- Custom shaders need a compatible `DepthOnly`, `DepthNormalsOnly`, `UniversalForward`, `UniversalForwardOnly`, or `SRPDefaultUnlit` pass.
- XR architecture is texture-array aware but is not validated for this release.
- The package targets the Universal 3D Renderer, not the URP 2D Renderer.
