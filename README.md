# Outline

Object-aware screen-space outlines for Unity 6 URP.

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
target.RefreshRenderers();
```

## Bulk selection

Set the profile selection mode to `Layers` or `Targets + Layers`, then choose a normal Layer Mask. The default mode is `Targets`, so installing the package never outlines the entire scene.

## Rendering

The renderer feature runs after opaque rendering. It first captures selected depth with each object's compatible URP depth/forward pass, then writes object ID, geometric normal and optional target color into transient metadata. The composite compares selected linear eye depth with camera depth before resolving object-aware edges. Thickness above one pixel uses two linear-cost separable expansion passes.

## Limitations

- Opaque and alpha-clipped URP Lit/Unlit materials are supported.
- True blended transparent objects are not supported in 0.1.0.
- Custom shaders need a compatible `DepthOnly`, `DepthNormalsOnly`, `UniversalForward`, `UniversalForwardOnly`, or `SRPDefaultUnlit` pass.
- XR architecture is texture-array aware but is not validated for this release.
- The package targets the Universal 3D Renderer, not the URP 2D Renderer.
