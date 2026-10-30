# Jumbus — postavljanje (MySQL)

ASP.NET Core 10 MVC, EF Core 9 + Pomelo, MySQL, dvojezično (sr / en).

Šema se pravi SQL skriptom, ne EF migracijama. Skripte su u `sql/`.

---

## 1. Baza

```bash
mysql -u root -p < sql/01-schema.sql
mysql -u root -p < sql/02-seed.sql
```

Iz MySQL Workbench: otvori fajl, pa Execute (munja). Prvo `01`, pa `02`.

`01-schema.sql` počinje sa `DROP DATABASE IF EXISTS jumbus` — briše sve i
pravi iznova. `02-seed.sql` počinje `TRUNCATE`-ovima, pa se može puštati
više puta bez duplikata.

Ostale skripte su dopune za bazu koja već postoji, i svaka može da se
pusti više puta:

| skripta | šta radi |
|---|---|
| `04-sample-images.sql` | po jedna ilustracija za 22 proizvoda iz seed-a, vezana po SKU-u (radi i čišćenje iz `03`) |
| `05-faza6.sql` | tabela `SlugRedirects` za preusmerenje starih adresa; nova baza iz `01` je već ima |

Provera:

```sql
USE jumbus;
SELECT (SELECT COUNT(*) FROM Products)   AS Proizvodi,
       (SELECT COUNT(*) FROM ProductVariants) AS Varijante,
       (SELECT COUNT(*) FROM Users)      AS Korisnici;
```

Očekivano: 22, 141, 1.

---

## 2. Connection string

U `appsettings.json` zameni `CHANGE_ME` svojom root lozinkom:

```json
"DefaultConnection": "Server=localhost;Port=3306;Database=jumbus;User Id=root;Password=TVOJA_LOZINKA;CharSet=utf8mb4;"
```

Bolje nego root: napravi zaseban nalog samo za aplikaciju.

```sql
CREATE USER 'jumbus_app'@'localhost' IDENTIFIED BY 'jaka-lozinka-ovde';
GRANT SELECT, INSERT, UPDATE, DELETE ON jumbus.* TO 'jumbus_app'@'localhost';
FLUSH PRIVILEGES;
```

Lozinka ne treba da stoji u `appsettings.json` koji ide u git. Za razvoj:
desni klik na projekat u Visual Studiju → **Manage User Secrets**, pa tamo:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=jumbus;User Id=jumbus_app;Password=jaka-lozinka-ovde;CharSet=utf8mb4;"
  }
}
```

---

## 3. Zašto EF Core 9, a ne 10

Pomelo je MySQL provajder za EF Core. Poslednja objavljena verzija je
**9.0.0** i njena NuGet zavisnost je
`Microsoft.EntityFrameworkCore.Relational >= 9.0.0 && <= 9.0.999`.
Neće se učitati uz EF Core 10 — pukne pri startu, ne pri build-u.

Zato `WebShop.csproj` cilja `net10.0`, a EF pakete drži na `9.0.x`.
EF Core 9 radi bez problema na .NET 10 runtime-u. Kad Pomelo objavi
10.0.0, prebaci svih pet paketa na `10.0.x` odjednom.

Ako baš hoćeš EF Core 10 sada, postoji fork `Microting.EntityFrameworkCore.MySql`
koji objavljuje `10.0.x` verzije. Nije zvanični Pomelo — proceni sam da li
ti odgovara zavisnost od forka.

---

## 4. Pokreni

```bash
dotnet restore
dotnet run
```

Ili F5 u Visual Studiju.

Prijava: `admin@jumbus.rs` / `Admin123!`

**Promeni ovu lozinku odmah po prvoj prijavi.** Stoji u `02-seed.sql` u
fajlu koji će završiti u gitu.

---

## 5. Provera da sve radi

| Šta | Kako |
|---|---|
| Baza | `/` — panel prikazuje 8 kategorija, 22 proizvoda, 141 varijantu |
| Preusmeravanje jezika | `/` vodi na `/sr` |
| Prebacivanje jezika | Klik „English" → `/en`, ceo interfejs se menja |
| Formatiranje | Cene na `/sr` su `8.990 RSD`, na `/en` `8,990 RSD` |
| Ime shopa | Wordmark u header-u je „Jumbus" (iz `Shop:Name`) |
| Admin | Prijava, pa `/admin` |

---

## 6. EF migracije — namerno preskočene

`SeedOnStartup` je `false`, a seeder više ne poziva `MigrateAsync`.
Šema dolazi iz SQL-a.

Ako kasnije ipak hoćeš EF migracije, tabela `__EFMigrationsHistory` već
postoji i prazna je. Postupak:

```bash
dotnet ef migrations add InitialCreate
```

Zatim uporedi generisani `Up()` sa `01-schema.sql`. Ako se poklapa, upiši
red u istoriju da EF ne pokuša da pravi tabele ponovo:

```sql
INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260913090000_InitialCreate', '9.0.0');
```

`MigrationId` mora tačno da odgovara imenu generisanog fajla.

---

## 7. Razlike u odnosu na SQL Server verziju

| | SQL Server | MySQL |
|---|---|---|
| `DateTime` | `datetime2` | `datetime(6)` |
| `bool` | `bit` | `tinyint(1)` |
| `DateOnly` | `date` | `date` |
| Identity string PK | `nvarchar(450)` | `varchar(255)` |
| Kaskade | ograničenje na višestruke putanje | bez tog ograničenja |
| Poređenje stringova | neosetljivo na velika slova | `utf8mb4_unicode_ci`, isto |

Collation `utf8mb4_unicode_ci` znači da pretraga ne razlikuje velika i
mala slova, što je ponašanje koje si imao i na SQL Serveru.

Jedna stvar na koju treba paziti: `utf8mb4_unicode_ci` **ne izjednačava**
`č` i `c`. Pretraga za „ciscenje" neće naći „čišćenje". Zato slug-ovi u
bazi već stoje bez dijakritike (`uljni-gel-za-ciscenje`). Za pretragu po
nazivu u fazi 2 dodaću isti tretman.

---

## 8. Pre nego što ovo ode na internet

- Promeni admin lozinku
- Napravi zaseban MySQL nalog, ne koristi root
- Sesija je u memoriji — iza load balansera korpe gostiju nestaju,
  tada ide Redis ili MySQL distribuirani keš (mesto označeno u `Program.cs`)
- Pravne stranice su prazne; tekst mora da pregleda pravnik
