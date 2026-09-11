# Planet image credits and limits

The approved `earth.jpg` source is retained unchanged. The map and cloud files beside it are
equirectangular layers; they are not substitutes for that approved source-disc image.

| Asset | Pixels | Projection used by game | Provenance / limitation |
| --- | ---: | --- | --- |
| `venus.jpg` | 2048 x 1024 | Equirectangular | Replaced the previous 5,659-byte comparison image with NASA Science's 3D Venus texture (30 May 2025), stitched from Magellan radar imagery with global texture filling gaps; NASA describes it as JPL/Caltech generated planetary-map data. This is radar-derived surface representation, not a visible-light photograph through Venus's clouds. |
| `mercury.jpg` | 2147 x 2147 | Source disc | Existing source-disc asset; no full-surface map is claimed. |
| `mars.jpg` | 1440 x 720 | Equirectangular | Existing map-sized asset. |
| `jupiter.jpg`, `saturn.jpg`, `neptune.jpg` | 720 x 360 | Equirectangular | Existing map-sized assets. Their limited resolution remains visible in close inspection. |
| `uranus.jpg` | 1024 x 512 | Source disc | Existing source-disc asset; no global texture is inferred. |
| `moon.jpg` | 1913 x 1911 | Source disc | Existing source-disc asset; `moon-map.jpg` is the separate equirectangular map. |

Primary replacement source: [NASA Science: Venus 3D resource](https://science.nasa.gov/3d-resources/venus/).
It identifies the texture as Magellan RADAR imagery with gaps filled from a global texture and
attributes the generated planetary-map data to JPL/Caltech. The runtime now records Venus as an
equirectangular texture (`GetSourceDisc("venus") == Vector4.Zero`), so it is never cropped or
wrapped as a narrow source disc.
