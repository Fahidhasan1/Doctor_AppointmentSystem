// Admin Dashboard JS

(function () {
    const menuToggle = document.getElementById("menuToggle");
    const sidebar = document.getElementById("sidebar");
    const dropdownToggle = document.querySelector("[data-dropdown]");
    const submenu = document.querySelector(".nav-submenu");

    // Sidebar toggle (mobile)
    if (menuToggle && sidebar) {
        menuToggle.addEventListener("click", () => {
            sidebar.classList.toggle("sidebar--open");
        });
    }

    // Dropdown "Manage Users"
    if (dropdownToggle && submenu) {
        dropdownToggle.addEventListener("click", () => {
            dropdownToggle.classList.toggle("open");
            submenu.classList.toggle("open");
        });
    }

    // Topbar date
    function updateDate() {
        const dateElem = document.getElementById("currentDate");
        if (!dateElem) return;

        const now = new Date();
        const options = {
            weekday: "long",
            day: "2-digit",
            month: "short",
            year: "numeric"
        };
        dateElem.textContent = now.toLocaleDateString("en-US", options);
    }

    updateDate();
    setInterval(updateDate, 60000);

    // Stat card active state
    const statCards = document.querySelectorAll(".stat-card");
    statCards.forEach(card => {
        card.addEventListener("click", () => {
            statCards.forEach(c => c.classList.remove("stat-card--active"));
            card.classList.add("stat-card--active");
        });
    });

    // ==== Charts (Chart.js) ====
    if (typeof Chart !== "undefined") {
        const revenueValues = window.dashboardRevenueData || [];
        const appointmentValues = window.dashboardAppointmentData || [];

        // Revenue bar chart
        const revCanvas = document.getElementById("revenueChart");
        if (revCanvas) {
            const revCtx = revCanvas.getContext("2d");
            const revGradient = revCtx.createLinearGradient(0, 0, 0, 230);
            revGradient.addColorStop(0, "#7ea5ff");
            revGradient.addColorStop(1, "#4f7aff");

            new Chart(revCtx, {
                type: "bar",
                data: {
                    labels: ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"],
                    datasets: [{
                        data: revenueValues,
                        backgroundColor: function (ctx) {
                            const index = ctx.dataIndex;
                            if (index === 10 || index === 11) {
                                const grad = revCtx.createLinearGradient(0, 0, 0, 230);
                                grad.addColorStop(0, "#59d0ff");
                                grad.addColorStop(1, "#1b71ff");
                                return grad;
                            }
                            return revGradient;
                        },
                        borderRadius: 10,
                        borderSkipped: false
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: { display: false },
                        tooltip: {
                            callbacks: {
                                label: function (context) {
                                    const value = context.raw;
                                    return "৳ " + Number(value || 0).toLocaleString();
                                }
                            }
                        }
                    },
                    layout: {
                        padding: { top: 8, right: 8, left: 0, bottom: 0 }
                    },
                    scales: {
                        x: {
                            grid: { display: false, drawBorder: false },
                            ticks: {
                                color: "#7b88b0",
                                font: { size: 11 }
                            }
                        },
                        y: {
                            beginAtZero: true,
                            grid: {
                                color: "#edf1ff",
                                drawBorder: false
                            },
                            ticks: {
                                color: "#7b88b0",
                                font: { size: 11 },
                                callback: function (value) {
                                    return "৳ " + Number(value || 0).toLocaleString();
                                }
                            }
                        }
                    }
                }
            });
        }

        // Appointment line chart
        const appCanvas = document.getElementById("appointmentChart");
        if (appCanvas) {
            const appCtx = appCanvas.getContext("2d");
            const appGradient = appCtx.createLinearGradient(0, 0, 0, 230);
            appGradient.addColorStop(0, "rgba(93, 135, 255, 0.4)");
            appGradient.addColorStop(1, "rgba(93, 135, 255, 0.02)");

            new Chart(appCtx, {
                type: "line",
                data: {
                    labels: ["Jan", "Feb", "Mar", "Apr", "May", "Jun"],
                    datasets: [{
                        data: appointmentValues,
                        fill: true,
                        backgroundColor: appGradient,
                        borderColor: "#5d87ff",
                        borderWidth: 2,
                        tension: 0.4,
                        pointRadius: 4,
                        pointBackgroundColor: "#5d87ff",
                        pointBorderWidth: 0,
                        pointHoverRadius: 5
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: { display: false }
                    },
                    layout: {
                        padding: { top: 8, right: 8, left: 0, bottom: 8 }
                    },
                    scales: {
                        x: {
                            grid: { display: false, drawBorder: false },
                            ticks: {
                                color: "#7b88b0",
                                font: { size: 11 }
                            }
                        },
                        y: {
                            beginAtZero: true,
                            grid: {
                                color: "#edf1ff",
                                drawBorder: false
                            },
                            ticks: {
                                color: "#7b88b0",
                                font: { size: 11 }
                            }
                        }
                    }
                }
            });
        }
    }
})();
