# Jumbus

Online prodavnica ženske garderobe i kozmetike, na srpskom i engleskom.

ASP.NET Core MVC (.NET 10) · EF Core 9 + Pomelo · MySQL 8 · ASP.NET Identity

## Šta ima

- **Katalog** sa filterima i varijantama (veličina, boja, nijansa, zapremina), korpa i za goste, plaćanje pouzećem (kartica je za sada probna).
- **Nalog**: porudžbine, adrese, lista želja, recenzije, preuzimanje podataka i brisanje naloga.
- **Admin** na `/admin`: porudžbine, proizvodi, varijante, fotografije, stanje, kuponi, kategorije, brendovi, atributi i recenzije.
- **SEO**: adrese sa prevedenim slug-ovima (`/sr/haljine`, `/en/dresses`), hreflang, Open Graph, JSON-LD, `sitemap.xml` i preusmerenje starih adresa.
- **Pravne stranice** po Zakonu o zaštiti potrošača i ZZPL, uz pristanak na kolačiće.

## Pokretanje

Potrebni su .NET 10 SDK i MySQL 8 (npr. sa Workbench-om).

1. **Baza.** Iz foldera `sql` u projektu pusti redom:
   - `01-schema.sql` — šema (briše i pravi bazu `jumbus` iznova);
   - `02-seed.sql` — kategorije, 22 proizvoda i admin nalog;
   - `04-sample-images.sql` — ilustracije proizvoda.
2. **Lozinka baze ne ide u `appsettings.json`.** Desni klik na projekat → **Manage User Secrets**, pa upiši:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Port=3306;Database=jumbus;User Id=root;Password=TVOJA_LOZINKA;CharSet=utf8mb4;"
     }
   }
   ```
3. Pokreni projekat (F5). Prodavnica je na `/sr`, a admin na `/admin`.
   - Admin nalog je `admin@jumbus.rs` / `Admin123!`. Lozinku promeni odmah.

Detalji su u `UPUTSTVO-MYSQL.md`, a šta je rađeno u kojoj fazi u `FAZA-2.md` … `FAZA-6.md`.

## Pre objavljivanja sajta

- U `appsettings.json` popuni `Shop:Company` (podaci firme) i `Shop:BaseUrl` (javna adresa).
- Daj pravniku da pregleda tekstove u `Views/Legal/` — srpska verzija je u `*.sr.cshtml`.
- Promeni admin lozinku.
- Fotografije iz admina (`wwwroot/media/products/{id}/`) nisu u repozitorijumu. Pravi im backup posebno, kao i bazi.

## Raspored

| folder | šta je u njemu |
|---|---|
| `Areas/Admin` | admin panel |
| `Controllers`, `Views`, `ViewModels` | prodavnica |
| `Services` | sva logika (kontroleri je samo pozivaju) |
| `Data` | DbContext i mapiranje; šema je u `sql/`, ne u EF migracijama |
| `Resources` | prevodi interfejsa (`.resx`, sr i en) |
| `Infrastructure` | rute po jeziku, kolačići, SEO pomoćnici |
| `sql` | šema, početni podaci i dopune po fazama |
