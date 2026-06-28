// Rasterise the SVG endpoint output independently and scan it — proves the SVG carries the same
// scannable QR as the PNG.
import { Resvg } from "@resvg/resvg-js";
import { PNG } from "pngjs";
import jsQR from "jsqr";

const base = process.argv[2] ?? "http://localhost:5092";
const qs = new URLSearchParams({ amount: "25.5", iban: "SK7283300000009111111118", vs: "1234", beneficiary: "Jan Novak", format: "svg", size: "520" });

const encRes = await fetch(`${base}/api/v1/encode`, { method: "POST", headers: { "content-type": "application/json" }, body: JSON.stringify({ amount: 25.5, iban: "SK7283300000009111111118", variableSymbol: "1234", beneficiaryName: "Jan Novak" }) });
const { code } = await encRes.json();

const svg = await (await fetch(`${base}/api/v1/qr?${qs}`)).text();
const png = new Resvg(svg).render().asPng();

const img = PNG.sync.read(png);
const decoded = jsQR(new Uint8ClampedArray(img.data), img.width, img.height);
console.log("svg size:", img.width, "x", img.height);
console.log("scanned :", decoded ? decoded.data : "(not detected)");
console.log("MATCH   :", decoded && decoded.data === code);
process.exit(decoded && decoded.data === code ? 0 : 1);
