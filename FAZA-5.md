# Faza 5 — admin panel

Na `/admin`, samo za nalog sa ulogom Admin. Admin nema jezički prefiks u adresi, pa se jezik bira u bočnom meniju i pamti u kolačiću.

## Šta je novo

**Pregled** — promet, broj porudžbina i prosečna porudžbina za 30 dana, promet po danu, i šta čeka čoveka: porudžbine za potvrdu i slanje, stanje za dopunu, recenzije. Brojevi stoje i kao značke u meniju.

**Porudžbine** — lista sa statusima, pretraga po broju, imejlu, prezimenu ili telefonu. Detalj nudi samo korake koji su mogući iz trenutnog statusa:

| iz | može u |
|---|---|
| Primljena | Potvrđena, Otkazana |
| Potvrđena | Poslata (kurir + broj pošiljke), Otkazana |
| Poslata | Isporučena, Vraćena |
| Isporučena | Vraćena |

Uz svaki korak ide i sve što prati promenu statusa:
- **Otkazivanje** vraća robu na stanje i oslobađa iskorišćeni kupon.
- **Povraćaj** vraća robu na stanje po izboru — otvorena kozmetika obično ne može ponovo u prodaju.
- **Isporuka** porudžbinu plaćenu pouzećem označava kao plaćenu.
- Kupcu ide mejl o promeni, za sada samo u log.

Tu su još interna napomena, ručna promena statusa plaćanja i štampa otpremnice.

**Proizvodi** — lista sa filterima i forma: SKU, kategorija, brend, status, izdvajanje na početnoj, oba jezika (naziv, slug, opisi, sastav, održavanje, meta podaci) i atributi celog proizvoda, kao tip kože.
- Slug se pravi iz naziva ako ostane prazan.
- Objavljivanje traži bar jednu aktivnu varijantu.
- Kategorija se ne menja dok proizvod ima varijante, jer njihovi atributi zavise od nje.

**Varijante** — vrednosti atributa iz kategorije (veličina i boja, nijansa, zapremina), redovna i akcijska cena sa periodom po beogradskom vremenu, stanje, prag za dopunu, rok trajanja i težina.
- Dve varijante sa istim vrednostima se odbijaju.
- Poručivana varijanta se ne briše, samo isključuje.

**Fotografije** — otpremanje više fotografija odjednom, glavna fotografija, redosled, opis slike na oba jezika i vezivanje fotografije za boju ili nijansu.

**Stanje** — sve što je ispod praga, sa upisom prebrojanog broja.

**Kuponi** — procenat, iznos ili besplatna dostava; najveći popust, najmanja korpa, period, broj korišćenja i ograničenje na kategoriju. Korišćen kupon se ne briše, samo isključuje.

**Brendovi** i **vrednosti atributa** — nova boja ili nijansa ruža, sa uzorkom boje. Same atribute menja programer, jer se na njima grade filteri.

**Recenzije** — red za odobravanje iz faze 4 preseljen je u novi izgled.

## Greške pronađene i ispravljene

**1. Cena u adminu mogla je da bude sto puta veća.** MVC čita decimalne brojeve po jeziku zahteva. Na srpskom je tačka znak za hiljade, pa je `6990.50` — tačno ono što šalje polje za broj u pregledaču — postalo **699050**, bez ikakve greške. Provereno kroz pravi MVC pipeline:

| upisano | MVC (srpski) | sada |
|---|---|---|
| `6990.50` | 699050 | 6990,5 |
| `6.990,50` | 6990,50 | 6990,5 |
| `6,990.50` | greška | 6990,5 |
| `6 990` | greška | 6990 |

Rešeno sopstvenim binderom za decimalne brojeve (`FlexibleDecimalModelBinder`), koji važi za ceo sajt.

**2. Kupon „samo za kategoriju" važio je za celu korpu.** Kolona `RestrictedToCategoryId` postoji od faze 1, ali je `PricingService` nikad nije čitao. Sada popust ide samo na stavke iz te kategorije ili njenih potkategorija, a kupon se odbija ako takvih stavki nema. Testirano pravim `Calculate`:

| korpa: haljina 6990 + serum 5990 | popust |
|---|---|
| 10% na celu korpu | 1298 |
| 10% na Odeću (gornji nivo) | 699 |
| 10% na Kozmetiku | 599 |
| 9000 RSD na Haljine | 6990, ne više od vrednosti haljine |

**3. `@page` u `_Pagination.cshtml`.** Razor ga čita kao direktivu stranice, a ne kao promenljivu `page`. Zamenjeno sa `@(page)`, što je ispravno u svakoj verziji. Ako ti je build prolazio, ovo ništa ne menja.

## Zaštita od istovremenog rada

