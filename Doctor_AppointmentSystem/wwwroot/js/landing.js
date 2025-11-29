// Smooth scroll helper
function scrollToSection(id) {
    var section = document.getElementById(id);
    if (section) {
        section.scrollIntoView({ behavior: "smooth" });
    }
}

function openModal(modalId) {
    var overlay = document.getElementById(modalId);
    if (!overlay) return;
    overlay.classList.add("modal-open");
    document.body.classList.add("no-scroll");
}

function closeAllModals() {
    document.querySelectorAll(".auth-modal-overlay").forEach(function (ov) {
        ov.classList.remove("modal-open");
    });
    document.body.classList.remove("no-scroll");
}

document.addEventListener("DOMContentLoaded", function () {
    var appointmentBtn = document.querySelector(".js-appointment-btn");
    var bookBtn = document.querySelector(".js-book-btn");
    var navLinks = document.querySelectorAll(".nav-link");

    if (appointmentBtn) {
        appointmentBtn.addEventListener("click", function () {
            scrollToSection("signup"); // placeholder for actual appointment section
        });
    }

    if (bookBtn) {
        bookBtn.addEventListener("click", function () {
            scrollToSection("signin");
        });
    }

    navLinks.forEach(function (link) {
        link.addEventListener("click", function (e) {
            var href = this.getAttribute("href");
            if (!href || href.charAt(0) !== "#") return;

            var targetId = href.replace("#", "");

            if (targetId === "signin") {
                e.preventDefault();
                openModal("signinModal");
                return;
            }

            if (targetId === "signup") {
                e.preventDefault();
                openModal("signupModal");
                return;
            }

            if (targetId) {
                e.preventDefault();
                scrollToSection(targetId);

                navLinks.forEach(function (l) {
                    l.classList.remove("nav-active");
                });
                this.classList.add("nav-active");
            }
        });
    });

    // Navbar Sign Up button
    var navSignupBtn = document.querySelector('[data-open="signupModal"]');
    if (navSignupBtn) {
        navSignupBtn.addEventListener("click", function () {
            openModal("signupModal");
        });
    }

    // Close buttons & overlay click
    document.querySelectorAll("[data-close-modal]").forEach(function (btn) {
        btn.addEventListener("click", closeAllModals);
    });

    document.querySelectorAll(".auth-modal-overlay").forEach(function (overlay) {
        overlay.addEventListener("click", function (e) {
            if (e.target === overlay) {
                closeAllModals();
            }
        });
    });

    // ESC key closes modals
    document.addEventListener("keydown", function (e) {
        if (e.key === "Escape") {
            closeAllModals();
        }
    });

    // Switch between Sign In and Sign Up
    document.querySelectorAll("[data-switch-to]").forEach(function (btn) {
        btn.addEventListener("click", function () {
            var target = this.getAttribute("data-switch-to");
            closeAllModals();
            if (target === "signin") openModal("signinModal");
            if (target === "signup") openModal("signupModal");
        });
    });

    // Password show / hide on press
    document.querySelectorAll(".auth-password-toggle").forEach(function (toggleBtn) {
        var wrapper = toggleBtn.closest(".auth-input-wrap");
        if (!wrapper) return;
        var input = wrapper.querySelector("input");
        if (!input) return;

        function showPassword() {
            input.type = "text";
        }

        function hidePassword() {
            input.type = "password";
        }

        ["mousedown", "touchstart"].forEach(function (evt) {
            toggleBtn.addEventListener(evt, function (e) {
                e.preventDefault();
                showPassword();
            });
        });

        ["mouseup", "mouseleave", "touchend", "touchcancel"].forEach(function (evt) {
            toggleBtn.addEventListener(evt, function () {
                hidePassword();
            });
        });

        // Prevent click from submitting forms
        toggleBtn.addEventListener("click", function (e) {
            e.preventDefault();
        });
    });

    // About section highlight toggle
    var aboutCards = document.querySelectorAll(".about-highlight-item");
    aboutCards.forEach(function (card) {
        card.addEventListener("click", function () {
            aboutCards.forEach(function (c) { c.classList.remove("about-active"); });
            card.classList.add("about-active");
        });
    });

    // Doctor filter logic
    var doctorCards = document.querySelectorAll(".doctor-card");
    var filterName = document.getElementById("filterName");
    var filterSpecialty = document.getElementById("filterSpecialty");
    var filterExperience = document.getElementById("filterExperience");
    var resetBtn = document.getElementById("filterResetBtn");

    function applyDoctorFilters() {
        var nameValue = filterName.value.toLowerCase().trim();
        var specValue = filterSpecialty.value;
        var expValue = filterExperience.value;

        doctorCards.forEach(function (card) {
            var cardName = (card.getAttribute("data-name") || "").toLowerCase();
            var cardSpec = card.getAttribute("data-specialty") || "";
            var cardExp = parseInt(card.getAttribute("data-experience") || "0", 10);

            var matchesName = !nameValue || cardName.indexOf(nameValue) !== -1;
            var matchesSpec = specValue === "all" || specValue === "" || cardSpec === specValue;

            var matchesExp = true;
            if (expValue === "0-5") {
                matchesExp = cardExp <= 5;
            } else if (expValue === "5-10") {
                matchesExp = cardExp > 5 && cardExp <= 10;
            } else if (expValue === "10plus") {
                matchesExp = cardExp > 10;
            }

            if (matchesName && matchesSpec && matchesExp) {
                card.classList.remove("doctor-card-hidden");
            } else {
                card.classList.add("doctor-card-hidden");
            }
        });
    }

    if (filterName) filterName.addEventListener("keyup", applyDoctorFilters);
    if (filterSpecialty) filterSpecialty.addEventListener("change", applyDoctorFilters);
    if (filterExperience) filterExperience.addEventListener("change", applyDoctorFilters);
    if (resetBtn) {
        resetBtn.addEventListener("click", function () {
            filterName.value = "";
            filterSpecialty.value = "all";
            filterExperience.value = "all";
            applyDoctorFilters();
        });
    }
});
