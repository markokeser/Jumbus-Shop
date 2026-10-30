using WebShop.Models.Catalog;

namespace WebShop.Data.Seed;

/// <summary>
/// The demo catalog, kept as plain data so the seeding logic in
/// DatabaseSeeder stays readable. Replace these lists with real stock
/// once the shop goes live; nothing else depends on them.
/// </summary>
internal static class SeedCatalog
{
    internal sealed record AttributeSeed(
        string Code,
        AttributeDataType DataType,
        bool IsVariantDefining,
        bool IsFilterable,
        string? Unit,
        int DisplayOrder,
        string NameSr,
        string NameEn,
        (string Code, string LabelSr, string LabelEn, string? Swatch)[] Options);

    internal sealed record CategorySeed(
        string Code,
        string? ParentCode,
        int DisplayOrder,
        string NameSr,
        string NameEn,
        string SlugSr,
        string SlugEn,
        string? DescriptionSr,
        string? DescriptionEn,
        string[] AttributeCodes);

    internal sealed record BrandSeed(string Name, string Slug);

    internal sealed record ProductSeed
    {
        public required string Sku { get; init; }
        public required string CategoryCode { get; init; }
        public required string BrandName { get; init; }
        public required string NameSr { get; init; }
        public required string NameEn { get; init; }
        public required string ShortSr { get; init; }
        public required string ShortEn { get; init; }
        public required string DescriptionSr { get; init; }
        public required string DescriptionEn { get; init; }
        public string? IngredientsSr { get; init; }
        public string? IngredientsEn { get; init; }
        public string? CareSr { get; init; }
        public string? CareEn { get; init; }
        public required decimal Price { get; init; }
        public decimal? SalePrice { get; init; }
        public bool IsFeatured { get; init; }

        /// <summary>Option codes for the "color" attribute, clothing only.</summary>
        public string[] Colors { get; init; } = [];

        /// <summary>Option codes for the "size" attribute, clothing only.</summary>
        public string[] Sizes { get; init; } = [];

        /// <summary>Option codes for the "shade" attribute, makeup only.</summary>
        public string[] Shades { get; init; } = [];

        /// <summary>Numeric values for the "volume" attribute, in ml.</summary>
        public decimal[] Volumes { get; init; } = [];

        /// <summary>Non-variant attribute applied to every variant of the product.</summary>
        public string? SkinType { get; init; }

        /// <summary>Months from today until the batch expires. Cosmetics only.</summary>
        public int? ShelfLifeMonths { get; init; }
    }

    internal static readonly AttributeSeed[] Attributes =
    [
        new("size", AttributeDataType.Option, true, true, null, 10, "Veličina", "Size",
        [
            ("xs", "XS", "XS", null),
            ("s", "S", "S", null),
            ("m", "M", "M", null),
            ("l", "L", "L", null),
            ("xl", "XL", "XL", null),
            ("xxl", "XXL", "XXL", null)
        ]),

        new("color", AttributeDataType.Option, true, true, null, 20, "Boja", "Colour",
        [
            ("black", "Crna", "Black", "#1A1A1A"),
            ("ivory", "Slonovača", "Ivory", "#F2EBDD"),
            ("burgundy", "Bordo", "Burgundy", "#6E1B2E"),
            ("olive", "Maslinasta", "Olive", "#6B7048"),
            ("navy", "Teget", "Navy", "#22304A"),
            ("blush", "Puder roze", "Blush", "#E7C4C0"),
            ("camel", "Kamel", "Camel", "#B08658")
        ]),

        new("shade", AttributeDataType.Option, true, true, null, 30, "Nijansa", "Shade",
        [
            ("01-porcelain", "01 Porcelan", "01 Porcelain", "#F5E3D7"),
            ("02-warm-ivory", "02 Topla slonovača", "02 Warm Ivory", "#EFD3BC"),
            ("03-sand", "03 Pesak", "03 Sand", "#E0BC9B"),
            ("04-honey", "04 Med", "04 Honey", "#CE9E72"),
            ("05-amber", "05 Ćilibar", "05 Amber", "#B07C50"),
            ("06-chestnut", "06 Kesten", "06 Chestnut", "#8C5A38")
        ]),

        new("volume", AttributeDataType.Numeric, true, true, "ml", 40, "Zapremina", "Volume", []),

        new("skin-type", AttributeDataType.Option, false, true, null, 50, "Tip kože", "Skin type",
        [
            ("dry", "Suva", "Dry", null),
            ("oily", "Masna", "Oily", null),
            ("combination", "Mešovita", "Combination", null),
            ("sensitive", "Osetljiva", "Sensitive", null),
            ("all", "Sve vrste kože", "All skin types", null)
        ])
    ];

