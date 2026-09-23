# [0.2.0](https://github.com/Natteens/easyoutline/compare/v0.1.0...v0.2.0) (2026-09-23)


### Bug Fixes

* refine outline rendering and editor UX ([8c37ad8](https://github.com/Natteens/easyoutline/commit/8c37ad830cd2c961747d27d96ea09bbed655d8f0))
* simplify outlines to stable silhouettes ([988635c](https://github.com/Natteens/easyoutline/commit/988635c4f089c18cacec9687837f442faf660167))


### Features

* create standalone outline renderer ([3fbddcd](https://github.com/Natteens/easyoutline/commit/3fbddcda22db713f2789e0b4aea71c56af50a02a))
* refine outline controls and inspector UX ([2c9f8fe](https://github.com/Natteens/easyoutline/commit/2c9f8fe1f05b42dfb7f9fe7588564af180045334))

# Changelog

## Unreleased

- Replaced branded Inspector headers and success badges with compact Unity-style controls and error-only help boxes.
- Hid the internal rendering-layer bit, added an open-scene conflict repair, and kept layer selection on Unity's GameObject LayerMask.
- Added optional occlusion-edge outlines, off by default for pure projected-object silhouettes.
- Resolved selected visibility before silhouette dilation, removing destination-depth clipping.
- Moved the outline pass after the skybox so Scene View contours remain complete against the sky.
- Replaced depth-carrying RGBA16F dilation buffers with compact group and color data.
- Added a background-depth regression sphere and movable wall to the QA scene.
- Rebuilt the profile, target, and renderer feature inspectors with compact controls and live status.
- Replaced normal/depth crease detection with a stable binary silhouette mask.
- Made thickness expansion preserve uniform opacity and deterministic object ownership.
- Removed the normals metadata buffer and internal-detail settings.
- Fixed editor `PropertyField` UXML namespaces across all custom inspectors.

## [0.1.0] - 2026-09-21

- Added object-aware URP RenderGraph outlines.
- Added depth-correct occlusion, stable object IDs and multi-renderer grouping.
- Added alpha-clipped coverage through original material depth passes.
- Added 1–8 pixel screen-space thickness.
- Added runtime `OutlineTarget`, UI Toolkit inspectors and one-click setup.
