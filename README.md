# PAY by square API

Rýchle a jednoduché API na **generovanie a čítanie** platobných QR kódov **PAY by square**
(pre slovenské aj české banky). Beží **bez databázy**, je samostatne hostovateľné (napríklad na
NAS-e) a kód vykresľuje v oficiálnej podobe loga PAY by square – pre tlač aj pre obrazovky.
PNG aj SVG výstup je **identický** a sebestačný (text je vykreslený ako vektor, nezávisí od
písiem v systéme).

Moderná ASP.NET (.NET 10) náhrada za projekt
[slatinsky/php-pay-by-square](https://github.com/slatinsky/php-pay-by-square), funkčne
porovnateľná s plateným riešením [easy-square.sk](https://www.easy-square.sk/).

| pre tlač (`logo=print`) | pre obrazovky (`logo=electronic`) |
|---|---|
| ![Ukážkový QR kód pre tlač](docs/sample-qr.png) | ![Ukážkový QR kód pre obrazovky](docs/sample-qr-electronic.png) |

Rozmery, farby aj tvary loga sú prevzané priamo z vektorov v oficiálnych dokumentoch
([špecifikácia 1.1.0](https://www.sbaonline.sk/wp-content/uploads/2020/03/pay-by-square-specifications-1_1_0.pdf),
[manuál loga 1.0.4](https://www.sbaonline.sk/wp-content/uploads/2020/03/pay-by-square-logo-manual-1_0_4.pdf)),
takže 30 mm kód dá logo 31,044 × 36,379 mm presne podľa manuálu a nápis „PAY by square" je
odkreslený z manuálu, nie sadzba náhradným písmom. Všetko, čo štandard určuje, je pevne dané –
API nevie vrátiť nevyhovujúci kód.

## Čo ponúka

- **PNG aj SVG** výstup, oba **vizuálne identické** (plateným službám väčšinou stačí len PNG)
- **Obe oficiálne podoby loga** – pre tlač (s rámčekom) aj pre obrazovky – v ktorejkoľvek zo štyroch povolených farieb
- **Nedá sa pokaziť** – tichá zóna, korekcia chýb, čierno-biely kód aj prítomnosť loga sú pevne dané; nepovolená farba loga skončí chybou 400
- **Dekódovanie** existujúceho QR reťazca späť na údaje platby (`/decode`)
- **Validácia IBAN** (mod-97) pre SK, CZ a všetky ostatné krajiny
- BIC/SWIFT je **voliteľný**, podpora **mena CZK** a viacerých účtov
- Nastaviteľná veľkosť (`size` / `ppm`) a farba loga
- Bez databázy, bez registrácie, bez API kľúča (voliteľne sa dá zapnúť)
- Interaktívna dokumentácia (Swagger UI) na `/swagger`

## Spustenie cez Docker Compose

Vytvorte súbor `docker-compose.yml` v tomto repozitári je pripravená šablóna [docker-compose.yml](docker-compose.yml)

Potom spustite:

```bash
docker compose up -d
```

A je to. Služba beží na porte **8080**:

- Dokumentácia: `http://<ip>:8080/swagger`
- Príklad QR kódu: `http://<ip>:8080/api/v1/qr?amount=10&iban=SK7283300000009111111118`

## API endpointy

### `GET /api/v1/qr` — QR obrázok z parametrov v URL
Ideálne na priame vloženie do `<img>` značky alebo do e-mailu/faktúry.

```
GET /api/v1/qr?amount=25.50&iban=SK7283300000009111111118&vs=1234&note=Faktura%2042&format=png&size=512
```

| Parameter | Význam | Poznámka |
|---|---|---|
| `amount` / `price` | suma | max. 2 desatinné miesta |
| `currency` | mena (ISO) | predvolene `EUR`, pre ČR `CZK` |
| `iban` | číslo účtu (**povinné**) | medzery sa ignorujú, kontrola mod-97 |
| `bic` / `swift` | BIC | voliteľné |
| `vs`, `cs`, `ss` | variabilný / konštantný / špecifický symbol | |
| `date` | dátum splatnosti | formát `RRRR-MM-DD` |
| `note` | správa pre prijímateľa | diakritika sa predvolene odstráni |
| `beneficiary` / `recipient` | názov príjemcu | |
| `format` | `png` / `svg` / `string` / `json` | viď nižšie |
| `size` | cieľová veľkosť dlhšej strany v px | `32` – `4096`, napr. `256`, `512`, `960` |
| `ppm` | pixelov na modul | `1` – `100`, predvolene `8`; alternatíva k `size` |
| `logo` | podoba loga | `print` (predvolené) / `electronic` |
| `brandcolor` | farba loga | `#A1C7E9`, `#6FA4D7` (predvolené), `#5F6062`, `#000000` – alebo názvy `sky`, `blue`, `grey`, `black` |

**Hodnoty parametra `logo`** – obe podoby sú z manuálu loga 1.0.4:

| `logo` | Čo vykreslí | Kedy |
|---|---|---|
| `print` (predvolené) | „basic logo for print devices" – kód v otvorenom rámčeku, pod ním nápis a ikona | faktúry, tlač |
| `electronic` | „basic logo for electronic devices" – holý kód, pod ním nápis a ikona bez rámčeka | obrazovky, e-maily |

Kód sa nikdy nevracia bez loga – špecifikácia (bod 3.2) ho vyžaduje pri každom kóde. Pevne dané
sú aj tichá zóna (4 moduly), korekcia chýb `L` (tabuľka 11) a čierny kód na bielom pozadí, takže
sa už nedajú nastaviť.

**Hodnoty parametra `format`:**

| `format` | Čo vráti | Content-Type |
|---|---|---|
| `png` (predvolené) | QR obrázok s logom PAY by square (raster) | `image/png` |
| `svg` | ten istý obrázok ako vektor (vizuálne identický s PNG) | `image/svg+xml` |
| `string` | **iba** zakódovaný reťazec PAY by square ako čistý text (žiadny obrázok) – hodí sa, keď si QR generujete vlastným nástrojom | `text/plain` |
| `json` | ten istý reťazec zabalený v JSON-e: `{ "code": "..." }` | `application/json` |

### `POST /api/v1/qr` — QR obrázok z JSON tela
Telo je objekt platby (viď nižšie), parametre vzhľadu sa zadávajú v URL. Údaje platby teda idú
v tele, vzhľad v query stringu:

```
POST /api/v1/qr?format=svg&size=512&logo=electronic&brandcolor=black
```

| Parameter | Význam | Hodnoty | Predvolené |
|---|---|---|---|
| `format` | formát odpovede | `png` / `svg` / `string` / `json` | `png` |
| `size` | cieľová veľkosť dlhšej strany v px | `32` – `4096` | podľa `ppm` |
| `ppm` | pixelov na modul (alternatíva k `size`) | `1` – `100` | `8` |
| `logo` | podoba loga | `print` / `electronic` | `print` |
| `brandcolor` | farba loga | `#A1C7E9`, `#6FA4D7`, `#5F6062`, `#000000` – alebo názvy `sky`, `blue`, `grey`, `black` | `#6FA4D7` |

Sú to tie isté parametre vzhľadu ako pri `GET /api/v1/qr`: `size` má prednosť pred `ppm`,
hodnoty `format` a `logo` sú popísané v tabuľkách vyššie.

### `POST /api/v1/encode` — len zakódovaný reťazec
```json
{ "amount": 25.50, "iban": "SK7283300000009111111118", "bic": "FIOZSKBAXXX",
  "variableSymbol": "1234", "note": "Faktúra 42", "beneficiaryName": "Ján Novák",
  "dueDate": "2026-06-28" }
```
→ `{ "code": "0006..." }`

**Polia objektu platby** (používa ich `POST /api/v1/qr` aj `POST /api/v1/encode`; vracia ich
`POST /api/v1/decode`):

| Pole | Typ | Význam | Poznámka |
|---|---|---|---|
| `iban` | text | číslo účtu (**povinné**) | medzery sa ignorujú, kontrola mod-97 |
| `bic` | text | BIC/SWIFT účtu | voliteľné, banky ho už nevyžadujú |
| `amount` | číslo | suma | voliteľná – kód môže byť aj bez sumy |
| `currencyCode` | text | mena (ISO 4217) | predvolene `EUR`, pre ČR `CZK` |
| `dueDate` | text | dátum splatnosti | formát `RRRR-MM-DD` |
| `variableSymbol` | text | variabilný symbol | max. 10 číslic |
| `constantSymbol` | text | konštantný symbol | max. 4 číslice |
| `specificSymbol` | text | špecifický symbol | max. 10 číslic |
| `originatorReference` | text | referencia platiteľa | štruktúrovaná referencia SEPA |
| `note` | text | správa pre prijímateľa | max. 140 znakov, diakritika sa predvolene odstráni |
| `beneficiaryName` | text | názov príjemcu | niektoré banky (napr. SLSP) ho vyžadujú |
| `beneficiaryAddressLine1` | text | adresa príjemcu – ulica | |
| `beneficiaryAddressLine2` | text | adresa príjemcu – PSČ a mesto | |
| `alternativeAccounts` | pole | ďalšie účty na výber | `[{ "iban": "...", "bic": "..." }]` |

Povinný je len `iban`; prázdne polia sa do kódu nezapisujú. Celý kód musí vyjsť do 550 znakov
(špecifikácia, tabuľka 10) – dlhšia platba skončí chybou 400.

### `POST /api/v1/decode` — prečítanie kódu späť na údaje
```json
{ "code": "0006U000F0OSJJ2G9BRQ70..." }
```
→ `{ "crcValid": true, "rawData": "...", "payment": { ... } }`

### `POST /api/v1/validate-iban`
```json
{ "iban": "SK72 8330 0000 0091 1111 1118" }
```
→ `{ "valid": true, "normalized": "SK7283300000009111111118", "error": null }`

### `GET /health`
Kontrola dostupnosti → `{ "status": "ok" }`.

## Zabezpečenie (voliteľné)

| Premenná prostredia | Účinok |
|---|---|
| `PBS_API_KEY` | ak je nastavená, `/api/*` vyžaduje hlavičku `X-Api-Key` so zhodnou hodnotou |
| `PBS_RATE_LIMIT` | počet požiadaviek za minútu na jednu IP adresu; nenastavené = bez limitu |

## Ako funguje kódovanie

1. Zostaví sa reťazec údajov podľa dátového modelu PAY by square (oddelený tabulátormi).
2. Pridá sa kontrolný súčet CRC-32 (little-endian).
3. Komprimácia **raw LZMA1** (`lc=3, lp=0, pb=2, dict=128 KiB`).
4. Pridá sa 4-bajtová hlavička (`00 00` + 2-bajtová dĺžka pôvodných dát).
5. Výsledok sa zakóduje cez base32hex (`0–9 A–V`).
6. Reťazec sa vykreslí ako QR kód.

Výstup je overený oproti referenčnému postupu `xz` a každý vygenerovaný kód je otestovaný, že sa
dá spätne prečítať bežným LZMA dekodérom – QR kódy preto fungujú v bankových aplikáciách.

## Licencia a kredity

PAY by square je štandard Slovenskej bankovej asociácie. Toto je nezávislá implementácia
inšpirovaná pôvodným PHP projektom (Jozef Slatinský, Ján Fečík). Rámček, nápis aj ikona sú
nakreslené vlastnou vektorovou grafikou podľa rozmerov odmeraných z manuálu loga 1.0.4;
vykresľovanie PNG aj SVG je úplne vlastné (bez grafických knižníc tretích strán).

**Všetky závislosti majú permisívne licencie** – žiadne licenčné komplikácie:

| Knižnica | Na čo | Licencia |
|---|---|---|
| LZMA SDK (7-Zip) | kompresia LZMA1 | Public domain |
| QRCoder | výpočet QR matice | MIT |
| Swashbuckle (len API) | Swagger/OpenAPI | MIT |

Nápis „PAY by square" nie je sadzba náhradným písmom – sú to obrysy odkreslené priamo z vektorov
v manuáli loga 1.0.4 (strana 7). Sú zapečené pri zostavení, za behu sa žiadne písmo nepoužíva.
Podrobnosti viď [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
