//const menuToggle = document.getElementById("menuToggle");
//const sidebar = document.getElementById("sidebar");

//// ✅ get ALL dropdown toggles, not just the first one
//const dropdownToggles = document.querySelectorAll("[data-dropdown]");

//if (menuToggle && sidebar) {
//    menuToggle.addEventListener("click", () => {
//        sidebar.classList.toggle("sidebar--open");
//    });
//}

//if (dropdownToggles && dropdownToggles.length > 0) {
//    dropdownToggles.forEach(toggle => {
//        const dropdown = toggle.closest(".nav-dropdown");
//        if (!dropdown) return;

//        const submenu = dropdown.querySelector(".nav-submenu");
//        if (!submenu) return;

//        toggle.addEventListener("click", () => {
//            toggle.classList.toggle("open");
//            submenu.classList.toggle("open");
//        });
//    });
//}

//// Date display
//function updateDate() {
//    const el = document.getElementById("currentDate");
//    if (!el) return;
//    const now = new Date();
//    el.textContent = now.toLocaleDateString("en-US", {
//        weekday: "long",
//        day: "2-digit",
//        month: "short",
//        year: "numeric"
//    });
//}
//updateDate();
//setInterval(updateDate, 60000);

//// REVENUE CHART – BAR
//const revCanvas = document.getElementById('revenueChart');
//if (revCanvas) {
//    const revCtx = revCanvas.getContext('2d');
//    new Chart(revCtx, {
//        type: 'bar',
//        data: {
//            labels: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'],
//            datasets: [{
//                label: 'Revenue',
//                data: [150000, 140000, 180000, 160000, 190000, 210000, 205000, 220000, 230000, 240000, 245000, 250000],
//                backgroundColor: 'rgba(79, 125, 255, 0.7)',
//                borderRadius: 8,
//                maxBarThickness: 32
//            }]
//        },
//        options: {
//            plugins: {
//                legend: { display: false }
//            },
//            scales: {
//                x: {
//                    grid: { display: false },
//                    ticks: {
//                        color: '#7b88b0',
//                        font: { size: 11 }
//                    }
//                },
//                y: {
//                    grid: { color: 'rgba(221, 229, 255, 0.7)' },
//                    ticks: {
//                        color: '#7b88b0',
//                        font: { size: 11 },
//                        beginAtZero: true
//                    }
//                }
//            }
//        }
//    });
//}

//// APPOINTMENT CHART – LINE
//const apptCanvas = document.getElementById('appointmentChart');
//if (apptCanvas) {
//    const apptCtx = apptCanvas.getContext('2d');
//    const gradient = apptCtx.createLinearGradient(0, 0, 0, 230);
//    gradient.addColorStop(0, 'rgba(79, 125, 255, 0.32)');
//    gradient.addColorStop(1, 'rgba(79, 125, 255, 0.02)');

//    new Chart(apptCtx, {
//        type: 'line',
//        data: {
//            labels: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun'],
//            datasets: [{
//                label: 'Appointments',
//                data: [40, 65, 55, 70, 68, 95],
//                fill: true,
//                backgroundColor: gradient,
//                borderColor: 'rgba(79, 125, 255, 1)',
//                tension: 0.35,
//                borderWidth: 2,
//                pointRadius: 3.5,
//                pointBackgroundColor: '#ffffff',
//                pointBorderColor: 'rgba(79, 125, 255, 1)',
//                pointBorderWidth: 2
//            }]
//        },
//        options: {
//            plugins: {
//                legend: { display: false }
//            },
//            scales: {
//                x: {
//                    grid: { display: false },
//                    ticks: {
//                        color: '#7b88b0',
//                        font: { size: 11 }
//                    }
//                },
//                y: {
//                    grid: { color: 'rgba(221, 229, 255, 0.7)' },
//                    ticks: {
//                        color: '#7b88b0',
//                        font: { size: 11 },
//                        beginAtZero: true
//                    }
//                }
//            }
//        }
//    });
//}

//// Make stat cards clickable using data-url
//document.addEventListener("DOMContentLoaded", function () {
//    const cards = document.querySelectorAll(".stat-card[data-url]");
//    cards.forEach(card => {
//        const url = card.getAttribute("data-url");
//        if (!url) return;
//        card.style.cursor = "pointer";
//        card.addEventListener("click", () => {
//            window.location.href = url;
//        });
//    });
//});

//// Auto-hide toast messages after 3 seconds
//document.addEventListener("DOMContentLoaded", () => {
//    const toast = document.querySelector(".alert-success-soft");
//    if (toast) {
//        setTimeout(() => {
//            toast.classList.add("toast-hidden");
//        }, 3000); // 3 seconds before fade-out

//        // Remove from DOM after fade
//        setTimeout(() => {
//            if (toast.parentNode) {
//                toast.parentNode.removeChild(toast);
//            }
//        }, 4000); // fully removed after fade animation
//    }
//});


const menuToggle = document.getElementById("menuToggle");
const sidebar = document.getElementById("sidebar");

// ✅ get ALL dropdown toggles, not just the first one
const dropdownToggles = document.querySelectorAll("[data-dropdown]");

if (menuToggle && sidebar) {
    menuToggle.addEventListener("click", () => {
        sidebar.classList.toggle("sidebar--open");
    });
}

