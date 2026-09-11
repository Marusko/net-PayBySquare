import { PNG } from "pngjs";
import jsQR from "jsqr";
import { Resvg } from "@resvg/resvg-js";

const base = "http://localhost:5099";
const qs = "amount=25.5&iban=SK7283300000009111111118&vs=1234&beneficiary=Jan%20Novak";
const { code } = await (await fetch(`${base}/api/v1/encode`, {
  method: "POST", headers: { "content-type": "application/json" },
  body: JSON.stringify({ amount: 25.5, iban: "SK7283300000009111111118", variableSymbol: "1234", beneficiaryName: "Jan Novak" }),
})).json();

const scan = (buf) => { const p = PNG.sync.read(buf); const d = jsQR(new Uint8ClampedArray(p.data), p.width, p.height); return { size: `${p.width}x${p.height}`, ok: d && d.data === code }; };

let fail = 0;
for (const logo of ["print", "electronic"]) {
  for (const brand of ["%23A1C7E9", "%236FA4D7", "%235F6062", "%23000000"]) {
    for (const size of [240, 512]) {
      const png = Buffer.from(await (await fetch(`${base}/api/v1/qr?${qs}&logo=${logo}&brandcolor=${brand}&size=${size}&format=png`)).arrayBuffer());
      const svg = await (await fetch(`${base}/api/v1/qr?${qs}&logo=${logo}&brandcolor=${brand}&size=${size}&format=svg`)).text();
      const a = scan(png), b = scan(Buffer.from(new Resvg(svg).render().asPng()));
      if (!a.ok || !b.ok) fail++;
      console.log(`${logo.padEnd(11)} ${decodeURIComponent(brand)} size=${String(size).padEnd(4)} png ${a.size.padEnd(9)} ${a.ok ? "ok" : "FAIL"}   svg ${b.size.padEnd(9)} ${b.ok ? "ok" : "FAIL"}`);
    }
  }
}
console.log(fail === 0 ? "\nALL MODES SCAN OK" : `\n${fail} FAILURES`);
