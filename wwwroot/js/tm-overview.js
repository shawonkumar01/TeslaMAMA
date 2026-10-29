window.TeslaMAMA = window.TeslaMAMA || {};

TeslaMAMA.Overview = (function () {
    const COLORS = ['#1a6e55', '#a9e5cb', '#2b5a9c', '#f5b83d', '#ef5b4c', '#5f7169'];
    const GRID_COLOR = '#dbe5df';
    const FONT_FAMILY = 'Inter, "Segoe UI", system-ui, Arial, sans-serif';
    const formatter = new Intl.NumberFormat('en-BD');

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
            const textY = height / 2;
            
            ctx.fillText(text, textX, textY);
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
                        borderRadius: 6
                    }]
                },
                options: {
                    scales: {
                        y: {
                            beginAtZero: true,
                            grid: { color: GRID_COLOR },
                            ticks: {
                                callback: function(value) {
                                    return formatCurrency(value);
                                },
                                font: { family: FONT_FAMILY }
                            }
                        },
                        x: {
                            grid: { display: false },
                            ticks: { font: { family: FONT_FAMILY } }
                        }
                    },
                    plugins: {
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
                    labels: data.statusCounts.map(x => x.label),
                    datasets: [{
                        data: data.statusCounts.map(x => x.count),
                        backgroundColor: COLORS.slice(0, data.statusCounts.length),
                        borderWidth: 0
                    }]
                },
                options: {
                    plugins: {
                        centreText: true
                    }
                },
                plugins: [centreTextPlugin]
            });
        }

        // Ride type chart
        if (data.rideTypeCounts && data.rideTypeCounts.length > 0) {
            createChart('rideTypeChart', {
                type: 'pie',
                data: {
                    labels: data.rideTypeCounts.map(x => x.label),
                    datasets: [{
                        data: data.rideTypeCounts.map(x => x.count),
                        backgroundColor: COLORS.slice(0, 2),
                        borderWidth: 0
                    }]
                }
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
                    labels: data.paymentMethodCounts.map(x => x.label),
                    datasets: [{
                        data: data.paymentMethodCounts.map(x => x.count),
                        backgroundColor: COLORS.slice(0, data.paymentMethodCounts.length),
                        borderWidth: 0
                    }]
                },
                options: {
                    plugins: {
                        centreText: true
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
