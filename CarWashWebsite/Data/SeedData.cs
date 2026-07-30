using CarWashWebsite.Models;

namespace CarWashWebsite.Data;

/// <summary>
/// The initial catalogue content. Applied once by <see cref="DbInitializer"/> when a table is
/// empty — after that, edit the rows in Postgres and this file stops mattering.
/// </summary>
public static class SeedData
{
    private static ServiceInclude[] Includes(params string[] items) =>
        items.Select((text, i) => new ServiceInclude { Text = text, SortOrder = i }).ToArray();

    private static PlanFeature[] Feats(params string[] items) =>
        items.Select((text, i) => new PlanFeature { Text = text, SortOrder = i }).ToArray();

    public static List<Service> Services() =>
    [
        new()
        {
            Slug = "express-wash",
            Name = "Express Body Wash",
            Tagline = "Quick outside clean in 40 minutes",
            Icon = "droplet",
            StartingPrice = 349,
            ListPrice = 499,
            Duration = "40 mins",
            Warranty = "Rewash free within 24 hrs",
            IsBestseller = true,
            SortOrder = 1,
            Includes = [.. Includes(
                "pH-neutral shampoo wash", "Wheel & tyre clean", "Tyre dressing",
                "Glass cleaning", "Air-dry finish")],
        },
        new()
        {
            Slug = "foam-wash",
            Name = "Snow Foam Wash",
            Tagline = "Touchless thick-foam pre-soak",
            Icon = "foam",
            StartingPrice = 649,
            ListPrice = 999,
            Duration = "1 hr 15 mins",
            Warranty = "Rewash free within 48 hrs",
            IsBestseller = true,
            SortOrder = 2,
            Includes = [.. Includes(
                "Snow-foam cannon pre-soak", "Two-bucket hand wash", "Microfibre dry",
                "Wheel arch clean", "Dashboard wipe-down")],
        },
        new()
        {
            Slug = "interior-detailing",
            Name = "Interior Deep Clean",
            Tagline = "Seats, roof, carpets — dry-foam extracted",
            Icon = "seat",
            StartingPrice = 1899,
            ListPrice = 2999,
            Duration = "4 hrs",
            Warranty = "7-day satisfaction promise",
            SortOrder = 3,
            Includes = [.. Includes(
                "Dry-foam seat shampoo", "Carpet & mat extraction", "Roof lining clean",
                "AC vent sanitisation", "Odour neutraliser")],
        },
        new()
        {
            Slug = "ceramic-coating",
            Name = "Ceramic Coating",
            Tagline = "9H glass coat, 3-year gloss lock",
            Icon = "shield",
            StartingPrice = 17999,
            ListPrice = 27999,
            Duration = "2 days",
            Warranty = "3-year coating warranty",
            IsBestseller = true,
            SortOrder = 4,
            Includes = [.. Includes(
                "Clay bar decontamination", "2-step paint correction", "9H ceramic base coat",
                "Hydrophobic top coat", "IR-cured curing")],
        },
        new()
        {
            Slug = "rubbing-polishing",
            Name = "Rubbing & Polishing",
            Tagline = "Swirl removal + machine gloss",
            Icon = "sparkle",
            StartingPrice = 2499,
            ListPrice = 3999,
            Duration = "5 hrs",
            Warranty = "3-month gloss warranty",
            SortOrder = 5,
            Includes = [.. Includes(
                "Compound rubbing", "Dual-action machine polish", "Swirl & scratch reduction",
                "Paint sealant", "Trim restoration")],
        },
        new()
        {
            Slug = "teflon-coating",
            Name = "Teflon Coating",
            Tagline = "Budget paint protection, 6-month shine",
            Icon = "layers",
            StartingPrice = 3499,
            ListPrice = 5499,
            Duration = "4 hrs",
            Warranty = "6-month warranty",
            SortOrder = 6,
            Includes = [.. Includes(
                "Full body wash", "Light polish", "Teflon sealant application",
                "Buff & finish", "Water-spot resistance")],
        },
        new()
        {
            Slug = "engine-bay",
            Name = "Engine Bay Cleaning",
            Tagline = "Degreased, dressed and safe to open",
            Icon = "engine",
            StartingPrice = 899,
            ListPrice = 1399,
            Duration = "1 hr 30 mins",
            Warranty = "Electricals masked & insured",
            SortOrder = 7,
            Includes = [.. Includes(
                "Sensitive part masking", "Degreaser treatment", "Low-pressure rinse",
                "Compressed-air dry", "Plastic trim dressing")],
        },
        new()
        {
            Slug = "underbody",
            Name = "Underbody Wash & Anti-Rust",
            Tagline = "Monsoon-proof your chassis",
            Icon = "chassis",
            StartingPrice = 1299,
            ListPrice = 1999,
            Duration = "2 hrs",
            Warranty = "1-year anti-rust warranty",
            SortOrder = 8,
            Includes = [.. Includes(
                "High-pressure underbody flush", "Mud & salt removal", "Rust converter on spots",
                "Anti-rust coating", "Wheel-well sealing")],
        },
        new()
        {
            Slug = "headlight-restore",
            Name = "Headlight Restoration",
            Tagline = "Clear out yellowing and haze",
            Icon = "headlight",
            StartingPrice = 799,
            ListPrice = 1299,
            Duration = "1 hr",
            Warranty = "6-month clarity warranty",
            IsNew = true,
            SortOrder = 9,
            Includes = [.. Includes(
                "Wet sanding of lens", "Oxidation removal", "Machine polish",
                "UV-protective clear coat", "Both headlamps covered")],
        },
        new()
        {
            Slug = "ppf",
            Name = "Paint Protection Film",
            Tagline = "Self-healing film against stone chips",
            Icon = "film",
            StartingPrice = 34999,
            ListPrice = 54999,
            Duration = "3 days",
            Warranty = "5-year film warranty",
            IsNew = true,
            SortOrder = 10,
            Includes = [.. Includes(
                "Surface decontamination", "Computer-cut film panels", "Self-healing TPU layer",
                "Edge wrapping", "Post-install IR cure")],
        },
        new()
        {
            Slug = "doorstep-wash",
            Name = "Doorstep Waterless Wash",
            Tagline = "We come to your parking — 90% less water",
            Icon = "van",
            StartingPrice = 449,
            ListPrice = 699,
            Duration = "50 mins",
            Warranty = "Rewash free within 24 hrs",
            IsNew = true,
            SortOrder = 11,
            Includes = [.. Includes(
                "Waterless wash solution", "Microfibre-only contact", "Interior vacuum",
                "Glass & mirror polish", "Tyre shine")],
        },
        new()
        {
            Slug = "sanitisation",
            Name = "AC & Cabin Sanitisation",
            Tagline = "Anti-bacterial fogging + filter clean",
            Icon = "spray",
            StartingPrice = 999,
            ListPrice = 1599,
            Duration = "1 hr",
            Warranty = "30-day odour-free promise",
            SortOrder = 12,
            Includes = [.. Includes(
                "Cabin filter clean", "Evaporator coil foam", "Anti-bacterial fogging",
                "Duct deodorising", "Steering & touchpoint wipe")],
        },
    ];

