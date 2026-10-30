# Faza 2 — katalog

## Prvo: ispravka iz faze 1

Pre nego što sam krenuo na katalog, našao sam grešku u SQL skriptama.

Skripte nisu deklarisale svoje kodiranje. Klijent čiji je podrazumevani
charset `latin1` je UTF-8 bajtove tumačio kao latin1 i dvostruko ih kodirao
pri upisu — `čišćenje` je u bazi postajalo `ÄiÅ¡Ä‡enje`. Tiho, bez greške.

Obe skripte sada počinju sa `SET NAMES utf8mb4;`. Testirao sam ih namerno
pokrenute sa latin1 klijentom — tekst je ispravan.

**Proveri svoju bazu:**

```sql
SELECT Name FROM ProductTranslations WHERE Culture='sr' AND Id=27;
```

Treba da vrati `Uljni gel za čišćenje`. Ako vidi `ÄiÅ¡Ä‡enje`, pusti
obe skripte ponovo — nove verzije su u `sql/`.

## Pretraga i dijakritika

Dobra vest: kad je tekst ispravno upisan, `utf8mb4_unicode_ci` sam izjednačava
č, ć, š i ž sa c, s i z. Pretraga za „ciscenje" nalazi „čišćenje" bez ikakve
dodatne kolone. Testirao sam.

Jedini izuzetak je `đ` — Unicode ga tretira kao zasebno slovo, pa se ne svodi
ni na `d` ni na `dj`. `SearchTerms.cs` to pokriva tako što uz originalni
pojam traži i prepisanu varijantu.

## Šta je napravljeno

**Rute sa prevedenim slug-ovima**

```
/sr/haljine                        kategorija
/sr/haljine/koktel-haljina-vera    proizvod
/en/dresses/vera-cocktail-dress    isti proizvod na engleskom
/sr/products?q=krema&color=black   pretraga i filteri
```

Kategorije i proizvodi imaju prevedene slug-ove; funkcionalne stranice
(`/sr/cart`, `/sr/checkout`) ostaju engleske, kao što smo se dogovorili.

`CatalogSlugConstraint` sprečava sudar: `/sr/haljine` i `/sr/cart` su oba dva
segmenta, pa bi bez ograničenja `cart` bio kandidat za slug kategorije.

Proizvod otvoren pod pogrešnom kategorijom radi, ali uz 301 na pravu adresu —
jedna stranica nikad ne stoji na dve URL adrese.

**Filteri**

`ProductFilterService` sastavlja upit. Po jedna klauzula za svaki atribut,
spojene sa AND; opcije unutar jednog atributa idu sa OR. Crna i teget plus
veličina M znači „postoji u crnoj ili teget, i postoji u M".

Filter panel nudi samo atribute koje kategorije u prikazu zaista koriste —
na kozmetici se ne pojavljuje filter za veličinu haljine. To dolazi iz
`CategoryAttributes`, iste tabele koja vodi i admin formu.

Panel je običan GET formular i radi bez JavaScripta. `catalog.js` samo
uklanja potrebu za dugmetom i onda ga sakrije. Na uskim ekranima se panel
skuplja da ne gura proizvode ispod ekrana.

**Stranica proizvoda**

Variant picker radi lokalno — sve varijante su već na stranici kao JSON, pa
promena veličine nije novi zahtev ka serveru. Opcije do kojih se ne može doći
iz trenutnog izbora se precrtavaju, ne sakrivaju: korisno je videti da
veličina postoji ali je rasprodata.

Tabela svih varijanti sa šiframa, cenama i stanjem stoji ispod opisa.

JSON-LD Product schema je na stranici, sa cenama u invariant formatu —
schema.org očekuje decimalnu tačku, srpski zarez bi učinio ponudu neupotrebljivom.

## Provereno

- Obe SQL skripte pokrenute sa latin1 klijentom, tekst ispravan
- Katalog, kategorija i dve stranice proizvoda renderovane iz **žive baze**
  kroz prave upite, sa pravim `site.css`
- 1440 px i 390 px — nema horizontalnog prelivanja
- Filter panel za „Haljine" nudi veličinu i boju, bez zapremine i nijanse
- Svi ključevi prevoda postoje na oba jezika, placeholderi se poklapaju
- Nema sukoba `asp-page` / `asp-controller` ni u jednom fajlu

**Nije kompajlirano** — nemam .NET SDK u okruženju, Microsoftovi hostovi su
blokirani. C# je pregledan ručno.

## Ostaje

- Dodavanje u korpu je dugme bez funkcije do faze 3
- Recenzije su faza 4
- Tabela mera po veličini traži podatke kojih u šemi još nema
- Filter po zapremini je numerički, pa nema opcije za čekiranje — pokriva ga
  raspon cena dok faza 5 ne doda numeričke opsege
