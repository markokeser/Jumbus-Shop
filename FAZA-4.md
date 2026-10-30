# Faza 4 — nalog kupca

## Šta je novo

**Prijava i registracija** na sopstvenim stranicama (`/sr/account/login`, `/sr/account/register`), na srpskom i engleskom, u dizajnu sajta. Stranice iz paketa ASP.NET Identity imaju engleski tekst ugrađen u paket i ne mogu se prevesti kroz naše `.resx` fajlove — ranija beleška u projektu je pogrešno tvrdila da mogu. Identity servisi ispod (heširanje lozinki, zaključavanje, tokeni) ostaju isti.

**Zaboravljena lozinka** sa linkom za resetovanje. Mejlovi se još ne šalju: u Development okruženju link se prikazuje na ekranu i u logu; u produkciji se ne loguje, jer je token u logu isto što i lozinka.

**Moj nalog**: pregled, lični podaci, promena lozinke, porudžbine sa trakom statusa (primljena → potvrđena → poslata → isporučena), adresar sa glavnom adresom, lista želja.

**Praćenje porudžbine za goste** (`/sr/orders/track`) — broj porudžbine i imejl moraju oba da se poklope.

**Recenzije** sa moderacijom. Oznaka „potvrđena kupovina" samo za isporučene porudžbine. Autor se prikazuje kao „Marko P.", nikad imejl. Red za odobravanje je na `/admin/reviews` — jedini deo admin panela koji nije mogao da čeka fazu 5, jer bez njega nijedna recenzija ne bi bila objavljena.

**Checkout** popunjava podatke iz naloga i glavne adrese.

**Politika lozinki**: najmanje 8 znakova i jedna cifra. Greške Identity-ja su prevedene.

**Ograničenje zahteva**: 10 u minuti po IP adresi za prijavu, registraciju, reset lozinke i praćenje porudžbine. Ugrađeno u ASP.NET, bez paketa.

## Greške iz faze 3 pronađene i ispravljene

**1. Porudžbina nije mogla da se pošalje.** Polje „prihvatam uslove" koristilo je `[Range(typeof(bool), "true", "true")]`. MVC upisuje granice kao `"True"`, jQuery ih pretvara sa `Number("True")` = `NaN`, i ništa nije ≥ NaN. Testirano sa pravim jquery-validation 1.21.0 u Chromium-u:

| | pre | posle |
|---|---|---|
| neštiklirano | pregledač pušta, server odbija | pregledač blokira sa porukom |
| štiklirano | **pregledač blokira** | šalje se |

Zamenjeno sopstvenim `MustBeTrueAttribute` + `wwwroot/js/validation-extras.js`.

**2. Uslovne greške su bile nevidljive.** Kad fali PIB ili adresa računa, greška se upisivala pod `CompanyTaxNumber`, a pogled je traži pod `Form.CompanyTaxNumber`. Forma se vraćala bez objašnjenja. Uz to, poruka bi bila doslovno `Validation_Required` — ručno dodate greške ne prolaze kroz prevod.

**3. Porudžbine gostiju su bile javne.** Stranica potvrde prikazivala je gostujuću porudžbinu svakome ko zna broj, a brojevi su redni. Sada je vidi samo vlasnik naloga ili sesija pregledača koja ju je poslala; gosti kasnije koriste praćenje (broj + imejl).

## Takođe ispravljeno

**Jezik se sada pamti u kolačiću.** Obećano u fazi 1, nikad urađeno. Bez toga bi stranice bez jezičkog prefiksa (preusmerenje na prijavu, admin) bile na jeziku pregledača umesto na izabranom.

**Linkovi u zaglavlju i podnožju** sada eksplicitno navode jezik. Na stranicama bez jezika u ruti (admin panel) nije bilo odakle da ga pozajme i izlazili su prazni.

**Putanja validacione skripte.** Noviji Visual Studio šabloni stavljaju je u `dist/`, stariji ne; pogrešna putanja tiho gasi svu klijentsku validaciju. `_ValidationScriptsPartial` sada bira onu koja postoji.

## Baza

**Bez izmena šeme.** Sve tabele za fazu 4 postoje od faze 1.

Jedna napomena: jedinstveni indeks liste želja ne sprečava duplikate kad je varijanta `NULL` — MySQL tretira svaki NULL kao različit. Testirano na MariaDB (dva ista reda prošla). Zaštita je u kodu.

## Kako da probaš

1. Registruj nalog, poruči nešto, otvori **Nalog → Moje porudžbine**.
2. Pomeri porudžbinu kroz statuse (dok admin panel ne stigne):

   ```sql
   UPDATE Orders SET Status = 2, ConfirmedUtc = UTC_TIMESTAMP(), ShippedUtc = UTC_TIMESTAMP(),
          CourierName = 'D Express', TrackingNumber = 'DE123456789RS'
   WHERE OrderNumber = '2026-000001';

   UPDATE Orders SET Status = 3, DeliveredUtc = UTC_TIMESTAMP()
   WHERE OrderNumber = '2026-000001';
   ```

   Status: 0 primljena, 1 potvrđena, 2 poslata, 3 isporučena, 4 otkazana, 5 vraćena.
3. Posle statusa 3 napiši recenziju tog proizvoda — dobija oznaku „potvrđena kupovina".
4. Prijavi se kao `admin@jumbus.rs` i odobri je na `/admin/reviews`.
5. Odjavi se, pa na **Praćenje porudžbine** u podnožju unesi broj i imejl.
6. **Zaboravljena lozinka** — u Development-u link se pojavi na sledećoj stranici.

## Šta nije provereno

Kod nije kompajliran — u ovom okruženju nema .NET SDK-a. Provereno je: validacija u pravom pregledaču sa pravim bibliotekama, SQL logika na MariaDB (potvrđena kupovina, NULL u indeksu, promena statusa), statički pregled (ključevi prevoda u oba jezika uključujući one iz enuma, pogledi, partial-i, DI registracije, klase bez stila), i render novih ekrana sa pravim CSS-om na 1440 i 390 px. Ako build prijavi grešku, pošalji je.

## Svesno odloženo

- **Brisanje naloga** (pravo na brisanje po ZZPL) — faza 6, zajedno sa pravnim stranicama i pristankom na kolačiće.
- **Promena imejla** — traži potvrdni mejl.
- **Spajanje ranijih gostujućih porudžbina sa novim nalogom** — namerno ne: imejl nije potvrđen, pa bi neko mogao da se registruje tuđim imejlom i čita tuđe porudžbine.
- **Pravi mejlovi** — `IAccountEmailSender` i `IOrderEmailSender` su spremni za zamenu.
- **Ograničenje zahteva iza proksija** — tada svi zahtevi dolaze sa adrese proksija; treba podesiti forwarded headers.