    public static List<WashPlan> Plans() =>
    [
        new()
        {
            Name = "Weekend Shine",
            Blurb = "For the car that only steps out on Sundays.",
            PricePerMonth = 899,
            ListPricePerMonth = 1396,
            WashCount = "4 express washes / month",
            WashesIncluded = 4,
            SortOrder = 1,
            Features = [.. Feats(
                "4 express body washes", "Free tyre dressing",
                "Glass cleaning every visit", "Priority weekend slots")],
        },
        new()
        {
            Name = "Daily Driver",
            Blurb = "Office commute, potholes, dust — handled.",
            PricePerMonth = 1699,
            ListPricePerMonth = 2796,
            WashCount = "8 washes + 1 interior / month",
            WashesIncluded = 9,
            IsPopular = true,
            SortOrder = 2,
            Features = [.. Feats(
                "6 express + 2 foam washes", "1 interior vacuum & wipe", "Free doorstep pickup",
                "Monsoon underbody flush", "10% off any add-on")],
        },
        new()
        {
            Name = "Showroom Fresh",
            Blurb = "Keep it looking like delivery day, always.",
            PricePerMonth = 3299,
            ListPricePerMonth = 5497,
            WashCount = "Unlimited washes + quarterly polish",
            WashesIncluded = 20,
            SortOrder = 3,
            Features = [.. Feats(
                "Unlimited foam washes", "Monthly interior deep clean", "Quarterly machine polish",
                "Free engine bay clean", "Dedicated detailer", "Doorstep pickup & drop")],
        },
    ];