**Status porudžbine** se prvo „zauzima" uslovnim upitom `UPDATE … WHERE Status = <stari>`, pa tek onda menja stanje robe. Testirano na MariaDB sa dva admina koja istovremeno otkazuju istu porudžbinu: drugi čeka zaključan red, vidi da je porudžbina već pomerena i ne dira ništa. Roba je vraćena tačno jednom (5→7 i 5→6), a kupon oslobođen jednom.

**Stanje** se upisuje samo ako je još uvek broj koji je admin video. Ako u međuvremenu stigne porudžbina, upis se odbija sa porukom i novim brojem, umesto da tiho vrati prodat komad u prodaju. Testirano: admin je video 5, porudžbina je uzela 1, upis 12 je odbijen i ostalo je 4.

## Fotografije — šta treba znati

- Čuvaju se u `wwwroot/media/products/{id}/`, pod nasumičnim imenom.
- Tip se određuje po prvim bajtovima fajla, ne po imenu. Fajl `slika.jpg` koji je zapravo HTML ili SVG se odbija, jer bi inače mogao da izvrši skriptu na domenu prodavnice.
- Dozvoljeno: JPEG, PNG i WebP, do 5 MB po fotografiji, najviše 12 odjednom.
- **Pri objavljivanju na server sačuvaj `wwwroot/media`**, jer ga neki načini objavljivanja brišu. Ubaci ga i u `.gitignore`.
- Ne prave se umanjene verzije; za to treba biblioteka za slike, pa je ostavljeno za kasnije.
- Iza load balansera svi serveri moraju da vide isti folder. Jedino mesto koje bi se menjalo je `ProductImageService`.

## Baza

**Bez izmena šeme.** Sve tabele postoje od faze 1.

## Kako da probaš

1. Prijavi se kao `admin@jumbus.rs` i otvori `/admin`, ili klikni „Administracija" u zaglavlju.
2. **Proizvodi → Novi proizvod**: SKU, potkategorija, nazivi na oba jezika. Čuva se kao nacrt.
3. **Dodaj varijantu** — u polje za cenu upiši npr. `6990.50`, sačuvaj i otvori varijantu ponovo. Polje mora da pokazuje 6990.5, a ne 699050.
4. Otpremi fotografije, postavi status na Objavljen i proveri proizvod u prodavnici.
5. Poruči ga, pa u **Porudžbine** prođi kroz Potvrdi → Poslato (sa brojem pošiljke) → Isporučeno. Kod pouzeća plaćanje postaje „plaćeno".
6. Napravi još jednu porudžbinu i otkaži je, pa u **Stanje** proveri da se roba vratila.
7. Napravi kupon samo za Kozmetiku i proveri u korpi da popust ide samo na kozmetiku.

**Pre puštanja u rad promeni admin lozinku** (`/sr/account/password`), jer je `Admin123!` zapisan u dokumentaciji.

## Kako je provereno

Ovog puta je kod **kompajliran**. Ubuntu arhiva sada nudi .NET 10 SDK, pa je kompajliran sav kod, svi `.cs` i `.cshtml` fajlovi, protiv pravog ASP.NET Core 10 frameworka. NuGet je ovde blokiran, pa je samo EF Core API zamenjen kodom koji ima iste potpise. Kompajler je našao dve greške pre isporuke: `@page` iz faze 2 i istu zamku sa `@attribute` u novom kodu.

Pravi kontroleri i pogledi su zatim pokrenuti u pretraživaču, sa lažnim podacima umesto baze:
- **23 admin stranice × srpski i engleski × 1440 i 390 px.** Bez neprevedenih ključeva, JS grešaka, neuspelih zahteva i horizontalnog prelivanja.
- **Slanje formi:** greške sa servera stoje pored pravog polja, obavezne vrednosti atributa proveravaju se već u pretraživaču, a konflikt stanja ažurira skriveni broj.
- **Bezbednost:** POST bez antiforgery tokena dobija 400.
- **Promena jezika** radi.

Pretraživač je usput uhvatio jedan bug u novom kodu, ispravljen pre isporuke: Razor na `<form>` elementu piše `data-confirm=""` umesto da izostavi atribut, pa je „Označi kao poslato" otvaralo prazan prozor za potvrdu.

## Šta nije provereno

**LINQ upiti u admin servisima nisu izvršeni nad pravom bazom**, jer bez EF Core paketa ne može. Napisani su u istom obliku kao upiti iz faza 2–4 koji kod tebe rade. Ako neka admin stranica pukne pri prvom otvaranju, pošalji grešku — biće to prevođenje upita, a ne logika.

## Svesno odloženo

- **Kategorije**: nova kategorija traži prevode, slug-ove, dodelu atributa i preusmerenja starih adresa. Retko se menjaju, pa idu uz fazu 6.
- **Preusmerenje posle promene slug-a** objavljenog proizvoda — faza 6 (SEO). Forma za sada upozorava.
- Umanjene verzije fotografija, uloge sa manjim pravima (npr. samo porudžbine), dnevnik izmena, izvoz porudžbina i lista kupaca.
