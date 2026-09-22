# Changelog

## Unreleased

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