    public static List<Testimonial> Testimonials() =>
    [
        new()
        {
            Quote = "Booked the snow foam wash at 9 PM, they picked the car up at 8:30 the next morning. Came back cleaner than the showroom handed it over.",
            Author = "Rohit Deshmukh", Car = "Hyundai Creta", Locality = "Baner", SortOrder = 1,
        },
        new()
        {
            Quote = "The interior deep clean got two years of chai stains out of my back seat. I genuinely did not think that was possible.",
            Author = "Sneha Kulkarni", Car = "Maruti Baleno", Locality = "Kothrud", SortOrder = 2,
        },
        new()
        {
            Quote = "Ceramic coating was done over two days with photos at every stage. Six months in, rain just rolls off. Worth every rupee.",
            Author = "Amit Jadhav", Car = "Skoda Slavia", Locality = "Hinjewadi", SortOrder = 3,
        },
        new()
        {
            Quote = "Doorstep waterless wash is perfect for my society where washing with a hose is not allowed. Guy was done in 45 minutes.",
            Author = "Farhan Shaikh", Car = "Tata Nexon", Locality = "Viman Nagar", SortOrder = 4,
        },
        new()
        {
            Quote = "Took the Daily Driver plan. Eight washes a month for less than what my old cleaner charged, and no more missed Mondays.",
            Author = "Priya Nair", Car = "Honda City", Locality = "Magarpatta", Rating = 4, SortOrder = 5,
        },
        new()
        {
            Quote = "Underbody anti-rust before the monsoon was the best decision. Zero rust spots after the wettest July in years.",
            Author = "Vikram Patil", Car = "Mahindra XUV700", Locality = "Wakad", SortOrder = 6,
        },
    ];

    public static List<FaqItem> Faqs() =>
    [
        new()
        {
            Question = "Which areas of Pune do you cover?",
            Answer = "We run 14 detailing studios and 30+ doorstep vans across Pune — Baner, Aundh, Kothrud, Hinjewadi, Wakad, Pimple Saudagar, Viman Nagar, Kharadi, Magarpatta, Hadapsar, Koregaon Park, Camp, Deccan, Karve Nagar, Sinhagad Road, Katraj, Undri and Chinchwad. Enter your locality at checkout and we will confirm the nearest studio.",
            SortOrder = 1,
        },
        new()
        {
            Question = "How much does a basic car wash cost?",
            Answer = "An Express Body Wash starts at ₹349 for a hatchback, ₹400 for a sedan and ₹470 for an SUV. Snow Foam Wash starts at ₹649. Every quote you see on the site is the final amount — no GST surprises, no 'consumables' line item at the counter.",
            SortOrder = 2,
        },
        new()
        {
            Question = "Is doorstep service really free?",
            Answer = "Pickup and drop is free on all services above ₹999 and on every subscription plan. Below that it is a flat ₹149 within 8 km of your nearest studio. The Doorstep Waterless Wash happens at your parking spot, so nothing moves at all.",
            SortOrder = 3,
        },
        new()
        {
            Question = "What water and chemicals do you use?",
            Answer = "RO-treated water for the final rinse, pH-neutral biodegradable shampoos, and 3M / Meguiar's compounds for paint work. Our studios recycle roughly 70% of wash water, and the waterless doorstep wash uses under 2 litres per car.",
            SortOrder = 4,
        },
        new()
        {
            Question = "Will polishing or coating damage my paint?",
            Answer = "No. Paint thickness is measured with a gauge before any correction work, and we never remove more clear coat than is safe. You get before-and-after readings in your job card. If a panel is too thin to correct, we tell you instead of cutting into it.",
            SortOrder = 5,
        },
        new()
        {
            Question = "How do I pay, and can I get a rewash?",
            Answer = "UPI, cards, net banking or cash after the job is approved by you. Wash services carry a free rewash if you are unhappy within 24–48 hours, and coatings carry written warranties of 6 months to 5 years depending on the product.",
            SortOrder = 6,
        },
        new()
        {
            Question = "Can I reschedule or cancel a booking?",
            Answer = "Reschedule or cancel free of charge up to 4 hours before your slot from the booking link we WhatsApp you. Cancellations inside 4 hours on paint and coating jobs carry a ₹500 bay-blocking fee, since the bay is held for two full days.",
            SortOrder = 7,
        },
    ];