if (dropdownToggles && dropdownToggles.length > 0) {
    dropdownToggles.forEach(toggle => {
        const dropdown = toggle.closest(".nav-dropdown");
        if (!dropdown) return;

        const submenu = dropdown.querySelector(".nav-submenu");
        if (!submenu) return;

        toggle.addEventListener("click", () => {
            toggle.classList.toggle("open");
            submenu.classList.toggle("open");
        });
    });
}

// Date display
function updateDate() {
    const el = document.getElementById("currentDate");
    if (!el) return;
    const now = new Date();
    el.textContent = now.toLocaleDateString("en-US", {
        weekday: "long",
        day: "2-digit",
        month: "short",
        year: "numeric"
    });
}
updateDate();
setInterval(updateDate, 60000);

// ===============================
// ✅ Dynamic Chart Data (from Razor)
// window.adminDashboardData = {
//    labels: ["Jan",...],
//    revenue: [..12..],
//    appointments: [..12..]
// }
// ===============================
const dash = window.adminDashboardData || null;

// Fallbacks (your original static data)
const fallbackLabels12 = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
const fallbackRevenue12 = [150000, 140000, 180000, 160000, 190000, 210000, 205000, 220000, 230000, 240000, 245000, 250000];
const fallbackAppt6Labels = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun'];
const fallbackAppt6Data = [40, 65, 55, 70, 68, 95];

// Use dynamic if present
const labels12 = Array.isArray(dash?.labels) && dash.labels.length ? dash.labels : fallbackLabels12;
const revenue12 = Array.isArray(dash?.revenue) && dash.revenue.length ? dash.revenue : fallbackRevenue12;

// For appointments chart, your UI currently shows only 6 months in screenshot.
// If backend returns 12 months, we will display last 6 months to keep the same UI.
let apptLabelsForChart = fallbackAppt6Labels;
let apptDataForChart = fallbackAppt6Data;

if (Array.isArray(dash?.appointments) && dash.appointments.length) {
    const apptAll = dash.appointments;
    const lblAll = labels12;

    if (apptAll.length >= 6 && lblAll.length >= 6) {
        apptLabelsForChart = lblAll.slice(-6);
        apptDataForChart = apptAll.slice(-6);
    } else {
        // if for some reason backend sends less than 6, just use what is available
        apptLabelsForChart = lblAll;
        apptDataForChart = apptAll;
    }
}

// REVENUE CHART – BAR
const revCanvas = document.getElementById('revenueChart');
if (revCanvas) {
    const revCtx = revCanvas.getContext('2d');

    new Chart(revCtx, {
        type: 'bar',
        data: {
            labels: labels12,
            datasets: [{
                label: 'Revenue',
                data: revenue12,
                backgroundColor: 'rgba(79, 125, 255, 0.7)',
                borderRadius: 8,
                maxBarThickness: 32
            }]
        },
        options: {
            plugins: {
                legend: { display: false }
            },
            scales: {
                x: {
                    grid: { display: false },
                    ticks: {
                        color: '#7b88b0',
                        font: { size: 11 }
                    }
                },
                y: {
                    beginAtZero: true,
                    grid: { color: 'rgba(221, 229, 255, 0.7)' },
                    ticks: {
                        color: '#7b88b0',
                        font: { size: 11 }
                    }
                }

            }
        }
    });
}

// APPOINTMENT CHART – LINE
const apptCanvas = document.getElementById('appointmentChart');
if (apptCanvas) {
    const apptCtx = apptCanvas.getContext('2d');

    const gradient = apptCtx.createLinearGradient(0, 0, 0, 230);
    gradient.addColorStop(0, 'rgba(79, 125, 255, 0.32)');
    gradient.addColorStop(1, 'rgba(79, 125, 255, 0.02)');

    new Chart(apptCtx, {
        type: 'line',
        data: {
            labels: apptLabelsForChart,
            datasets: [{
                label: 'Appointments',
                data: apptDataForChart,
                fill: true,
                backgroundColor: gradient,
                borderColor: 'rgba(79, 125, 255, 1)',
                tension: 0.35,
                borderWidth: 2,
                pointRadius: 3.5,
                pointBackgroundColor: '#ffffff',
                pointBorderColor: 'rgba(79, 125, 255, 1)',
                pointBorderWidth: 2
            }]
        },
        options: {
            plugins: {
                legend: { display: false }
            },
            scales: {
                x: {
                    grid: { display: false },
                    ticks: {
                        color: '#7b88b0',
                        font: { size: 11 }
                    }
                },
                y: {
                    grid: { color: 'rgba(221, 229, 255, 0.7)' },
                    ticks: {
                        color: '#7b88b0',
                        font: { size: 11 },
                        beginAtZero: true
                    }
                }
            }
        }
    });
}

// Make stat cards clickable using data-url
document.addEventListener("DOMContentLoaded", function () {
    const cards = document.querySelectorAll(".stat-card[data-url]");
    cards.forEach(card => {
        const url = card.getAttribute("data-url");
        if (!url) return;
        card.style.cursor = "pointer";
        card.addEventListener("click", () => {
            window.location.href = url;
        });
    });
});

// Auto-hide toast messages after 3 seconds
document.addEventListener("DOMContentLoaded", () => {
    const toast = document.querySelector(".alert-success-soft");
    if (toast) {
        setTimeout(() => {
            toast.classList.add("toast-hidden");
        }, 3000); // 3 seconds before fade-out

        // Remove from DOM after fade
        setTimeout(() => {
            if (toast.parentNode) {
                toast.parentNode.removeChild(toast);
            }
        }, 4000); // fully removed after fade animation
    }
});