    internal static readonly CategorySeed[] Categories =
    [
        new("clothing", null, 10, "Garderoba", "Clothing", "garderoba", "clothing",
            "Odevni komadi za svaki dan i za posebne prilike.",
            "Pieces for everyday wear and for the occasions in between.",
            []),

        new("dresses", "clothing", 11, "Haljine", "Dresses", "haljine", "dresses",
            "Od jednostavnih dnevnih krojeva do haljina za veče.",
            "From simple day cuts to evening pieces.",
            ["size", "color"]),

        new("tops", "clothing", 12, "Majice i bluze", "Tops and blouses", "majice-i-bluze", "tops-and-blouses",
            "Osnovni komadi koji se slažu sa svim ostalim iz ormara.",
            "The pieces that go with everything else in the wardrobe.",
            ["size", "color"]),

        new("outerwear", "clothing", 13, "Jakne i kaputi", "Jackets and coats", "jakne-i-kaputi", "jackets-and-coats",
            "Slojevi za prelazne mesece i za zimu.",
            "Layers for the in-between months and for winter.",
            ["size", "color"]),

        new("cosmetics", null, 20, "Kozmetika", "Cosmetics", "kozmetika", "cosmetics",
            "Nega i šminka, sa punim sastavom na svakoj stranici.",
            "Skincare and makeup, with the full ingredient list on every page.",
            []),

        new("skincare", "cosmetics", 21, "Nega lica", "Skincare", "nega-lica", "skincare",
            "Kreme, serumi i čišćenje, razvrstani po tipu kože.",
            "Creams, serums and cleansers, sorted by skin type.",
            ["volume", "skin-type"]),

        new("makeup", "cosmetics", 22, "Šminka", "Makeup", "sminka", "makeup",
            "Nijanse su fotografisane na dnevnom svetlu, bez retuširanja.",
            "Shades are photographed in daylight, without retouching.",
            ["shade", "volume"]),

        new("fragrance", "cosmetics", 23, "Parfemi", "Fragrance", "parfemi", "fragrance",
            "Parfemske vode u dve zapremine.",
            "Eau de parfum in two sizes.",
            ["volume"])
    ];

    internal static readonly BrandSeed[] Brands =
    [
        new("Aurora Atelier", "aurora-atelier"),
        new("Mila Studio", "mila-studio"),
        new("Noir Label", "noir-label"),
        new("Vela Beauty", "vela-beauty"),
        new("Lumen Skin", "lumen-skin"),
        new("Petit Rouge", "petit-rouge")
    ];

