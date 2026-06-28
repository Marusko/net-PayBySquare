# Third-party notices

This project ships with only permissive-licensed dependencies. PNG and SVG rendering is written
in-house (no third-party imaging or font library at runtime).

## Runtime dependencies

| Component | Used for | Licence |
|---|---|---|
| LZMA-SDK (7-Zip) | raw LZMA1 compression | Public domain |
| QRCoder | QR matrix generation | MIT |
| Swashbuckle.AspNetCore (API only) | Swagger / OpenAPI UI | MIT |

## Bundled assets

- **"PAY by square" wordmark** — the file `src/PayBySquare.Core/Assets/Wordmark.bin` contains
  vector outlines derived from the **Lato** typeface (SIL Open Font License 1.1). The license text
  is included at `tools/bake/OFL.txt`. The outlines are generated at build time by the dev-only
  tool in `tools/bake` (which is **not** part of the shipped application).

## Dev-only tooling (not shipped)

- `tools/bake` uses SixLabors.ImageSharp.Drawing / SixLabors.Fonts (Six Labors Split License) to
  bake the wordmark outlines once. It is not referenced by `PayBySquare.Core` or `PayBySquare.Api`
  and is excluded from the Docker image.
