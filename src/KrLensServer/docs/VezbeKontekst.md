# Kontekst iz `docs/Vezbe - Materijali`

Ovaj backend je namerno približen stilu laboratorijskih materijala umesto modernom "framework-heavy" pristupu. Najvažniji obrasci koji se ponavljaju kroz projekte su:

- statičke pomoćne klase umesto duboke hijerarhije objekata
- `unsafe` i ručni prolazi kroz bafer slike
- eksplicitne petlje, privremene promenljive i mehanička obrada podataka
- ručno pisanje i čitanje binarnih formata kada je potrebno
- jasna podela na osnovne filtere, konvolucije, prostorne deformacije i tonalne transformacije

## Pregled projekata

- `04 Bitmap (.NET)/Export/CSharpFilters`
  Glavni obrazac za osnovne filtre. `Filters.cs` koristi `LockBits`, stride, offset i direktan pristup pikselima za invert, grayscale, brightness, contrast i gamma.

- `05 Filteri/Filters8Bit/CSharpFilters`
  Isti proceduralni pristup, ali sa ručnim radom nad 8-bit BMP formatom i paletama. `BitMapBytes.cs` pokazuje da je u materijalima prihvatljivo i očekivano ručno upravljanje sirovim bajtovima i zaglavljima.

- `05 Filteri/FiltersConvolution/Filters/CSharpFilters`
  Referentni stil za konvolucione i prostorne filtre. `FiltersBasic.cs`, `Filters.cs` i `FiltersDisplacement.cs` razdvajaju osnovne piksel operacije, matrice konvolucije i mapiranje koordinata za efekte kao što su smooth, edge i water.

- `06 JPEG postupak/UnsafeExample1`
  Minimalan primer `unsafe` rada. Bitno je što pokazuje da nizak nivo manipulacije memorijom nije izuzetak, već deo očekivanog stila.

- `06 JPEG postupak/UnsafeExample2`
  Još direktniji primer rada nad memorijom i pokazuje isti "manual first" pristup kao i bitmap vežbe.

- `06 JPEG postupak/UnsafeExample3`
  Učvršćuje isti obrazac: eksplicitna kontrola nad pokazivačima, bez skrivanja mehanike iza apstrakcija.

- `06 JPEG postupak/PS_Analyzer`
  Ručna parsiranja bitstream-a, stanje parsera i mehaničko čitanje bita po bit. Važno kao referenca za MSI deo koji takođe ručno formira i proverava binarni zapis.

- `06 JPEG postupak/TraceJPG`
  Ručno prolazi JPEG markere i segmente. Naglasak je na transparentnoj obradi formata, ne na apstraktnim bibliotekama.

- `06 JPEG postupak/MPEGBuilder1 Solution/BitmapImage`
  Najjači signal za binarne kodeke: pomoćne klase, eksplicitne tabele, ručno zapisivanje tokova, Huffman, DCT i nizak nivo kontrole nad reprezentacijom podataka.

- `06 JPEG postupak/MPEGBuilder1 Solution/MPEGBuilder1UI`
  UI je tanak sloj nad proceduralnim helperima. To je isti obrazac koji sada backend koristi: API ostaje tanak, a obrada slike je spuštena u pomoćne klase.

- `06 JPEG postupak/MPEGBuilder1 Solution/MPEGBuilder1`
  Orkestracija više helpera bez preterane apstrakcije. Bitno kao referenca za pipeline, ne za samu piksel logiku.

- `07 Histogram i WAVE/CPI/CPI.Audio`
  Ručno pisanje zaglavlja i stream-ova. Ovo je referenca za način na koji su MSI encoder i decoder strukturirani.

- `07 Histogram i WAVE/CPI/Examples`
  Primer kako se niski nivo helpera koristi iz jednostavne aplikacije bez komplikovanja osnovne logike.

- `07 Histogram i WAVE/mp3_stream_src`
  C/C++ primeri koji dodatno potvrđuju isti obrazac: ručni rad sa baferima, zaglavljima i stream-ovima.

- `07 Histogram i WAVE/AudioCD`
  Niskonivojske pomoćne klase i eksplicitna obrada audio podataka. Važno kao stilski paralelizam za format i bafer logiku.

- `07 Histogram i WAVE/AVI Player`
  Stariji MFC stil, ali ista ideja: proceduralni helperi i direktna kontrola nad formatom.

- `BitArrayDemo.cs`
  Mali pomoćni primer koji potvrđuje da su bitovske operacije očekivani deo ovih materijala.

## Kako je to preslikano u backend

- `src/KrLensServer/src/KrLensServer.Core/Filters/BitmapFilterBasic.cs`
  Grupisani osnovni filteri u jednom proceduralnom helperu, u stilu bitmap vežbi.

- `src/KrLensServer/src/KrLensServer.Core/Filters/BitmapFilterConvolution.cs`
  Konvolucije i matrice izvedene po uzoru na `FiltersConvolution`.

- `src/KrLensServer/src/KrLensServer.Core/Filters/BitmapFilterDisplacement.cs`
  Prostorni filteri sa eksplicitnim mapiranjem koordinata, po uzoru na `FiltersDisplacement.cs`.

- `src/KrLensServer/src/KrLensServer.Core/Filters/BitmapFilterTonal.cs`
  Tonalne transformacije odvojene od osnovnih filtera, umesto da budu razbacane kroz API sloj.

- Tanki `IFilter` wrapper-i
  API i pipeline zadržavaju postojeći ugovor prema frontendu, ali realna obrada sada živi u pomoćnim klasama bližim stilu sa vežbi.

- MSI encoder/decoder
  I dalje ostaju ručno strukturirani i mehanički proverljivi, što je najbliže JPEG/MPEG/WAVE materijalima.