    internal static readonly ProductSeed[] Products =
    [
        // ── Dresses ──────────────────────────────────────────────────────
        new()
        {
            Sku = "DR-001",
            CategoryCode = "dresses",
            BrandName = "Aurora Atelier",
            NameSr = "Koktel haljina Vera",
            NameEn = "Vera cocktail dress",
            ShortSr = "Midi kroj sa naborom u struku, od teže viskoze.",
            ShortEn = "Midi cut with a gathered waist, in heavier viscose.",
            DescriptionSr = "Haljina srednje dužine sa naborom koji naglašava struk i suknjom koja pada bez dodatnog volumena. Postavljena je do kolena, pa se ne lepi. Zatvara se skrivenim rajsferšlusom na levom boku.",
            DescriptionEn = "A mid-length dress with gathering that marks the waist and a skirt that falls without added volume. Lined to the knee so it does not cling. Closes with a concealed zip at the left hip.",
            IngredientsSr = "80% viskoza, 20% poliester. Postava: 100% viskoza.",
            IngredientsEn = "80% viscose, 20% polyester. Lining: 100% viscose.",
            CareSr = "Pranje na 30°C, bez sušilice. Peglati na srednjoj temperaturi.",
            CareEn = "Machine wash at 30°C, do not tumble dry. Iron on medium.",
            Price = 8990m,
            SalePrice = 6990m,
            IsFeatured = true,
            Colors = ["black", "burgundy"],
            Sizes = ["s", "m", "l", "xl"]
        },
        new()
        {
            Sku = "DR-002",
            CategoryCode = "dresses",
            BrandName = "Mila Studio",
            NameSr = "Lanena haljina Ada",
            NameEn = "Ada linen dress",
            ShortSr = "Ravan kroj bez postave, za letnje dane.",
            ShortEn = "A straight, unlined cut for warm days.",
            DescriptionSr = "Pravi kroj bez struka, sa dva bočna džepa i dugmadima od kokosa do pola prednjice. Lan omekšava posle prvog pranja i namerno se gužva.",
            DescriptionEn = "A straight cut with no waist seam, two side pockets and coconut buttons down the front placket. The linen softens after the first wash and creases on purpose.",
            IngredientsSr = "100% lan.",
            IngredientsEn = "100% linen.",
            CareSr = "Pranje na 30°C, peglati dok je blago vlažna.",
            CareEn = "Wash at 30°C, iron while slightly damp.",
            Price = 7490m,
            Colors = ["ivory", "olive"],
            Sizes = ["xs", "s", "m", "l"]
        },
        new()
        {
            Sku = "DR-003",
            CategoryCode = "dresses",
            BrandName = "Noir Label",
            NameSr = "Pletena haljina Iva",
            NameEn = "Iva knit dress",
            ShortSr = "Rebrasti pleteni materijal, dužina do lista.",
            ShortEn = "Ribbed knit, falling to mid-calf.",
            DescriptionSr = "Uska pletena haljina sa rukavima do lakta i prorezom pozadi. Materijal se rasteže u oba pravca, pa prati liniju tela bez stezanja.",
            DescriptionEn = "A close-fitting knit with elbow sleeves and a back vent. The fabric stretches in both directions, so it follows the body without pulling.",
            IngredientsSr = "70% viskoza, 25% poliamid, 5% elastan.",
            IngredientsEn = "70% viscose, 25% polyamide, 5% elastane.",
            CareSr = "Ručno pranje ili program za osetljivo, sušiti na ravnoj površini.",
            CareEn = "Hand wash or delicate cycle, dry flat.",
            Price = 6490m,
            Colors = ["black", "camel"],
            Sizes = ["s", "m", "l"]
        },
        new()
        {
            Sku = "DR-004",
            CategoryCode = "dresses",
            BrandName = "Aurora Atelier",
            NameSr = "Košulja haljina Sena",
            NameEn = "Sena shirt dress",
            ShortSr = "Kroj košulje sa pojasom koji se vezuje.",
            ShortEn = "A shirt cut with a tie belt.",
            DescriptionSr = "Klasična košulja haljina sa okovratnikom, manžetnama i pojasom u istom materijalu. Bez pojasa nosi se kao ravan kroj preko uskih pantalona.",
            DescriptionEn = "A classic shirt dress with collar, cuffs and a self-fabric belt. Worn without the belt it becomes a straight layer over narrow trousers.",
            IngredientsSr = "65% pamuk, 32% poliester, 3% elastan.",
            IngredientsEn = "65% cotton, 32% polyester, 3% elastane.",
            CareSr = "Pranje na 40°C, peglati na srednjoj temperaturi.",
            CareEn = "Wash at 40°C, iron on medium.",
            Price = 7990m,
            Colors = ["navy", "ivory"],
            Sizes = ["s", "m", "l", "xl", "xxl"]
        },

        // ── Tops ─────────────────────────────────────────────────────────
        new()
        {
            Sku = "TP-001",
            CategoryCode = "tops",
            BrandName = "Mila Studio",
            NameSr = "Svilena bluza Lea",
            NameEn = "Lea silk blouse",
            ShortSr = "Opuštena bluza sa spuštenim ramenom.",
            ShortEn = "A relaxed blouse with a dropped shoulder.",
            DescriptionSr = "Bluza od svilene mešavine, sa spuštenim ramenom i manžetnom koja se zakopčava jednim dugmetom. Prednjica je nešto kraća od zadnjeg dela.",
            DescriptionEn = "A silk-blend blouse with a dropped shoulder and a single-button cuff. The front hem sits slightly higher than the back.",
            IngredientsSr = "55% svila, 45% viskoza.",
            IngredientsEn = "55% silk, 45% viscose.",
            CareSr = "Hemijsko čišćenje ili ručno pranje u hladnoj vodi.",
            CareEn = "Dry clean, or hand wash cold.",
            Price = 5990m,
            IsFeatured = true,
            Colors = ["ivory", "black", "blush"],
            Sizes = ["xs", "s", "m", "l"]
        },
        new()
        {
            Sku = "TP-002",
            CategoryCode = "tops",
            BrandName = "Noir Label",
            NameSr = "Pamučna majica Nora",
            NameEn = "Nora cotton tee",
            ShortSr = "Gušći pamuk koji drži oblik posle pranja.",
            ShortEn = "A heavier cotton that holds its shape after washing.",
            DescriptionSr = "Majica od pamuka od 220 g/m², sa ojačanim okruglim izrezom. Kroj je ravan, bez sužavanja u struku.",
            DescriptionEn = "A 220 gsm cotton tee with a reinforced crew neck. The cut is straight, with no taper at the waist.",
            IngredientsSr = "100% organski pamuk.",
            IngredientsEn = "100% organic cotton.",
            CareSr = "Pranje na 40°C, sušilica na niskoj temperaturi.",
            CareEn = "Wash at 40°C, tumble dry low.",
            Price = 2490m,
            SalePrice = 1990m,
            Colors = ["black", "ivory", "olive", "navy"],
            Sizes = ["xs", "s", "m", "l", "xl"]
        },
        new()
        {
            Sku = "TP-003",
            CategoryCode = "tops",
            BrandName = "Aurora Atelier",
            NameSr = "Džemper Rea",
            NameEn = "Rea jumper",
            ShortSr = "Merino vuna, kroj sa spuštenim ramenom.",
            ShortEn = "Merino wool with a dropped shoulder.",
            DescriptionSr = "Džemper od fine merino vune koji ne bode. Rukav je nešto duži, sa rebrastom manžetnom koja se može podviti.",
            DescriptionEn = "A fine merino jumper that does not itch. Sleeves run slightly long, with a ribbed cuff that can be folded back.",
            IngredientsSr = "100% merino vuna, 19,5 mikrona.",
            IngredientsEn = "100% merino wool, 19.5 micron.",
            CareSr = "Program za vunu na 30°C, sušiti na ravnoj površini.",
            CareEn = "Wool cycle at 30°C, dry flat.",
            Price = 8490m,
            Colors = ["camel", "burgundy", "navy"],
            Sizes = ["s", "m", "l", "xl"]
        },
        new()
        {
            Sku = "TP-004",
            CategoryCode = "tops",
            BrandName = "Mila Studio",
            NameSr = "Košulja Ela",
            NameEn = "Ela shirt",
            ShortSr = "Poplin košulja sa skrivenim zakopčavanjem.",
            ShortEn = "A poplin shirt with a concealed placket.",
            DescriptionSr = "Poplin košulja klasičnog kroja, sa skrivenim dugmadima i džepom na levoj strani. Materijal je gust, pa se ne providi.",
            DescriptionEn = "A classic poplin shirt with concealed buttons and a patch pocket on the left. The weave is dense enough not to show through.",
            IngredientsSr = "97% pamuk, 3% elastan.",
            IngredientsEn = "97% cotton, 3% elastane.",
            CareSr = "Pranje na 40°C, peglati na visokoj temperaturi.",
            CareEn = "Wash at 40°C, iron hot.",
            Price = 4990m,
            Colors = ["ivory", "navy"],
            Sizes = ["xs", "s", "m", "l", "xl"]
        },

        // ── Outerwear ────────────────────────────────────────────────────
        new()
        {
            Sku = "OW-001",
            CategoryCode = "outerwear",
            BrandName = "Noir Label",
            NameSr = "Vuneni kaput Ivana",
            NameEn = "Ivana wool coat",
            ShortSr = "Dvoredni kaput dužine do kolena.",
            ShortEn = "A double-breasted coat, knee length.",
            DescriptionSr = "Kaput od mešavine vune, sa dvorednim zakopčavanjem i džepovima sa preklopom. Postava je klizava, pa lako ide preko džempera.",
            DescriptionEn = "A wool-blend coat with a double-breasted front and flap pockets. The lining is slippery enough to go over a jumper without catching.",
            IngredientsSr = "70% vuna, 25% poliester, 5% kašmir. Postava: 100% viskoza.",
            IngredientsEn = "70% wool, 25% polyester, 5% cashmere. Lining: 100% viscose.",
            CareSr = "Isključivo hemijsko čišćenje.",
            CareEn = "Dry clean only.",
            Price = 24990m,
            SalePrice = 19990m,
            IsFeatured = true,
            Colors = ["camel", "black"],
            Sizes = ["s", "m", "l", "xl"]
        },
        new()
        {
            Sku = "OW-002",
            CategoryCode = "outerwear",
            BrandName = "Aurora Atelier",
            NameSr = "Trenč mantil Tara",
            NameEn = "Tara trench coat",
            ShortSr = "Klasičan trenč sa pojasom i epoletama.",
            ShortEn = "A classic trench with belt and epaulettes.",
            DescriptionSr = "Trenč od impregniranog pamuka, sa pojasom, epoletama i preklopom na leđima. Odbija kišu bez dodatnog sloja.",
            DescriptionEn = "A coated-cotton trench with belt, epaulettes and a storm flap at the back. Sheds rain without an extra layer.",
            IngredientsSr = "100% pamuk sa vodoodbojnim premazom.",
            IngredientsEn = "100% cotton with a water-repellent finish.",
            CareSr = "Hemijsko čišćenje, ne prati u mašini.",
            CareEn = "Dry clean, do not machine wash.",
            Price = 18990m,
            Colors = ["camel", "olive"],
            Sizes = ["xs", "s", "m", "l"]
        },
        new()
        {
            Sku = "OW-003",
            CategoryCode = "outerwear",
            BrandName = "Mila Studio",
            NameSr = "Jakna od štepane tkanine Vida",
            NameEn = "Vida quilted jacket",
            ShortSr = "Lagana štepana jakna za prelazne mesece.",
            ShortEn = "A light quilted jacket for the in-between months.",
            DescriptionSr = "Kratka štepana jakna sa punjenjem od recikliranih vlakana. Pakuje se u sopstveni džep, pa staje u torbu.",
            DescriptionEn = "A short quilted jacket filled with recycled fibre. Packs into its own pocket and fits in a bag.",
            IngredientsSr = "Spoljašnji sloj: 100% reciklirani poliester. Punjenje: 100% reciklirani poliester.",
            IngredientsEn = "Shell: 100% recycled polyester. Fill: 100% recycled polyester.",
            CareSr = "Pranje na 30°C, sušilica na niskoj temperaturi da se punjenje razdvoji.",
            CareEn = "Wash at 30°C, tumble dry low so the fill separates.",
            Price = 11990m,
            Colors = ["black", "olive", "burgundy"],
            Sizes = ["s", "m", "l", "xl"]
        },

        // ── Skincare ─────────────────────────────────────────────────────
        new()
        {
            Sku = "SC-001",
            CategoryCode = "skincare",
            BrandName = "Lumen Skin",
            NameSr = "Hidratantna krema sa niacinamidom",
            NameEn = "Niacinamide moisturiser",
            ShortSr = "Laka krema za mešovitu kožu, bez mirisa.",
            ShortEn = "A light cream for combination skin, fragrance free.",
            DescriptionSr = "Krema sa 5% niacinamida i glicerinom, formulisana bez mirisa i esencijalnih ulja. Upija se za minut i može ispod šminke.",
            DescriptionEn = "A cream with 5% niacinamide and glycerin, formulated without fragrance or essential oils. Absorbs in about a minute and sits under makeup.",
            IngredientsSr = "Aqua, Glycerin, Niacinamide, Caprylic/Capric Triglyceride, Cetearyl Alcohol, Squalane, Panthenol, Sodium Hyaluronate, Tocopherol, Phenoxyethanol.",
            IngredientsEn = "Aqua, Glycerin, Niacinamide, Caprylic/Capric Triglyceride, Cetearyl Alcohol, Squalane, Panthenol, Sodium Hyaluronate, Tocopherol, Phenoxyethanol.",
            CareSr = "Naneti ujutru i uveče na očišćenu kožu.",
            CareEn = "Apply morning and evening to cleansed skin.",
            Price = 2890m,
            IsFeatured = true,
            Volumes = [50m, 100m],
            SkinType = "combination",
            ShelfLifeMonths = 24
        },
        new()
        {
            Sku = "SC-002",
            CategoryCode = "skincare",
            BrandName = "Lumen Skin",
            NameSr = "Serum sa hijaluronskom kiselinom",
            NameEn = "Hyaluronic acid serum",
            ShortSr = "Tri molekulske težine hijaluronske kiseline.",
            ShortEn = "Three molecular weights of hyaluronic acid.",
            DescriptionSr = "Vodenasti serum koji se nanosi na blago vlažnu kožu, pre kreme. Ne sadrži alkohol ni miris.",
            DescriptionEn = "A watery serum applied to slightly damp skin, before cream. Contains no alcohol or fragrance.",
            IngredientsSr = "Aqua, Sodium Hyaluronate, Glycerin, Panthenol, Sodium Hyaluronate Crosspolymer, Hydrolyzed Hyaluronic Acid, Phenoxyethanol, Ethylhexylglycerin.",
            IngredientsEn = "Aqua, Sodium Hyaluronate, Glycerin, Panthenol, Sodium Hyaluronate Crosspolymer, Hydrolyzed Hyaluronic Acid, Phenoxyethanol, Ethylhexylglycerin.",
            CareSr = "Naneti dve do tri kapi na vlažnu kožu, pa zatvoriti kremom.",
            CareEn = "Apply two or three drops to damp skin, then seal with cream.",
            Price = 2390m,
            SalePrice = 1890m,
            Volumes = [30m],
            SkinType = "all",
            ShelfLifeMonths = 18
        },
        new()
        {
            Sku = "SC-003",
            CategoryCode = "skincare",
            BrandName = "Vela Beauty",
            NameSr = "Uljni gel za čišćenje",
            NameEn = "Cleansing oil gel",
            ShortSr = "Skida vodootpornu šminku bez trljanja.",
            ShortEn = "Removes waterproof makeup without rubbing.",
            DescriptionSr = "Gel na bazi ulja koji se emulguje u dodiru sa vodom. Nanosi se na suvu kožu i suve ruke, pa ispira mlakom vodom.",
            DescriptionEn = "An oil-based gel that emulsifies on contact with water. Applied to dry skin with dry hands, then rinsed with lukewarm water.",
            IngredientsSr = "Caprylic/Capric Triglyceride, Helianthus Annuus Seed Oil, PEG-20 Glyceryl Triisostearate, Simmondsia Chinensis Seed Oil, Tocopherol.",
            IngredientsEn = "Caprylic/Capric Triglyceride, Helianthus Annuus Seed Oil, PEG-20 Glyceryl Triisostearate, Simmondsia Chinensis Seed Oil, Tocopherol.",
            CareSr = "Koristiti uveče, kao prvi korak dvostrukog čišćenja.",
            CareEn = "Use in the evening, as the first step of a double cleanse.",
            Price = 2190m,
            Volumes = [150m, 250m],
            SkinType = "all",
            ShelfLifeMonths = 24
        },
        new()
        {
            Sku = "SC-004",
            CategoryCode = "skincare",
            BrandName = "Vela Beauty",
            NameSr = "Bogata krema za suvu kožu",
            NameEn = "Rich cream for dry skin",
            ShortSr = "Gusta krema sa ceramidima, za zimske mesece.",
            ShortEn = "A dense ceramide cream for the winter months.",
            DescriptionSr = "Gusta krema sa ceramidima i sheom, namenjena koži koja zateže. Ostavlja blagi sjaj koji nestane za nekoliko minuta.",
            DescriptionEn = "A dense cream with ceramides and shea, for skin that feels tight. Leaves a slight sheen that settles within a few minutes.",
            IngredientsSr = "Aqua, Butyrospermum Parkii Butter, Glycerin, Cetearyl Alcohol, Ceramide NP, Ceramide AP, Cholesterol, Squalane, Phenoxyethanol.",
            IngredientsEn = "Aqua, Butyrospermum Parkii Butter, Glycerin, Cetearyl Alcohol, Ceramide NP, Ceramide AP, Cholesterol, Squalane, Phenoxyethanol.",
            CareSr = "Naneti uveče na očišćenu kožu, po potrebi i ujutru.",
            CareEn = "Apply in the evening to cleansed skin, and in the morning if needed.",
            Price = 3290m,
            Volumes = [50m],
            SkinType = "dry",
            ShelfLifeMonths = 24
        },
        new()
        {
            Sku = "SC-005",
            CategoryCode = "skincare",
            BrandName = "Lumen Skin",
            NameSr = "Gel za osetljivu kožu",
            NameEn = "Gel for sensitive skin",
            ShortSr = "Bez mirisa, bez alkohola, bez esencijalnih ulja.",
            ShortEn = "No fragrance, no alcohol, no essential oils.",
            DescriptionSr = "Lagan gel sa pantenolom i beta-glukanom, za kožu koja reaguje na većinu proizvoda. Testiran pod dermatološkom kontrolom.",
            DescriptionEn = "A light gel with panthenol and beta-glucan, for skin that reacts to most products. Dermatologically tested.",
            IngredientsSr = "Aqua, Glycerin, Panthenol, Beta-Glucan, Allantoin, Sodium Hyaluronate, Xanthan Gum, Phenoxyethanol.",
            IngredientsEn = "Aqua, Glycerin, Panthenol, Beta-Glucan, Allantoin, Sodium Hyaluronate, Xanthan Gum, Phenoxyethanol.",
            CareSr = "Naneti na očišćenu kožu, ujutru i uveče.",
            CareEn = "Apply to cleansed skin, morning and evening.",
            Price = 2590m,
            Volumes = [50m, 100m],
            SkinType = "sensitive",
            ShelfLifeMonths = 24
        },

        // ── Makeup ───────────────────────────────────────────────────────
        new()
        {
            Sku = "MU-001",
            CategoryCode = "makeup",
            BrandName = "Petit Rouge",
            NameSr = "Tečni puder sa srednjim prekrivanjem",
            NameEn = "Medium coverage foundation",
            ShortSr = "Šest nijansi, satenski završetak.",
            ShortEn = "Six shades, satin finish.",
            DescriptionSr = "Puder srednjeg prekrivanja koji se gradi u slojevima. Ostaje na koži oko osam sati bez pudera za fiksiranje.",
            DescriptionEn = "A buildable medium-coverage foundation. Holds for around eight hours without setting powder.",
            IngredientsSr = "Aqua, Cyclopentasiloxane, Glycerin, Titanium Dioxide, Iron Oxides, Dimethicone, Tocopheryl Acetate, Phenoxyethanol.",
            IngredientsEn = "Aqua, Cyclopentasiloxane, Glycerin, Titanium Dioxide, Iron Oxides, Dimethicone, Tocopheryl Acetate, Phenoxyethanol.",
            CareSr = "Naneti sunđerom ili četkicom, od sredine lica ka spolja.",
            CareEn = "Apply with a sponge or brush, working outwards from the centre.",
            Price = 3490m,
            IsFeatured = true,
            Shades = ["01-porcelain", "02-warm-ivory", "03-sand", "04-honey", "05-amber", "06-chestnut"],
            Volumes = [30m],
            ShelfLifeMonths = 12
        },
        new()
        {
            Sku = "MU-002",
            CategoryCode = "makeup",
            BrandName = "Petit Rouge",
            NameSr = "Korektor sa mat završetkom",
            NameEn = "Matte finish concealer",
            ShortSr = "Gušća formula za ispod očiju i za pojedinačna mesta.",
            ShortEn = "A denser formula for under-eyes and spot coverage.",
            DescriptionSr = "Korektor koji se ne skuplja u linijama ispod očiju. Aplikator je od gustog flomastera, pa se lako dozira.",
            DescriptionEn = "A concealer that does not collect in lines under the eyes. The dense doe-foot applicator makes it easy to dose.",
            IngredientsSr = "Aqua, Dimethicone, Glycerin, Titanium Dioxide, Iron Oxides, Silica, Tocopherol, Phenoxyethanol.",
            IngredientsEn = "Aqua, Dimethicone, Glycerin, Titanium Dioxide, Iron Oxides, Silica, Tocopherol, Phenoxyethanol.",
            CareSr = "Naneti u tankom sloju i utapkati prstom.",
            CareEn = "Apply thinly and tap in with a fingertip.",
            Price = 1990m,
            SalePrice = 1590m,
            Shades = ["01-porcelain", "02-warm-ivory", "03-sand", "04-honey"],
            Volumes = [7m],
            ShelfLifeMonths = 12
        },
        new()
        {
            Sku = "MU-003",
            CategoryCode = "makeup",
            BrandName = "Vela Beauty",
            NameSr = "Ruž sa satenskim završetkom",
            NameEn = "Satin lipstick",
            ShortSr = "Pigmentovan u jednom prelazu, nije suv.",
            ShortEn = "Full pigment in one pass, and not drying.",
            DescriptionSr = "Ruž koji ostavlja satenski trag i ne isušuje usne. Boja drži oko četiri sata, duže bez jela.",
            DescriptionEn = "A lipstick with a satin finish that does not dry the lips. Colour holds for around four hours, longer without eating.",
            IngredientsSr = "Ricinus Communis Seed Oil, Cera Alba, Candelilla Cera, Iron Oxides, Mica, Tocopherol, Parfum.",
            IngredientsEn = "Ricinus Communis Seed Oil, Cera Alba, Candelilla Cera, Iron Oxides, Mica, Tocopherol, Parfum.",
            CareSr = "Naneti direktno iz stika ili četkicom za precizniju liniju.",
            CareEn = "Apply straight from the bullet, or with a brush for a sharper line.",
            Price = 1690m,
            Shades = ["03-sand", "04-honey", "05-amber", "06-chestnut"],
            Volumes = [4m],
            ShelfLifeMonths = 18
        },
        new()
        {
            Sku = "MU-004",
            CategoryCode = "makeup",
            BrandName = "Petit Rouge",
            NameSr = "Maskara za volumen",
            NameEn = "Volumising mascara",
            ShortSr = "Četkica sa vlaknima, ne pravi grudve.",
            ShortEn = "A fibre brush that does not clump.",
            DescriptionSr = "Maskara koja podiže trepavice i drži savijanje ceo dan. Skida se toplom vodom, bez sredstva za skidanje šminke.",
            DescriptionEn = "A mascara that lifts lashes and holds the curl all day. Comes off with warm water, no remover needed.",
            IngredientsSr = "Aqua, Cera Alba, Copernicia Cerifera Wax, Iron Oxides, Panthenol, Phenoxyethanol.",
            IngredientsEn = "Aqua, Cera Alba, Copernicia Cerifera Wax, Iron Oxides, Panthenol, Phenoxyethanol.",
            CareSr = "Zameniti nakon šest meseci od otvaranja.",
            CareEn = "Replace six months after opening.",
            Price = 1890m,
            Volumes = [10m],
            ShelfLifeMonths = 12
        },

        // ── Fragrance ────────────────────────────────────────────────────
        new()
        {
            Sku = "FR-001",
            CategoryCode = "fragrance",
            BrandName = "Noir Label",
            NameSr = "Parfemska voda Sever",
            NameEn = "Sever eau de parfum",
            ShortSr = "Vetiver, kedar i bergamot.",
            ShortEn = "Vetiver, cedar and bergamot.",
            DescriptionSr = "Suv, drvenast miris koji se otvara bergamotom i završava vetiverom. Drži oko šest sati na koži.",
            DescriptionEn = "A dry, woody scent that opens on bergamot and settles into vetiver. Holds for around six hours on skin.",
            IngredientsSr = "Alcohol Denat., Parfum, Aqua, Limonene, Linalool, Coumarin.",
            IngredientsEn = "Alcohol Denat., Parfum, Aqua, Limonene, Linalool, Coumarin.",
            CareSr = "Naneti na zglobove i vrat, ne trljati posle nanošenja.",
            CareEn = "Apply to wrists and neck, do not rub in.",
            Price = 6990m,
            Volumes = [50m, 100m],
            ShelfLifeMonths = 36
        },
        new()
        {
            Sku = "FR-002",
            CategoryCode = "fragrance",
            BrandName = "Vela Beauty",
            NameSr = "Parfemska voda Mira",
            NameEn = "Mira eau de parfum",
            ShortSr = "Jasmin, mošus i beli biber.",
            ShortEn = "Jasmine, musk and white pepper.",
            DescriptionSr = "Cvetni miris sa oštrijim uvodom belog bibera. Lakši je od klasičnih jasminskih parfema i podnosi dnevno nošenje.",
            DescriptionEn = "A floral scent with a sharper white pepper opening. Lighter than most jasmine fragrances and wearable during the day.",
            IngredientsSr = "Alcohol Denat., Parfum, Aqua, Benzyl Salicylate, Linalool, Hexyl Cinnamal.",
            IngredientsEn = "Alcohol Denat., Parfum, Aqua, Benzyl Salicylate, Linalool, Hexyl Cinnamal.",
            CareSr = "Čuvati dalje od direktne sunčeve svetlosti.",
            CareEn = "Store away from direct sunlight.",
            Price = 5990m,
            SalePrice = 4790m,
            Volumes = [50m, 100m],
            ShelfLifeMonths = 36
        }
    ];
}
