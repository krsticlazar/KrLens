# KrLens

KrLens je klijent/server aplikacija za filtriranje slike. Projekat je uradjen kao predispitna obaveza na predmetu Multimedijalni sistemi na Elektronskom fakultetu u Nisu.

Frontend sluzi za upload slike, izbor filtera, preview rezultata, undo/redo, vracanje originala, rotaciju i download. Obrada slike se izvrsava na backendu; klijent samo salje sliku i parametre filtera i prikazuje rezultat koji server vrati.

## Struktura

- `src/KrLensClient` - React klijent.
- `src/KrLensServer` - ASP.NET Core backend.
- `scripts` - BAT skripte za pripremu okruzenja i pokretanje aplikacije.
- `docs` - dokumentacija, grafici latencije i rezultati merenja.

## Pokretanje

Najbrzi nacin na Windows-u:

1. Pokrenuti `scripts/prepare_environment.bat` kao administrator.
2. Pokrenuti `scripts/start_application.bat` kao administrator.
3. Otvoriti frontend na `http://localhost:5173`.

Sta skripte rade:

- `prepare_environment.bat` proverava .NET SDK i Node.js, radi `dotnet restore` za backend i `npm install` za frontend.
- `start_application.bat` otvara dva CMD prozora: backend sa `dotnet run` i frontend sa `npm run dev`.

Rucno pokretanje:

```bat
cd src\KrLensServer\src\KrLensServer.API
dotnet run
```

```bat
cd src\KrLensClient
npm install
npm run dev
```

## Funkcionalnosti

- Upload slika: PNG, JPEG, BMP, GIF i MSI.
- Server-side obrada slike.
- Preview aktivne slike i prikaz originalnog formata/dimenzija.
- Undo/redo do 50 koraka.
- Vrati original.
- Rotacija udesno.
- Download u PNG, JPEG, BMP, GIF i MSI formatu.
- Batch obrada filtera preko backend API-ja.
- Logovanje upload-a, filtera i gresaka na serveru.

## Filteri

Testirani filteri:

- Grayscale
- Invert
- Brightness
- Contrast
- Gamma
- Smooth
- EdgeDetectHV
- Flip
- Water
- Stucki
- HistogramEqualizing

Osnovni filteri koriste `Parallel.For` po redovima slike. Kompleksniji filteri koriste paralelizaciju tamo gde pikseli ne zavise jedni od drugih.

## Dokumentacija i merenja

Glavna dokumentacija je u `docs/KrLens_dokumentacija.docx`.

U folderu `docs` se nalaze:

- Word dokumentacija projekta.
- CSV rezultati benchmark merenja.

Benchmark je radjen preko stvarnog backend API-ja na 12 slika normalizovanih na 1024x768, sa 10 ponavljanja po filteru.

## Repo

GitHub: https://github.com/krsticlazar/KrLens
