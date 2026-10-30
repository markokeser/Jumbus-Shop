# Faza 3 — korpa i checkout

## Šta je napravljeno

**Korpa u sesiji.** Gost kupuje bez naloga. U sesiji stoje samo `variantId`
i količina — cena, stanje i naziv se čitaju iz baze pri svakom zahtevu. Korpa
ostavljena preko noći ne može da se naplati po jučerašnjoj ceni ni da kupi
nešto što je u međuvremenu rasprodato.

Kad se nešto promeni, korpa to **kaže naglas** umesto da tiho ispravi:
„Svilena bluza Lea je smanjena na 1, toliko je ostalo."

**Cenovnik.** `PricingService` računa redom: međuzbir, popust, dostava, ukupno,
pa PDV. Redosled nije proizvoljan — prag za besplatnu dostavu se gleda posle
popusta, jer bi inače kupon bio plaćen dvaput.

**PDV se izvlači, ne dodaje.** Maloprodajne cene u Srbiji već sadrže PDV, pa je
iznos koji se prikazuje na računu izvučen iz ukupnog:
`PDV = ukupno − ukupno / 1,20`. Dodavanje 20% odozgo bi svakog kupca
naplatilo 20% više.

**Zaštita od prodaje ispod nule.** Stanje se skida uslovnim UPDATE-om:

```sql
UPDATE ProductVariants SET StockQuantity = StockQuantity - ?
WHERE Id = ? AND IsActive = 1 AND StockQuantity >= ?
```

Ako je pogođeno 0 redova, artikal je otišao između korpe i potvrde —
transakcija se poništava i kupac dobija poruku. Dva kupca koja istovremeno
uzimaju poslednji komad oba stižu do ove linije, ali samo jedan menja red.
Čitanje pa upis bi propustilo oba.

Isto važi i za kupone sa ograničenim brojem upotreba.

**Podaci porudžbine se kopiraju, ne referenciraju.** Naziv, šifra i cena se
upisuju u `OrderItems` u trenutku kupovine. Ako se proizvod sutra preimenuje,
poskupi ili povuče, porudžbina i dalje piše šta je stvarno kupljeno.

**Ništa o novcu ne dolazi iz formulara.** Checkout formular nosi samo kontakt
i adresu. Cene, popust i ukupno se ponovo računaju na serveru iz korpe.

## Provereno

**Matematika** — portovao sam pravila i pustio ih kroz granične slučajeve:
- PDV izvučen iz 6.000 daje neto 5.000 + PDV 1.000; 5.000 × 1,20 = 6.000
- 10% od 45.000 udara u limit od 3.000
- fiksnih 500 na korpu od 300 ne pravi negativan iznos
- korpa od 6.200 sa 10% popusta pada ispod praga i dostava se **naplaćuje**

**Zaštita stanja** — testirana na pravoj bazi. Stanje 3, prva porudžbina uzima
2 (1 red pogođen), druga pokušava 2 (0 redova, odbijena), stanje ostaje 1.

**Prikaz** — korpa i checkout renderovani sa pravim `site.css` i pravim
redovima iz baze. 1440 px i 390 px, bez prelivanja. PDV na ekranu: 19.260 RSD
ukupno, 3.210 RSD PDV — tačno.

**Statički** — nema sukoba route atributa, nema nedostajućih `using`-a, svih
163 ključa postoje na oba jezika sa istim placeholderima, sve klase
stilizovane, sve zavisnosti kontrolera registrovane.

**Nije kompajlirano.** Microsoftovi hostovi su i dalje blokirani.

## Plaćanje karticom nije pravo

`MockCardProvider` odobrava sve i izmišlja referencu. Radi **samo u
Development modu** — u produkciji vraća grešku umesto da se pretvara.

Prava integracija nije popunjavanje te klase. Domaći procesori (NestPay preko
banke, WSPay, Monri) preusmeravaju kupca na formu banke, pa zovu povratni URL
sa potpisanim podatkom koji mora da se proveri pre nego što se porudžbina
tretira kao plaćena. To su dva dodatna endpointa i tajni ključ za potpis.

Pouzeće radi i danas, i u Srbiji je i dalje najčešći način plaćanja.

## Mejlovi nisu pravi

`LoggingOrderEmailSender` upisuje u log šta bi poslao. Potvrda porudžbine je
jedina poruka koju prodavnica **zakonski mora** da pošalje, pa je ovo
privremeno rešenje sa rokom. Zamena je registracija druge implementacije u
`Program.cs`, ništa drugo se ne menja.

## Sledeće

Faza 4: nalog kupca, istorija porudžbina, praćenje statusa, adresar,
wishlist i recenzije.
