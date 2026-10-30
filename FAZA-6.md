# Faza 6 — lokalizacija, SEO i pravne stranice

Poslednja faza iz plana. Uz SEO i lokalizaciju donosi i ono što je iz faza 4 i 5 ostavljeno za nju: pravne stranice, pristanak na kolačiće, brisanje naloga, kategorije u adminu i preusmerenje starih adresa.

## Pre puštanja u rad

1. **Podaci firme** — upiši ih u `appsettings.json`, odeljak `Shop:Company`. Sada stoje u uglastim zagradama, a vide se u footeru, na pravnim stranicama, u obrascu za odustanak i u podacima za Google.
2. **Javna adresa** — `Shop:BaseUrl`, npr. `https://www.jumbus.rs`. Bez nje se adrese prave od trenutnog zahteva, što iza proxy-ja često ispadne `http://` sa internim imenom.
3. **Pravnik** — tekstovi u `Views/Legal/*.sr.cshtml` prate Zakon o zaštiti potrošača (88/2021) i ZZPL (87/2018), ali su nacrt. Ostavio sam napomene na mestima koja zavise od tvoje odluke: ko plaća povratnu dostavu, rokovi slanja, spisak obrađivača (hosting, knjigovodstvo).
4. **Admin lozinka** — i dalje `Admin123!` iz dokumentacije.

## Šta je novo

**SEO**
- Canonical, hreflang za sr, en i `x-default`, i Open Graph za pregled linka na mrežama. Proizvod šalje svoju fotografiju i tip `product`.
- Strukturirani podaci (JSON-LD):
  - proizvod sa cenom i zalihama po varijanti i sa ocenom iz recenzija;
  - putanja Početna › Odeća › Haljine na kategoriji i proizvodu;
  - podaci o prodavnici na početnoj.

  Prave se kao podaci, ne kao tekst u Razor-u, pa ih naziv sa navodnicima ili `</script>` ne može pokvariti. To je i provereno.
- `/sitemap.xml` — početna, katalog, aktivne kategorije, objavljeni proizvodi i informativne stranice. Uz svaku adresu ide i njena verzija na drugom jeziku.
- `/robots.txt` — isključuje admin, korpu, plaćanje, nalog i porudžbine, i pokazuje na sitemap.
- **noindex** važi za korpu, plaćanje, nalog, adrese, porudžbine, listu želja i stranice grešaka, bez obzira na pogled. Takve stranice nemaju canonical, da Google ne dobija suprotne signale.
- Kategorije dobijaju meta naslov i opis iz admina.

**Stara adresa vodi na novu (301).** Kad promeniš slug objavljenog proizvoda ili kategorije, stara adresa trajno preusmerava na novu. Linkovi iz Google-a, sa mreža i iz sačuvanih stranica nastavljaju da rade. Ako slug vratiš na stari, preusmerenje se samo briše. Napomena ispod polja za slug u adminu sada to i kaže, umesto upozorenja da će linkovi pući.

**Stranica 404** — u dizajnu prodavnice, na jeziku adrese, sa pretragom. Status ostaje 404, pa je Google ne indeksira. Ista stranica pokriva i 400 i 429 (previše pokušaja).

**Kategorije u adminu** (Podešavanja → Kategorije). Svaka kategorija ima naziv, slug, opis i meta podatke na oba jezika, redosled, aktivnost i atribute (koji važe, koji su obavezni, kojim redom). Pravila čuvaju katalog u obliku koji ostatak prodavnice očekuje:
- samo dva nivoa, a proizvodi samo u potkategorijama;
- kategorija sa objavljenim proizvodima ne može da se isključi, jer bi proizvodi ostali dostupni preko linka, a nestali iz menija;
- atribut koji varijante već koriste ne može da se ukloni;
- slug ne može da bude reč koju prodavnica već koristi (`cart`, `account`…);
- briše se samo prazna kategorija: bez proizvoda, potkategorija i kupona;
- kod (interni naziv) ne menja se posle prvog čuvanja.

**Pravne stranice**, svaka na srpskom i engleskom u posebnom fajlu:
- Uslovi korišćenja i prodaje
- Politika privatnosti
- Kolačići
- Reklamacije i povraćaj: odustanak za 14 dana, povraćaj novca, izuzetak za otvorenu kozmetiku, rokovi od 8 i 15 dana za reklamacije, vansudsko rešavanje sporova
- Obrazac za odustanak — u obliku iz pravilnika (Sl. glasnik 21/2022), sa upisanim podacima prodavca, za štampu ili PDF
- Dostava i plaćanje
- Kontakt
- Tabela veličina — ima je i stranica proizvoda, ispod izbora veličine

**Kolačići** — baner nudi „Samo neophodni" i „Prihvati sve", podjednako vidljive, a izbor se menja na stranici Kolačići.
- Prodavnica postavlja samo neophodne kolačiće, i svi su tako i označeni. „Prihvati sve" zato za sada ništa ne menja.
- ASP.NET-ov cookie policy odbacuje svaki budući kolačić koji nije označen kao neophodan, dok posetilac ne prihvati sve.
- Analitika, ako je ikad dodaš, ide u blok `canTrack` na kraju `_Layout.cshtml`.

