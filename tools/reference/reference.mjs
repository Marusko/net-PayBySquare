// Ground-truth PAY by square generator using the exact xz pipeline from the PHP repo.
import { spawnSync } from "node:child_process";
import zlib from "node:zlib";

function buildData({ price, iban, swift = "", vs = "", cs = "", ss = "", note = "", recipient = "", date = "20170101" }) {
  return ["", "1", "1", price, "EUR", date, vs, cs, ss, "", note, "1", iban, swift, "0", "0", recipient].join("\t");
}

function base32hex(buf) {
  let bits = "";
  for (const b of buf) bits += b.toString(2).padStart(8, "0");
  if (bits.length % 5) bits += "0".repeat(5 - (bits.length % 5));
  const A = "0123456789ABCDEFGHIJKLMNOPQRSTUV";
  let out = "";
  for (let i = 0; i < bits.length; i += 5) out += A[parseInt(bits.slice(i, i + 5), 2)];
  return out;
}

function crc32le(buf) {
  // zlib.crc32 not exposed; compute standard CRC32 (poly 0xEDB88320, reflected)
  let crc = 0xffffffff;
  for (const b of buf) {
    crc ^= b;
    for (let i = 0; i < 8; i++) crc = (crc >>> 1) ^ (0xedb88320 & -(crc & 1));
  }
  crc = (crc ^ 0xffffffff) >>> 0;
  // PHP: hash crc32b binary is big-endian, strrev -> little-endian bytes
  return Buffer.from([crc & 0xff, (crc >>> 8) & 0xff, (crc >>> 16) & 0xff, (crc >>> 24) & 0xff]);
}

function generate(input) {
  const data = Buffer.from(buildData(input), "utf8");
  const payload = Buffer.concat([crc32le(data), data]);
  const r = spawnSync("xz", ["--format=raw", "--lzma1=lc=3,lp=0,pb=2,dict=128KiB", "-c", "-"], { input: payload, maxBuffer: 1 << 24 });
  if (r.status !== 0) { console.error("xz failed", r.stderr?.toString()); process.exit(1); }
  const compressed = r.stdout;
  const header = Buffer.from([0, 0, payload.length & 0xff, (payload.length >>> 8) & 0xff]);
  const full = Buffer.concat([header, compressed]);
  return { data, payload, compressed, full, code: base32hex(full) };
}

const cases = [
  { price: "10", iban: "SK7283300000009111111118", swift: "FIOZSKBAXXX", vs: "47" },
  { price: "5.01", iban: "SK7700000000000000000000", swift: "CEKOSKBX", vs: "00000002", ss: "2022", cs: "0000", note: "pre jozka", recipient: "jozko mrkvicka", date: "20170101" },
  { price: "100.5", iban: "SK3112000000198742637541", swift: "" },
];
for (const c of cases) {
  const r = generate(c);
  console.log("INPUT:", JSON.stringify(c));
  console.log("DATA:", JSON.stringify(r.data.toString("utf8")));
  console.log("UNCOMPRESSED_LEN:", r.payload.length);
  console.log("PAYLOAD_HEX:", r.payload.toString("hex"));
  console.log("COMPRESSED_HEX:", r.compressed.toString("hex"));
  console.log("FULL_HEX:", r.full.toString("hex"));
  console.log("CODE:", r.code);
  console.log("-".repeat(60));
}
