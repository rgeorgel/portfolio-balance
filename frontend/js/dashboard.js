// Dashboard functionality
let investmentTypes = [];
let investments = [];
let portfolioPieChart = null;
let allocationComparisonChart = null;

async function loadDashboard() {
    try {
        // Load investment types
        const typesResponse = await fetchWithAuth(`${API_BASE_URL}/investmenttypes`);
        if (!typesResponse) return;
        investmentTypes = await typesResponse.json();

        // Load investments
        const investmentsResponse = await fetchWithAuth(`${API_BASE_URL}/investments`);
        if (!investmentsResponse) return;
        investments = await investmentsResponse.json();

        updateSummary();
        renderAllocationChart();
        renderPortfolioPieChart();
        renderAllocationComparisonChart();
        renderRecentInvestments();
    } catch (error) {
        console.error('Error loading dashboard:', error);
    }
}

function updateSummary() {
    const totalValue = investmentTypes.reduce((sum, type) => sum + type.currentTotalValue, 0);
    const totalInvestments = investments.length;

    document.getElementById('totalValue').textContent = formatCurrency(totalValue);
    document.getElementById('totalTypes').textContent = investmentTypes.length;
    document.getElementById('totalInvestments').textContent = totalInvestments;
}

function renderAllocationChart() {
    const chartContainer = document.getElementById('allocationChart');
    chartContainer.innerHTML = '';

    const totalValue = investmentTypes.reduce((sum, type) => sum + type.currentTotalValue, 0);

    investmentTypes.forEach(type => {
        const currentPercentage = totalValue > 0
            ? (type.currentTotalValue / totalValue) * 100
            : 0;

        const barHtml = `
            <div class="allocation-bar">
                <div class="allocation-bar-label">
                    <span><strong>${type.name}</strong></span>
                    <span>Alvo: ${formatPercentage(type.allocationPercentage)} | Atual: ${formatPercentage(currentPercentage)}</span>
                </div>
                <div class="allocation-bar-visual">
                    <div class="allocation-bar-fill" style="width: ${currentPercentage}%"></div>
                    <div class="allocation-bar-text">${formatCurrency(type.currentTotalValue)}</div>
                </div>
            </div>
        `;
        chartContainer.innerHTML += barHtml;
    });
}

function renderRecentInvestments() {
    const container = document.getElementById('recentInvestments');
    container.innerHTML = '';

    // Sort by creation date and take the 5 most recent
    const recentInvestments = [...investments]
        .sort((a, b) => new Date(b.createdDate) - new Date(a.createdDate))
        .slice(0, 5);

    if (recentInvestments.length === 0) {
        container.innerHTML = '<p>Nenhum investimento cadastrado ainda.</p>';
        return;
    }

    recentInvestments.forEach(investment => {
        const type = investmentTypes.find(t => t.id === investment.investmentTypeId);
        const cardHtml = `
            <div class="investment-card">
                <h4>${investment.name}</h4>
                <div class="type">${type ? type.name : 'Tipo desconhecido'}</div>
                <div class="value">${formatCurrency(investment.currentValue)}</div>
            </div>
        `;
        container.innerHTML += cardHtml;
    });
}

function renderPortfolioPieChart() {
    const ctx = document.getElementById('portfolioPieChart');
    if (!ctx) return;

    const totalValue = investmentTypes.reduce((sum, type) => sum + type.currentTotalValue, 0);

    // Destroy existing chart if it exists
    if (portfolioPieChart) {
        portfolioPieChart.destroy();
    }

    // Prepare data
    const labels = investmentTypes.map(type => type.name);
    const data = investmentTypes.map(type => type.currentTotalValue);
    const percentages = investmentTypes.map(type =>
        totalValue > 0 ? ((type.currentTotalValue / totalValue) * 100).toFixed(2) : 0
    );

    // Color palette
    const colors = [
        '#3498db', // Blue
        '#2ecc71', // Green
        '#f39c12', // Orange
        '#e74c3c', // Red
        '#9b59b6', // Purple
        '#1abc9c', // Turquoise
        '#34495e', // Dark Gray
        '#e67e22'  // Carrot
    ];

    portfolioPieChart = new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: labels,
            datasets: [{
                data: data,
                backgroundColor: colors.slice(0, investmentTypes.length),
                borderWidth: 2,
                borderColor: '#fff'
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            plugins: {
                legend: {
                    position: 'bottom',
                    labels: {
                        padding: 15,
                        font: {
                            size: 12
                        }
                    }
                },
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            const label = context.label || '';
                            const value = formatCurrency(context.parsed);
                            const percentage = percentages[context.dataIndex];
                            return `${label}: ${value} (${percentage}%)`;
                        }
                    }
                }
            }
        }
    });
}

function renderAllocationComparisonChart() {
    const ctx = document.getElementById('allocationComparisonChart');
    if (!ctx) return;

    const totalValue = investmentTypes.reduce((sum, type) => sum + type.currentTotalValue, 0);

    // Destroy existing chart if it exists
    if (allocationComparisonChart) {
        allocationComparisonChart.destroy();
    }

    // Prepare data
    const labels = investmentTypes.map(type => type.name);
    const targetData = investmentTypes.map(type => type.allocationPercentage);
    const currentData = investmentTypes.map(type =>
        totalValue > 0 ? (type.currentTotalValue / totalValue) * 100 : 0
    );

    allocationComparisonChart = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [
                {
                    label: 'Alocação Alvo (%)',
                    data: targetData,
                    backgroundColor: 'rgba(52, 152, 219, 0.7)',
                    borderColor: 'rgba(52, 152, 219, 1)',
                    borderWidth: 2
                },
                {
                    label: 'Alocação Atual (%)',
                    data: currentData,
                    backgroundColor: 'rgba(46, 204, 113, 0.7)',
                    borderColor: 'rgba(46, 204, 113, 1)',
                    borderWidth: 2
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            scales: {
                y: {
                    beginAtZero: true,
                    max: 100,
                    ticks: {
                        callback: function(value) {
                            return value + '%';
                        }
                    }
                }
            },
            plugins: {
                legend: {
                    position: 'bottom',
                    labels: {
                        padding: 15,
                        font: {
                            size: 12
                        }
                    }
                },
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            return `${context.dataset.label}: ${context.parsed.y.toFixed(2)}%`;
                        }
                    }
                }
            }
        }
    });
}

// Load dashboard on page load
document.addEventListener('DOMContentLoaded', loadDashboard);
