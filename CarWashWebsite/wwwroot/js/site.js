/* =============================================================================
   PitStop Autocare — progressive enhancement only.
   Every feature here degrades to a working page if JS fails: the booking form
   posts normally, FAQ panels render open-by-default server-side, and the
   carousels are plain horizontal scrollers.
   ========================================================================== */
(function () {
    "use strict";

    const $ = (sel, root) => (root || document).querySelector(sel);
    const $$ = (sel, root) => Array.from((root || document).querySelectorAll(sel));

    const rupees = (value) => "₹" + Math.round(value).toLocaleString("en-IN");

    function readJson(id, fallback) {
        const el = document.getElementById(id);
        if (!el) return fallback;
        try {
            return JSON.parse(el.textContent);
        } catch {
            return fallback;
        }
    }

    /* ------------------------------------------------------------ toast */
    let toastTimer;
    function toast(message) {
        const el = $("#toast");
        if (!el) return;
        el.textContent = message;
        el.hidden = false;
        requestAnimationFrame(() => el.classList.add("is-visible"));
        clearTimeout(toastTimer);
        toastTimer = setTimeout(() => {
            el.classList.remove("is-visible");
            setTimeout(() => { el.hidden = true; }, 250);
        }, 3200);
    }

    /* ------------------------------------------------------------ header */
    function initHeader() {
        const header = $("#siteHeader");
        if (header) {
            const onScroll = () => header.classList.toggle("is-stuck", window.scrollY > 4);
            window.addEventListener("scroll", onScroll, { passive: true });
            onScroll();
        }

        const toggle = $("#navToggle");
        const nav = $("#siteNav");
        if (toggle && nav) {
            toggle.addEventListener("click", () => {
                const open = nav.classList.toggle("is-open");
                toggle.setAttribute("aria-expanded", String(open));
                toggle.setAttribute("aria-label", open ? "Close menu" : "Open menu");
            });
            nav.addEventListener("click", (e) => {
                if (e.target.tagName === "A") {
                    nav.classList.remove("is-open");
                    toggle.setAttribute("aria-expanded", "false");
                }
            });
        }
    }

    /* ------------------------------------------------------------ city menu */
    function initCityMenu() {
        const button = $("#citySelect");
        const menu = $("#cityMenu");
        const label = $("#cityLabel");
        if (!button || !menu || !label) return;

        const close = () => {
            menu.hidden = true;
            button.setAttribute("aria-expanded", "false");
        };

        button.addEventListener("click", (e) => {
            e.stopPropagation();
            const open = menu.hidden;
            menu.hidden = !open;
            button.setAttribute("aria-expanded", String(open));
        });

        menu.addEventListener("click", (e) => {
            const option = e.target.closest("[data-city]");
            if (!option) return;
            const city = option.dataset.city;

            $$("[data-city]", menu).forEach((li) =>
                li.setAttribute("aria-selected", String(li === option)));
            label.textContent = city;
            close();

            if (city !== "Pune") {
                toast(city + " launches soon — showing Pune prices for now.");
            }
        });

        document.addEventListener("click", (e) => {
            if (!menu.hidden && !menu.contains(e.target) && e.target !== button) close();
        });
        document.addEventListener("keydown", (e) => {
            if (e.key === "Escape" && !menu.hidden) { close(); button.focus(); }
        });
    }

    /* ------------------------------------------------------------ copy code */
    function initCopyCode() {
        $$(".code-copy").forEach((btn) => {
            btn.addEventListener("click", async () => {
                const code = btn.dataset.code || btn.textContent.trim();
                try {
                    await navigator.clipboard.writeText(code);
                    toast("Code " + code + " copied. Use it at checkout.");
                } catch {
                    toast("Your code is " + code);
                }
            });
        });
    }

    /* ------------------------------------------------------------ accordion */
    function initAccordions() {
        $$(".accordion__trigger").forEach((trigger) => {
            trigger.addEventListener("click", () => {
                const panel = document.getElementById(trigger.getAttribute("aria-controls"));
                const isOpen = trigger.getAttribute("aria-expanded") === "true";

                // One panel at a time, per accordion group.
                const group = trigger.closest(".accordion");
                if (group && !isOpen) {
                    $$(".accordion__trigger", group).forEach((other) => {
                        if (other === trigger) return;
                        other.setAttribute("aria-expanded", "false");
                        const otherPanel = document.getElementById(other.getAttribute("aria-controls"));
                        if (otherPanel) otherPanel.hidden = true;
                    });
                }

                trigger.setAttribute("aria-expanded", String(!isOpen));
                if (panel) panel.hidden = isOpen;
            });
        });
    }

    /* ------------------------------------------------------------ carousels */
    function initCarousels() {
        $$("[data-carousel]").forEach((track) => {
            const name = track.dataset.carousel;
            const prev = $('[data-carousel-prev="' + name + '"]');
            const next = $('[data-carousel-next="' + name + '"]');

            const step = () => {
                const first = track.firstElementChild;
                if (!first) return track.clientWidth;
                const gap = parseFloat(getComputedStyle(track).columnGap) || 18;
                return first.getBoundingClientRect().width + gap;
            };

            const sync = () => {
                const max = track.scrollWidth - track.clientWidth - 2;
                if (prev) prev.disabled = track.scrollLeft <= 2;
                if (next) next.disabled = track.scrollLeft >= max;
            };

            if (prev) prev.addEventListener("click", () => track.scrollBy({ left: -step(), behavior: "smooth" }));
            if (next) next.addEventListener("click", () => track.scrollBy({ left: step(), behavior: "smooth" }));

            track.addEventListener("scroll", sync, { passive: true });
            window.addEventListener("resize", sync);
            sync();
        });
    }

    /* ------------------------------------------------------------ car data
       Populated in init(), not at load time: this script tag sits above the
       JSON <script> blocks that the views render in the Scripts section. */
    let CARS = [];
    let MULTIPLIERS = { Hatchback: 1, Sedan: 1.15, SUV: 1.35, Luxury: 1.75 };

    function findBrand(name) {
        return CARS.find((b) => b.name === name);
    }

    function bodyTypeOf(brandName, modelName) {
        const brand = findBrand(brandName);
        const model = brand && brand.models.find((m) => m.name === modelName);
        return model ? model.bodyType : null;
    }

    function multiplierFor(bodyType) {
        return MULTIPLIERS[bodyType] || 1;
    }

    /** Matches CatalogService.PriceFor — nearest ₹10. */
    function priceFor(base, bodyType) {
        return Math.round((base * multiplierFor(bodyType)) / 10) * 10;
    }

    function fillModels(select, brandName, selected) {
        const brand = findBrand(brandName);
        select.innerHTML = "";

        if (!brand) {
            select.appendChild(new Option("Select brand first", ""));
            select.disabled = true;
            return;
        }

        select.disabled = false;
        select.appendChild(new Option("Select model", ""));
        brand.models.forEach((m) => {
            const opt = new Option(m.name + " · " + m.bodyType, m.name);
            if (m.name === selected) opt.selected = true;
            select.appendChild(opt);
        });
    }

    /* ------------------------------------------------------------ hero picker */
    function initHeroPicker() {
        const form = $("#carPicker");
        if (!form) return;

        const brandSel = $("#pickBrand");
        const modelSel = $("#pickModel");
        const serviceSel = $("#pickService");
        const box = $("#quoteBox");
        const submit = $("#pickerSubmit");

        const remembered = loadCar();
        if (remembered && findBrand(remembered.brand)) {
            brandSel.value = remembered.brand;
            fillModels(modelSel, remembered.brand, remembered.model);
        }

        brandSel.addEventListener("change", () => {
            fillModels(modelSel, brandSel.value);
            render();
        });
        modelSel.addEventListener("change", render);
        serviceSel.addEventListener("change", render);

        function render() {
            const bodyType = bodyTypeOf(brandSel.value, modelSel.value);
            if (!bodyType) {
                box.hidden = true;
                submit.textContent = "Check price for free";
                return;
            }

            const option = serviceSel.selectedOptions[0];
            const slug = serviceSel.value;
            saveCar({ brand: brandSel.value, model: modelSel.value });

            // Optimistic render from the DOM, then confirm against the API so the
            // number always matches what the server would charge.
            box.hidden = false;
            $("#quoteBody").textContent = bodyType;
            $("#quoteLabel").textContent = "Your price · " + brandSel.value + " " + modelSel.value;

            fetch("/api/quote?serviceSlug=" + encodeURIComponent(slug) +
                  "&bodyType=" + encodeURIComponent(bodyType))
                .then((r) => (r.ok ? r.json() : Promise.reject()))
                .then((q) => {
                    $("#quotePrice").textContent = rupees(q.price);
                    $("#quoteList").textContent = rupees(q.listPrice);
                    $("#quoteSave").textContent = q.savingsPercent + "% off";
                    $("#quoteDuration").textContent = q.duration;
                    $("#quoteWarranty").textContent = q.warranty;
                })
                .catch(() => {
                    $("#quotePrice").textContent = "₹—";
                    $("#quoteList").textContent = "";
                    $("#quoteSave").textContent = "";
                    $("#quoteDuration").textContent = option ? option.textContent : "—";
                    $("#quoteWarranty").textContent = "Call us for an exact quote";
                });

            submit.textContent = "Book this wash";
        }

        form.addEventListener("submit", (e) => {
            e.preventDefault();

            if (!brandSel.value) {
                toast("Pick your car's brand first.");
                brandSel.focus();
                return;
            }
            if (!modelSel.value) {
                toast("Pick your car's model.");
                modelSel.focus();
                return;
            }

            window.location.href = "/book?service=" + encodeURIComponent(serviceSel.value);
        });

        render();
    }

    /* ------------------------------------------------------------ remembered car */
    const STORAGE_KEY = "pitstop.car";

    function saveCar(car) {
        try { localStorage.setItem(STORAGE_KEY, JSON.stringify(car)); } catch { /* private mode */ }
    }

    function loadCar() {
        try { return JSON.parse(localStorage.getItem(STORAGE_KEY) || "null"); } catch { return null; }
    }

    /* ------------------------------------------------------------ price table */
    function initPriceTable() {
        const table = $("#priceTable");
        const buttons = $$(".segmented [data-body]");
        if (!table || !buttons.length) return;

        buttons.forEach((btn) => {
            btn.addEventListener("click", () => {
                buttons.forEach((b) => {
                    const active = b === btn;
                    b.classList.toggle("is-active", active);
                    b.setAttribute("aria-selected", String(active));
                });

                const bodyType = btn.dataset.body;
                $$("tbody tr", table).forEach((row) => {
                    const base = Number(row.dataset.base);
                    const list = Number(row.dataset.list);
                    $("[data-price]", row).textContent = rupees(priceFor(base, bodyType));
                    $("[data-list-price]", row).textContent = rupees(priceFor(list, bodyType));
                });
            });
        });
    }

    /* ------------------------------------------------------------ booking page */
    function initBookingPage() {
        const form = $("#bookForm");
        if (!form) return;

        const brandSel = $("#bookBrand");
        const modelSel = $("#bookModel");
        const preselect = readJson("preselect", {});
        const remembered = loadCar();

        const initialBrand = (preselect.brand && findBrand(preselect.brand) && preselect.brand)
            || (remembered && findBrand(remembered.brand) && remembered.brand)
            || "";
        const initialModel = preselect.model || (remembered && remembered.model) || "";

        if (initialBrand) {
            brandSel.value = initialBrand;
            fillModels(modelSel, initialBrand, initialModel);
        } else {
            fillModels(modelSel, "");
        }

        brandSel.addEventListener("change", () => {
            fillModels(modelSel, brandSel.value);
            updateSummary();
        });

        form.addEventListener("change", updateSummary);
        form.addEventListener("input", updateSummary);

        function selectedService() {
            const radio = $('input[name="Form.ServiceSlug"]:checked', form);
            if (!radio) return null;
            const card = radio.closest(".radio-card");
            return {
                slug: radio.value,
                name: $(".radio-card__text strong", card).textContent,
                base: Number(radio.dataset.base),
                duration: radio.dataset.duration,
            };
        }

        function updateSummary() {
            const brand = brandSel.value;
            const model = modelSel.value;
            const bodyType = bodyTypeOf(brand, model);
            const service = selectedService();
            const locality = $('select[name="Form.Locality"]', form).value;
            const date = $('input[name="Form.PreferredDate"]', form).value;
            const slotInput = $('input[name="Form.PreferredSlot"]:checked', form);

            $("#sumCar").textContent = brand && model ? brand + " " + model : "—";
            $("#sumBody").textContent = bodyType || "—";
            $("#sumService").textContent = service ? service.name : "—";
            $("#sumWhere").textContent = locality ? locality + ", Pune" : "—";

            let when = "—";
            if (date) {
                const parsed = new Date(date + "T00:00:00");
                when = parsed.toLocaleDateString("en-IN", { weekday: "short", day: "numeric", month: "short" });
                if (slotInput) when += " · " + slotInput.value;
            } else if (slotInput) {
                when = slotInput.value;
            }
            $("#sumWhen").textContent = when;

            if (bodyType && service) {
                $("#sumTotal").textContent = rupees(priceFor(service.base, bodyType));
                $("#sumNote").textContent = service.name + " takes about " + service.duration +
                    ". Final amount is confirmed on inspection and includes GST.";
                if (brand && model) saveCar({ brand: brand, model: model });
            } else {
                $("#sumTotal").textContent = "₹—";
                $("#sumNote").textContent =
                    "Pick your car to see the exact price. Prices scale with body type — SUVs cost more to wash than hatchbacks.";
            }

            // Prices on the service cards follow the selected body type.
            const factor = bodyType ? multiplierFor(bodyType) : 1;
            $$(".radio-card input[data-base]", form).forEach((input) => {
                const target = $("[data-service-price]", input.closest(".radio-card"));
                if (target) {
                    target.textContent = (Math.round((Number(input.dataset.base) * factor) / 10) * 10)
                        .toLocaleString("en-IN");
                }
            });
        }

        /* Client-side validation mirrors the DataAnnotations on BookingRequest so the
           user is not bounced to a reloaded page for a missing field. */
        const rules = [
            { name: "Form.CarBrand", message: "Pick your car's brand." },
            { name: "Form.CarModel", message: "Pick your car's model." },
            { name: "Form.ServiceSlug", message: "Choose a service." },
            { name: "Form.Locality", message: "Choose your area." },
            { name: "Form.PreferredDate", message: "Pick a date." },
            { name: "Form.PreferredSlot", message: "Pick a time slot." },
            { name: "Form.Name", message: "Tell us your name.", test: (v) => v.trim().length >= 2 },
            {
                name: "Form.Phone",
                message: "Enter a valid 10-digit Indian mobile number.",
                test: (v) => /^[6-9]\d{9}$/.test(v.trim()),
            },
        ];

        function fieldError(name, message) {
            const input = form.querySelector('[name="' + name + '"]');
            if (!input) return;
            const holder = input.closest(".field") || input.closest("fieldset");
            const span = holder && holder.querySelector(".field__error");
            if (span) span.textContent = message || "";
            if (input.tagName !== "INPUT" || input.type !== "radio") {
                input.classList.toggle("is-invalid", Boolean(message));
            }
        }

        form.addEventListener("submit", async (e) => {
            let firstBad = null;

            rules.forEach((rule) => {
                const nodes = $$('[name="' + rule.name + '"]', form);
                const value = nodes.length && nodes[0].type === "radio"
                    ? (form.querySelector('[name="' + rule.name + '"]:checked') || {}).value || ""
                    : (nodes[0] ? nodes[0].value : "");

                const ok = rule.test ? rule.test(value) : Boolean(value);
                fieldError(rule.name, ok ? "" : rule.message);
                if (!ok && !firstBad) firstBad = nodes[0];
            });

            if (firstBad) {
                e.preventDefault();
                firstBad.scrollIntoView({ behavior: "smooth", block: "center" });
                if (firstBad.focus) firstBad.focus({ preventScroll: true });
                toast("A few fields still need filling in.");
                return;
            }

            // Post via the JSON API for an instant response; fall back to the
            // normal form post if anything goes wrong.
            e.preventDefault();
            const submit = $("#bookSubmit");
            submit.disabled = true;
            submit.textContent = "Booking…";

            const payload = {
                name: $('[name="Form.Name"]', form).value,
                phone: $('[name="Form.Phone"]', form).value,
                email: $('[name="Form.Email"]', form).value || null,
                carBrand: brandSel.value,
                carModel: modelSel.value,
                serviceSlug: $('input[name="Form.ServiceSlug"]:checked', form).value,
                locality: $('select[name="Form.Locality"]', form).value,
                preferredDate: $('input[name="Form.PreferredDate"]', form).value,
                preferredSlot: $('input[name="Form.PreferredSlot"]:checked', form).value,
                notes: $('[name="Form.Notes"]', form).value || null,
            };

            try {
                const response = await fetch("/api/bookings", {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify(payload),
                });

                if (!response.ok) throw new Error("api");

                const result = await response.json();
                window.location.href = "/book/confirmed/" + encodeURIComponent(result.reference);
            } catch {
                submit.disabled = false;
                submit.textContent = "Confirm booking";
                form.submit(); // server-rendered path, with anti-forgery token
            }
        });

        updateSummary();
    }

    /* ------------------------------------------------------------ boot */
    function init() {
        CARS = readJson("carData", CARS);
        MULTIPLIERS = readJson("bodyMultipliers", MULTIPLIERS);

        initHeader();
        initCityMenu();
        initCopyCode();
        initAccordions();
        initCarousels();
        initHeroPicker();
        initPriceTable();
        initBookingPage();
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})();
