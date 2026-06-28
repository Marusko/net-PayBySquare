// Independent check: render a QR via the API, decode the PNG with jsQR, compare to /encode.
import { PNG } from "pngjs";
import jsQR from "jsqr";

const base = process.argv[2] ?? "http://localhost:5099";
const payment = { amount: 25.5, iban: "SK7283300000009111111118", bic: "FIOZSKBAXXX", variableSymbol: "1234", note: "Faktura 42", beneficiaryName: "Jan Novak", dueDate: "2026-06-28" };

const encRes = await fetch(`${base}/api/v1/encode`, { method: "POST", headers: { "content-type": "application/json" }, body: JSON.stringify(payment) });
const { code } = await encRes.json();

const qs = new URLSearchParams({ amount: "25.5", iban: payment.iban, bic: payment.bic, vs: "1234", note: "Faktura 42", beneficiary: "Jan Novak", date: "2026-06-28", format: "png", size: "512" });
const pngRes = await fetch(`${base}/api/v1/qr?${qs}`);
const buf = Buffer.from(await pngRes.arrayBuffer());

const png = PNG.sync.read(buf);
const decoded = jsQR(new Uint8ClampedArray(png.data), png.width, png.height);

console.log("encoded code :", code);
console.log("scanned  code:", decoded ? decoded.data : "(QR not detected)");
console.log("MATCH        :", decoded && decoded.data === code);
process.exit(decoded && decoded.data === code ? 0 : 1);
