# Redizajn početne strane

## Šta je bio problem

Ekran koji si poslao imao je pet konkretnih stvari, ne „boje":

1. **Hero je bio 60% prazan.** Naslov se prelamao na tri reda uz levu ivicu,
   desna polovina je bila prazna, ništa nije držalo pogled.
2. **Kartice su bile roze pravougaonici od 386 px.** Placeholder se pravio da
   fotografija samo što nije stigla i ostavljao rupu.
3. **Debug panel je stajao na najvrednijem mestu** na stranici.
4. **Sve je bilo jedne vrednosti** — bele kartice na skoro beloj podlozi sa
   linijama od 1 px. Otuda utisak ispranosti.
5. **Traka kategorija nije razlikovala** nadkategoriju od podkategorije.

## Šta je promenjeno i zašto

Tekst na sajtu već govori pozicioniranje: izmerene veličine, pun sastav, bez
filtera. To je **transparentnost, ne luksuz.** Bodoni Moda sa mnogo praznog
prostora imitira luksuznu butik-estetiku i radi protiv sopstvenog teksta.

Novi pravac je **precizan katalog**:

- **Tipografija.** Newsreader (uredničke serifne novine — drži težinu na
  velikim veličinama gde didone postaje tanak kao žica) + Archivo za
  interfejs i podatke. Oba imaju latin-ext, pa č ć ž š đ rade.
- **Jedna glasna zona.** Hero je tamni blok boje šljive. Sve ispod je tiho.
- **Placeholder rešen podacima.** Umesto prazne rupe, kartica prikazuje
  stvarne boje, nijanse i zapremine iz tabele varijanti. Informiše sada,
  a kad stignu fotografije samo se skloni.
- **Hero desno** prikazuje pravi raspon veličina i sve nijanse iz baze —
  tvrdnja iz naslova potkrepljena podatkom, ne ukrasom.
- **Debug panel** se sada vidi samo u Development modu.

## Izmenjeni fajlovi

```
wwwroot/css/site.css                              napisan iznova
Views/Shared/_Layout.cshtml                       napisan iznova
Views/Home/Index.cshtml                           napisan iznova
Views/Shared/Components/CategoryNav/Default.cshtml napisan iznova
Views/Shared/Error.cshtml                         nove klase
Views/Shared/ComingSoon.cshtml                    nove klase
Areas/Admin/Views/Dashboard/Index.cshtml          nove klase
Controllers/HomeController.cs                     nove projekcije
ViewModels/Home/HomeIndexViewModel.cs             napisan iznova
Infrastructure/ShopSettings.cs                    + ReturnDays
appsettings.json                                  + ReturnDays
Resources/SharedResource.resx                     + 15 ključeva
Resources/SharedResource.sr.resx                  + 15 ključeva
```

Baza nije dirana. SQL skripte ostaju iste.

## Provereno

Renderovano kroz headless Chromium sa pravim `site.css`:

- 1440 px, 768 px i 390 px — nigde horizontalnog prelivanja
- srpski i engleski — duži srpski stringovi ne lome raspored
- dijakritika se ispisuje ispravno u oba pisma

## Ostaje za kasnije

Fotografije. Kad admin upload proradi u fazi 5, `<img>` u `.shot` bloku
preuzima mesto, a čipovi se spuštaju u telo kartice. CSS je već tako
napisan — `.shot img` je apsolutno pozicioniran preko čipova.
