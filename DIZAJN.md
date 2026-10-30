# Redizajn kataloga i ispravke

## Jednom pokreni u Workbench-u

`sql/03-remove-placeholder-images.sql` — briše probne putanje fotografija iz prvog seed-a (fajlovi nikad nisu postojali, pa je svaka kartica imala pokvarenu sliku). Fotografije otpremljene kroz admin ostaju.

## Ispravljeno

- **Bootstrap više nije potreban.** Koristili smo samo njegov reset i klasu `visually-hidden`; sada su u `site.css`. Bez `wwwroot/lib` se ranije videlo sve što treba da bude sakriveno. Folder `wwwroot/lib/bootstrap` možeš da obrišeš.
- **jQuery i validacija su u projektu** (`wwwroot/lib`, sa licencama), pa zip više ne zavisi od Visual Studio šablona.
- **Značka korpe** — sudar klase `empty` sa blokom „korpa je prazna" (90 px visine). Sada se prikazuje samo kad je korpa puna.
- **Kartica** sama sakriva fotografiju koja ne može da se učita.

## Novo

- Zaglavlje u jednom redu: kategorije sa podmenijem, Novo, Akcija, ikonice; na telefonu meni iza dugmeta.
- Traka filtera iznad proizvoda (padajući paneli, rade i bez JavaScript-a), aktivni filteri kao čipovi sa ×.
- Kartica: pločica u boji proizvoda dok nema fotografija; oznake Novo, popust, „Još N kom.", rasprodato.
- Mreža sa 4 kolone, šira strana (1320 px), čitljiviji sporedni tekst (kontrast 4,85:1 umesto 2,73:1), slova za korak veća.

Ostale stranice (proizvod, početna, nalog) dobijaju novo zaglavlje i veća slova, a njihov raspored se ne menja.
