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

- **"PAY by square" wordmark** — the file `src/PayBySquare.Core/Assets/Wordmark.bin` holds the
  wordmark as vector outlines traced from the artwork in the official *PAY by square logo manual
  1.0.4* (Slovak Banking Association), page 7. It is the logo itself, not a substitute typeface set
  to resemble it, which is what the manual requires ("Changes in composition of logo, that is
  editing of shapes or colors ... are not permitted"). The readable source is
  `tools/bake/wordmark-outlines.txt`; the blob is produced from it by the dev-only tool in
  `tools/bake` (which is **not** part of the shipped application).

  PAY by square is a standard of the Slovak Banking Association; the logo and the name are theirs.
  This project reproduces the logo as the manual specifies, for use alongside PAY by square codes.

## Dev-only tooling (not shipped)

- `tools/bake` has no package dependencies: it parses `wordmark-outlines.txt` and flattens the
  curves into the blob. It is not referenced by `PayBySquare.Core` or `PayBySquare.Api` and is
  excluded from the Docker image.