    public static List<Locality> Localities()
    {
        string[] names =
        [
            "Baner", "Aundh", "Kothrud", "Hinjewadi", "Wakad", "Pimple Saudagar",
            "Viman Nagar", "Kharadi", "Magarpatta", "Hadapsar", "Koregaon Park",
            "Camp", "Deccan", "Karve Nagar", "Sinhagad Road", "Katraj", "Undri", "Chinchwad",
        ];

        return [.. names.Select((name, i) => new Locality { Name = name, City = "Pune", SortOrder = i + 1 })];
    }

    public static List<CarBrand> CarBrands()
    {
        // (brand, [(model, bodyType), …]) — flattened here to keep the seed readable.
        (string Brand, (string Model, string Body)[] Models)[] source =
        [
            ("Maruti Suzuki",
            [
                ("Alto K10", "Hatchback"), ("Swift", "Hatchback"), ("Baleno", "Hatchback"),
                ("Wagon R", "Hatchback"), ("Dzire", "Sedan"), ("Ciaz", "Sedan"),
                ("Brezza", "SUV"), ("Grand Vitara", "SUV"), ("Ertiga", "SUV"),
            ]),
            ("Hyundai",
            [
                ("i10 Nios", "Hatchback"), ("i20", "Hatchback"), ("Aura", "Sedan"),
                ("Verna", "Sedan"), ("Venue", "SUV"), ("Creta", "SUV"), ("Alcazar", "SUV"),
            ]),
            ("Tata",
            [
                ("Tiago", "Hatchback"), ("Altroz", "Hatchback"), ("Tigor", "Sedan"),
                ("Punch", "SUV"), ("Nexon", "SUV"), ("Curvv", "SUV"),
                ("Harrier", "SUV"), ("Safari", "SUV"),
            ]),
            ("Mahindra",
            [
                ("XUV300", "SUV"), ("XUV700", "SUV"), ("Thar", "SUV"),
                ("Thar Roxx", "SUV"), ("Scorpio-N", "SUV"), ("Bolero", "SUV"),
            ]),
            ("Honda",
            [
                ("Amaze", "Sedan"), ("City", "Sedan"), ("Elevate", "SUV"),
            ]),
            ("Toyota",
            [
                ("Glanza", "Hatchback"), ("Taisor", "SUV"), ("Urban Cruiser Hyryder", "SUV"),
                ("Innova Crysta", "SUV"), ("Innova Hycross", "SUV"), ("Fortuner", "SUV"),
            ]),
            ("Kia",
            [
                ("Sonet", "SUV"), ("Syros", "SUV"), ("Seltos", "SUV"), ("Carens", "SUV"),
            ]),
            ("Volkswagen",
            [
                ("Virtus", "Sedan"), ("Taigun", "SUV"),
            ]),
            ("Skoda",
            [
                ("Slavia", "Sedan"), ("Kylaq", "SUV"), ("Kushaq", "SUV"), ("Kodiaq", "SUV"),
            ]),
            ("MG",
            [
                ("Astor", "SUV"), ("Hector", "SUV"), ("Windsor", "SUV"),
            ]),
            ("BMW",
            [
                ("3 Series", "Luxury"), ("5 Series", "Luxury"), ("X1", "Luxury"), ("X5", "Luxury"),
            ]),
            ("Mercedes-Benz",
            [
                ("A-Class", "Luxury"), ("C-Class", "Luxury"), ("E-Class", "Luxury"), ("GLC", "Luxury"),
            ]),
            ("Audi",
            [
                ("A4", "Luxury"), ("Q3", "Luxury"), ("Q5", "Luxury"),
            ]),
        ];

        return
        [
            .. source.Select((entry, brandIndex) => new CarBrand
            {
                Name = entry.Brand,
                SortOrder = brandIndex + 1,
                Models =
                [
                    .. entry.Models.Select((m, i) => new CarModel
                    {
                        Name = m.Model,
                        BodyType = m.Body,
                        SortOrder = i + 1,
                    }),
                ],
            }),
        ];
    }
}