**Fontovi sa sopstvenog servera.** Archivo i Newsreader se više ne učitavaju sa Google Fonts, pa otvaranje stranice ne šalje IP adresu posetioca Google-u. Fontovi su isti; licenca (OFL) je u `wwwroot/fonts`.

**Moji podaci** (u nalogu):
- preuzimanje svih podataka kao JSON fajl;
- brisanje naloga, uz lozinku i potvrdu.

Brisanjem odlaze nalog, adrese, lista želja i recenzije. Porudžbine ostaju, bez veze sa nalogom, jer ih propisi o računovodstvu i reklamacijama traže. Administratorski nalog ne može da se obriše odavde.

## Greška pronađena i ispravljena

**Recenzije i adrese nisu radile.** Rute kataloga (`/sr/haljine`, `/sr/haljine/vera`) registrovane su pre opšte rute. Zato je svaka adresa od dva ili tri dela išla na katalog, osim ako je kontroler na spisku rezervisanih reči u `CatalogSlugConstraint`. Na spisku nisu bili `addresses` ni `reviews`, pa je 404 vraćalo:
- slanje recenzije (`/sr/reviews/submit`);
- lista adresa, nova adresa i čuvanje adrese.

Izmena i brisanje adrese su radili, jer im adresa ima četiri dela.

Provereno pre i posle izmene: sa starim spiskom POST na `/sr/reviews/submit` i `/sr/addresses/save` vraća 404, a sa novim stiže do pravog kontrolera. Na spisak su dodati i novi kontroleri (`status`, `seo`). Pravilo je zapisano u komentaru klase: **svaki novi kontroler mora na taj spisak.**

## Baza

`sql/05-faza6.sql` — nova tabela `SlugRedirects`. Pusti je jednom u Workbench-u; može i više puta. `01-schema.sql` je već ima, za novu instalaciju.

## Kako da probaš

1. Pusti `sql/05-faza6.sql`, pa pokreni projekat.
2. Obriši kolačiće za localhost. Baner se pojavljuje; „Samo neophodni" ga sklanja.
3. Otvori `/sr/legal/returns` i `/sr/legal/withdrawalform`, pa dugmetom odštampaj obrazac.
4. Otvori `/sr/ovo-ne-postoji`, pa `/en/ovo-ne-postoji`.
5. Otvori `/sitemap.xml` i `/robots.txt`.
6. **Admin → Kategorije → Haljine:** promeni srpski slug u `haljine-i-tunike` i sačuvaj. `/sr/haljine` sada mora da preusmeri na novu adresu, kao i link na haljinu sa starim delom adrese. Posle vrati slug na `haljine`.
7. Isto probaj sa slug-om objavljenog proizvoda.
8. Napiši recenziju i dodaj adresu — sada rade.
9. **Nalog → Moji podaci:** preuzmi JSON, pa obriši probni nalog (ne admin).
10. Kad sajt bude javan, proveri proizvod na [Rich Results Test](https://search.google.com/test/rich-results).

## Kako je provereno

- **Kompajliranje:** ceo projekat, svi `.cs` i `.cshtml`, protiv .NET 10, sa EF stub-om kao ranije. Razor je opet uhvatio zamku: promenljiva `page` u petlji čita se kao direktiva `@page`. Ispravljeno pre isporuke.
- **Pretraživač:** pravi kontroleri i pogledi, sa lažnim podacima. Prošle su stranice prodavnice (14 na srpskom, 12 na engleskom) i 5 admin stranica, na 1440 i 390 px:
  - nema prelivanja, neprevedenih ključeva ni JS grešaka;
  - nema nijednog zahteva ka drugom domenu;
  - svaki JSON-LD se ispravno čita, a naziv sa `"` i `</script>` ostaje unutar podataka;
  - 404 je na jeziku adrese i sa statusom 404;
  - stare adrese daju 301;
  - sitemap je ispravan XML;
  - izbor kolačića radi u oba smera;
  - greške u formi kategorije stoje pored pravih polja.
- **Baza:** `05-faza6.sql` pušten dvaput na MariaDB; provereni su jedinstveni indeks i upit za staru adresu.

## Šta nije provereno

- **Upiti novih servisa** (kategorije, sitemap, izvoz podataka) nisu izvršeni nad pravom bazom, jer ovde nema EF paketa. Pisani su u istom obliku kao upiti iz faza 2–5 koji kod tebe rade. Ako neka stranica pukne, pošalji grešku.
- **Brisanje naloga i izvoz** nisu pokrenuti, jer nema Identity baze. Brisanje se oslanja na strane ključeve iz šeme: adrese, recenzije i lista želja `ON DELETE CASCADE`, porudžbine `SET NULL`.
- **Pravni tekstovi** nisu pravni savet. Pregled pravnika je obavezan.

## Šta ostaje posle plana

- Početna stranica i stranica proizvoda u novom dizajnu.
- Pravi mejlovi umesto loga.
- Pravo plaćanje karticom (NestPay ili WSPay).
- Umanjene verzije fotografija.
- Uloge sa manjim pravima i dnevnik izmena u adminu.
