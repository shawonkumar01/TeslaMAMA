window.TeslaMAMA = window.TeslaMAMA || {};

TeslaMAMA.Overview = (function () {
    const STATUS_LABELS = {
        Completed: "Completed",
        Cancelled: "Cancelled",
        NoDriverAvailable: "No driver found",
        Requested: "Requested",
        Matched: "Matched",
        DriverArrived: "Driver arrived",
        Started: "In progress",
        WaitingForPayment: "Awaiting payment"
    };

    const PAYMENT_LABELS = {
        Cash: "Cash",
        Card: "Card",
        MobileBanking: "Mobile banking"
    };

    // green = good, grey = passive, amber = attention, blue = in progress
    const STATUS_COLORS = {
        Completed: "#1a6e55",
        Cancelled: "#9aa8a1",
        NoDriverAvailable: "#f5b83d",
        Requested: "#2b5a9c",
        Matched: "#2b5a9c",
        DriverArrived: "#2b5a9c",
        Started: "#2b5a9c",
        WaitingForPayment: "#2b5a9c"
    };

    const COLORS = ['#1a6e55', '#a9e5cb', '#2b5a9c', '#f5b83d', '#ef5b4c', '#5f7169'];
    const GRID_COLOR = '#e6eee9';
    const FONT_FAMILY = '"Inter", "Noto Sans Bengali", "Nirmala UI", "Segoe UI", system-ui, sans-serif';
    const formatter = new Intl.NumberFormat('en-BD');

    const pretty = (key, map) => map[key] ?? key.replace(/([a-z])([A-Z])/g, "$1 $2");

    function formatCurrency(value) {
        return '৳' + formatter.format(value);
    }

    // Plugin for doughnut centre text
    const centreTextPlugin = {
        id: 'centreText',
        beforeDraw: function (chart) {
            if (chart.config.type !== 'doughnut') return;

            const width = chart.width;
            const height = chart.height;
            const ctx = chart.ctx;

            ctx.restore();
            const fontSize = (height / 114).toFixed(2);
            ctx.font = `bold ${fontSize}em ${FONT_FAMILY}`;
            ctx.textBaseline = 'middle';
            ctx.fillStyle = '#1b2b26';

            const text = chart.data.datasets[0].data.reduce((a, b) => a + b, 0);
            const textX = Math.round((width - ctx.measureText(text).width) / 2);
            const textY = height / 2 - 8;

            ctx.fillText(text, textX, textY);

            // Add "rides" label below
            const labelFontSize = (height / 280).toFixed(2);
            ctx.font = `${labelFontSize}em ${FONT_FAMILY}`;
            ctx.fillStyle = '#5f7169';
            const labelText = 'rides';
            const labelX = Math.round((width - ctx.measureText(labelText).width) / 2);
            const labelY = height / 2 + 12;

            ctx.fillText(labelText, labelX, labelY);
            ctx.save();
        }
    };

    function createChart(canvasId, config) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) return null;

        const prefersReducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        
        const defaultOptions = {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'bottom',
                    labels: {
                        font: { family: FONT_FAMILY },
                        padding: 16,
                        usePointStyle: true
                    }
                },
                tooltip: {
                    backgroundColor: '#1b2b26',
                    titleFont: { family: FONT_FAMILY, weight: 'bold' },
                    bodyFont: { family: FONT_FAMILY },
                    padding: 12,
                    cornerRadius: 8
                }
            },
            animation: prefersReducedMotion ? false : {
                duration: 500
            }
        };

        return new Chart(canvas, {
            ...config,
            options: { ...defaultOptions, ...config.options }
        });
    }

    function init(data) {
        if (!data || data.totalRides === 0) return;

        // Set Chart.js defaults with Bengali font support
        Chart.defaults.font.family = FONT_FAMILY;
        Chart.defaults.color = "#5f7169";

        // Spending chart
        if (data.monthlySpending && data.monthlySpending.length > 0) {
            createChart('spendingChart', {
                type: 'bar',
                data: {
                    labels: data.monthlySpending.map(x => x.label),
                    datasets: [{
                        label: 'Spending',
                        data: data.monthlySpending.map(x => x.amount),
                        backgroundColor: COLORS[0],
                        borderRadius: 8,
                        maxBarThickness: 44
                    }]
                },
                options: {
                    scales: {
                        y: {
                            beginAtZero: true,
                            ticks: {
                                maxTicksLimit: 5,
                                callback: function(value) {
                                    return formatCurrency(value);
                                }
                            },
                            grid: { color: GRID_COLOR },
                            border: { display: false }
                        },
                        x: {
                            grid: { display: false }
                        }
                    },
                    plugins: {
                        legend: { display: false },
                        tooltip: {
                            callbacks: {
                                label: function(context) {
                                    const item = data.monthlySpending[context.dataIndex];
                                    return `Spending: ${formatCurrency(item.amount)} • ${item.rides} rides`;
                                }
                            }
                        }
                    }
                }
            });
        }

        // Status chart
        if (data.statusCounts && data.statusCounts.length > 0) {
            createChart('statusChart', {
                type: 'doughnut',
                data: {
                    labels: data.statusCounts.map(x => pretty(x.label, STATUS_LABELS)),
                    datasets: [{
                        data: data.statusCounts.map(x => x.count),
                        backgroundColor: data.statusCounts.map(x => STATUS_COLORS[x.label] || COLORS[0]),
                        borderWidth: 0
                    }]
                },
                options: {
                    cutout: "72%",
                    plugins: {
                        centreText: true,
                        tooltip: {
                            callbacks: {
                                label: function(context) {
                                    const item = data.statusCounts[context.dataIndex];
                                    return `${pretty(item.label, STATUS_LABELS)}: ${item.count} rides`;
                                }
                            }
                        }
                    }
                },
                plugins: [centreTextPlugin]
            });
        }

        // Top routes chart
        if (data.topRoutes && data.topRoutes.length > 0) {
            createChart('topRoutesChart', {
                type: 'bar',
                data: {
                    labels: data.topRoutes.map(x => x.label),
                    datasets: [{
                        label: 'Rides',
                        data: data.topRoutes.map(x => x.count),
                        backgroundColor: COLORS[0],
                        borderRadius: 6
                    }]
                },
                options: {
                    indexAxis: 'y',
                    scales: {
                        x: {
                            beginAtZero: true,
                            grid: { color: GRID_COLOR },
                            ticks: { font: { family: FONT_FAMILY } }
                        },
                        y: {
                            grid: { display: false },
                            ticks: { font: { family: FONT_FAMILY } }
                        }
                    },
                    plugins: {
                        tooltip: {
                            callbacks: {
                                label: function(context) {
                                    const item = data.topRoutes[context.dataIndex];
                                    return `Rides: ${item.count} • ${formatCurrency(item.totalSpent)}`;
                                }
                            }
                        }
                    }
                }
            });
        }

        // Payment methods chart
        if (data.paymentMethodCounts && data.paymentMethodCounts.length > 0) {
            createChart('paymentChart', {
                type: 'doughnut',
                data: {
                    labels: data.paymentMethodCounts.map(x => pretty(x.label, PAYMENT_LABELS)),
                    datasets: [{
                        data: data.paymentMethodCounts.map(x => x.count),
                        backgroundColor: COLORS.slice(0, data.paymentMethodCounts.length),
                        borderWidth: 0
                    }]
                },
                options: {
                    cutout: "72%",
                    plugins: {
                        centreText: true,
                        tooltip: {
                            callbacks: {
                                label: function(context) {
                                    const item = data.paymentMethodCounts[context.dataIndex];
                                    return `${pretty(item.label, PAYMENT_LABELS)}: ${item.count} rides`;
                                }
                            }
                        }
                    }
                },
                plugins: [centreTextPlugin]
            });
        }

        // Weekday chart
        if (data.weekdayCounts && data.weekdayCounts.length > 0) {
            createChart('weekdayChart', {
                type: 'bar',
                data: {
                    labels: data.weekdayCounts.map(x => x.label),
                    datasets: [{
                        label: 'Rides',
                        data: data.weekdayCounts.map(x => x.count),
                        backgroundColor: COLORS[0],
                        borderRadius: 6
                    }]
                },
                options: {
                    scales: {
                        y: {
                            beginAtZero: true,
                            grid: { color: GRID_COLOR },
                            ticks: {
                                stepSize: 1,
                                font: { family: FONT_FAMILY }
                            }
                        },
                        x: {
                            grid: { display: false },
                            ticks: { font: { family: FONT_FAMILY } }
                        }
                    }
                }
            });
        }
    }

    return { init };
})();
