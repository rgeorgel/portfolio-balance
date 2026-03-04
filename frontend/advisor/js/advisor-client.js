// Advisor Client View Script
let clientData = null;
let investmentTypes = [];
let investments = [];
let transactions = [];
let profitabilityData = null;
let pieChart = null;
let comparisonChart = null;
let profitabilityChart = null;

const CHART_COLORS = [
    '#4CAF50', '#2196F3', '#FF9800', '#9C27B0', '#F44336',
    '#00BCD4', '#795548', '#607D8B', '#E91E63', '#3F51B5'
];

document.addEventListener('DOMContentLoaded', function() {
    // Check authentication
    checkAdvisorAuth();

    // Get client ID from URL
    const urlParams = new URLSearchParams(window.location.search);
    const clientId = urlParams.get('id');

    if (!clientId) {
        window.location.href = 'index.html';
        return;
    }

    // Load client data
    loadClientData(clientId);

    // Setup tabs
    setupTabs();
});

function setupTabs() {
    const tabs = document.querySelectorAll('.tab');
    tabs.forEach(tab => {
        tab.addEventListener('click', function() {
            // Remove active from all tabs
            tabs.forEach(t => t.classList.remove('active'));
            // Add active to clicked tab
            this.classList.add('active');

            // Hide all content
            document.querySelectorAll('.tab-content').forEach(content => {
                content.classList.remove('active');
            });

            // Show selected content
            const tabId = this.getAttribute('data-tab');
            document.getElementById(tabId).classList.add('active');
        });
    });
}

async function loadClientData(clientId) {
    try {
        // Load portfolio data
        const portfolioResponse = await fetchWithAdvisorAuth(`${API_BASE_URL}/advisor/clients/${clientId}/portfolio`);
        if (!portfolioResponse || !portfolioResponse.ok) {
            throw new Error('Falha ao carregar dados do cliente');
        }
        clientData = await portfolioResponse.json();

        // Update client info
        document.getElementById('clientName').textContent = clientData.username;
        document.getElementById('clientEmail').textContent = clientData.email;
        document.getElementById('totalValue').textContent = formatCurrency(clientData.totalValue);
        document.getElementById('totalInvestments').textContent = clientData.totalInvestments;

        // Count active types (with value > 0)
        const activeTypes = clientData.investmentTypes.filter(t => t.currentValue > 0).length;
        document.getElementById('totalTypes').textContent = activeTypes;

        investmentTypes = clientData.investmentTypes;
        investments = clientData.investments;

        // Render charts
        renderPieChart();
        renderComparisonChart();

        // Render investments table
        renderInvestmentsTable();

        // Render allocations
        renderAllocations();

        // Load transactions
        loadTransactions(clientId);

        // Load profitability data
        loadProfitability(clientId);

    } catch (error) {
        console.error('Error loading client data:', error);
        document.getElementById('clientName').textContent = 'Erro ao carregar';
    }
}

async function loadTransactions(clientId) {
    try {
        const response = await fetchWithAdvisorAuth(`${API_BASE_URL}/advisor/clients/${clientId}/transactions`);
        if (!response || !response.ok) {
            throw new Error('Falha ao carregar transacoes');
        }
        transactions = await response.json();
        renderTransactionsTable();
    } catch (error) {
        console.error('Error loading transactions:', error);
    }
}

function renderPieChart() {
    const ctx = document.getElementById('portfolioPieChart').getContext('2d');

    const typesWithValue = investmentTypes.filter(t => t.currentValue > 0);

    if (pieChart) {
        pieChart.destroy();
    }

    if (typesWithValue.length === 0) {
        ctx.font = '16px Arial';
        ctx.fillStyle = '#999';
        ctx.textAlign = 'center';
        ctx.fillText('Nenhum investimento cadastrado', ctx.canvas.width / 2, ctx.canvas.height / 2);
        return;
    }

    pieChart = new Chart(ctx, {
        type: 'pie',
        data: {
            labels: typesWithValue.map(t => t.name),
            datasets: [{
                data: typesWithValue.map(t => t.currentValue),
                backgroundColor: CHART_COLORS.slice(0, typesWithValue.length),
                borderWidth: 2,
                borderColor: '#fff'
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'bottom',
                    labels: {
                        padding: 15,
                        usePointStyle: true
                    }
                },
                datalabels: {
                    color: '#fff',
                    font: {
                        weight: 'bold',
                        size: 12
                    },
                    formatter: (value, context) => {
                        const total = context.dataset.data.reduce((a, b) => a + b, 0);
                        const percentage = ((value / total) * 100).toFixed(1);
                        return percentage + '%';
                    }
                }
            }
        },
        plugins: [ChartDataLabels]
    });
}

function renderComparisonChart() {
    const ctx = document.getElementById('allocationComparisonChart').getContext('2d');

    if (comparisonChart) {
        comparisonChart.destroy();
    }

    comparisonChart = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: investmentTypes.map(t => t.name),
            datasets: [
                {
                    label: 'Alvo (%)',
                    data: investmentTypes.map(t => t.allocationPercentage),
                    backgroundColor: 'rgba(26, 26, 46, 0.7)',
                    borderColor: '#1a1a2e',
                    borderWidth: 1
                },
                {
                    label: 'Atual (%)',
                    data: investmentTypes.map(t => t.currentPercentage),
                    backgroundColor: 'rgba(76, 175, 80, 0.7)',
                    borderColor: '#4CAF50',
                    borderWidth: 1
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'top'
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    max: 100,
                    ticks: {
                        callback: value => value + '%'
                    }
                }
            }
        }
    });
}

function renderInvestmentsTable() {
    const tbody = document.getElementById('investmentsTableBody');

    if (investments.length === 0) {
        tbody.innerHTML = '<tr><td colspan="5" style="text-align: center; color: #666;">Nenhum investimento cadastrado</td></tr>';
        return;
    }

    tbody.innerHTML = investments.map(inv => `
        <tr>
            <td><strong>${escapeHtml(inv.name)}</strong></td>
            <td><span class="type-badge">${escapeHtml(inv.investmentTypeName)}</span></td>
            <td>${inv.quantity ? formatQuantity(inv.quantity) : '-'}</td>
            <td>${inv.unitValue ? formatCurrency(inv.unitValue) : '-'}</td>
            <td><strong>${formatCurrency(inv.currentValue)}</strong></td>
        </tr>
    `).join('');
}

function renderAllocations() {
    const container = document.getElementById('allocationsContainer');

    if (investmentTypes.length === 0) {
        container.innerHTML = '<div style="text-align: center; color: #666;">Nenhuma alocacao encontrada</div>';
        return;
    }

    container.innerHTML = investmentTypes.map((type, index) => {
        const color = CHART_COLORS[index % CHART_COLORS.length];
        const currentPct = type.currentPercentage || 0;
        const targetPct = type.allocationPercentage || 0;
        const diff = currentPct - targetPct;
        const diffText = diff > 0 ? `+${diff.toFixed(1)}%` : `${diff.toFixed(1)}%`;
        const diffColor = diff > 0 ? '#4CAF50' : diff < 0 ? '#F44336' : '#666';

        return `
            <div class="allocation-bar">
                <div class="allocation-label">
                    <span class="name">${escapeHtml(type.name)}</span>
                    <span class="values">
                        Atual: ${currentPct.toFixed(1)}% | Alvo: ${targetPct.toFixed(1)}%
                        <span style="color: ${diffColor}; font-weight: 600; margin-left: 10px;">${diffText}</span>
                    </span>
                </div>
                <div class="bar-container">
                    <div class="bar-fill" style="width: ${Math.min(currentPct, 100)}%; background-color: ${color};"></div>
                    <div class="bar-target" style="left: ${targetPct}%;"></div>
                </div>
            </div>
        `;
    }).join('');
}

function renderTransactionsTable() {
    const tbody = document.getElementById('transactionsTableBody');

    if (transactions.length === 0) {
        tbody.innerHTML = '<tr><td colspan="4" style="text-align: center; color: #666;">Nenhuma transacao encontrada</td></tr>';
        return;
    }

    const typeNames = {
        1: 'Aporte',
        2: 'Resgate',
        3: 'Dividendo'
    };

    const typeColors = {
        1: '#4CAF50',
        2: '#F44336',
        3: '#2196F3'
    };

    tbody.innerHTML = transactions.slice(0, 20).map(tx => `
        <tr>
            <td>${formatDate(tx.transactionDate)}</td>
            <td>${escapeHtml(tx.investmentName)}</td>
            <td><span style="color: ${typeColors[tx.type]}; font-weight: 600;">${typeNames[tx.type] || 'Outro'}</span></td>
            <td><strong>${formatCurrency(tx.amount)}</strong></td>
        </tr>
    `).join('');
}

function formatQuantity(value) {
    if (value === null || value === undefined) return '-';
    return new Intl.NumberFormat('pt-BR', {
        minimumFractionDigits: 0,
        maximumFractionDigits: 8
    }).format(value);
}

function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

async function loadProfitability(clientId) {
    try {
        const response = await fetchWithAdvisorAuth(`${API_BASE_URL}/advisor/clients/${clientId}/profitability`);
        if (!response || !response.ok) {
            throw new Error('Falha ao carregar rentabilidade');
        }
        profitabilityData = await response.json();

        // Update summary card with total return
        updateTotalReturnCard();

        // Render profitability components
        renderProfitabilityStats();
        renderProfitabilityByTypeChart();
        renderProfitabilityTable();
        renderTopPerformers();

    } catch (error) {
        console.error('Error loading profitability:', error);
        document.getElementById('profitabilityStats').innerHTML = '<div style="text-align: center; color: #666;">Nao foi possivel carregar dados de rentabilidade</div>';
    }
}

function updateTotalReturnCard() {
    if (!profitabilityData) return;

    const totalReturn = profitabilityData.totalAbsoluteReturn;
    const totalReturnPct = profitabilityData.totalReturnPercentage;

    const returnEl = document.getElementById('totalReturn');
    const returnPctEl = document.getElementById('totalReturnPercentage');

    const colorClass = totalReturn >= 0 ? 'positive' : 'negative';
    const sign = totalReturn >= 0 ? '+' : '';

    returnEl.textContent = `${sign}${formatCurrency(totalReturn)}`;
    returnEl.className = `value ${colorClass}`;

    returnPctEl.textContent = `${sign}${totalReturnPct.toFixed(2)}%`;
    returnPctEl.className = `return-percentage ${colorClass}`;
}

function renderProfitabilityStats() {
    const container = document.getElementById('profitabilityStats');

    if (!profitabilityData) {
        container.innerHTML = '<div style="text-align: center; color: #666;">Sem dados de rentabilidade</div>';
        return;
    }

    const { totalInitialValue, totalCurrentValue, totalAbsoluteReturn, totalReturnPercentage, totalAnnualizedReturn, totalInvestments } = profitabilityData;

    const returnClass = totalAbsoluteReturn >= 0 ? 'positive' : 'negative';
    const returnSign = totalAbsoluteReturn >= 0 ? '+' : '';
    const annualizedClass = totalAnnualizedReturn >= 0 ? 'positive' : 'negative';
    const annualizedSign = totalAnnualizedReturn >= 0 ? '+' : '';

    container.innerHTML = `
        <div class="stat-item">
            <div class="label">Valor Inicial</div>
            <div class="value">${formatCurrency(totalInitialValue)}</div>
        </div>
        <div class="stat-item">
            <div class="label">Valor Atual</div>
            <div class="value">${formatCurrency(totalCurrentValue)}</div>
        </div>
        <div class="stat-item">
            <div class="label">Retorno Absoluto</div>
            <div class="value ${returnClass}">${returnSign}${formatCurrency(totalAbsoluteReturn)}</div>
        </div>
        <div class="stat-item">
            <div class="label">Retorno %</div>
            <div class="value ${returnClass}">${returnSign}${totalReturnPercentage.toFixed(2)}%</div>
        </div>
        <div class="stat-item">
            <div class="label">Retorno Anualizado</div>
            <div class="value ${annualizedClass}">${annualizedSign}${totalAnnualizedReturn.toFixed(2)}%</div>
        </div>
        <div class="stat-item">
            <div class="label">Investimentos</div>
            <div class="value">${totalInvestments}</div>
        </div>
    `;
}

function renderProfitabilityByTypeChart() {
    const ctx = document.getElementById('profitabilityByTypeChart').getContext('2d');

    if (profitabilityChart) {
        profitabilityChart.destroy();
    }

    if (!profitabilityData || !profitabilityData.profitabilityByType || profitabilityData.profitabilityByType.length === 0) {
        ctx.font = '16px Arial';
        ctx.fillStyle = '#999';
        ctx.textAlign = 'center';
        ctx.fillText('Sem dados de rentabilidade', ctx.canvas.width / 2, ctx.canvas.height / 2);
        return;
    }

    const types = profitabilityData.profitabilityByType;

    // Create colors based on positive/negative returns
    const backgroundColors = types.map(t =>
        t.returnPercentage >= 0 ? 'rgba(76, 175, 80, 0.7)' : 'rgba(244, 67, 54, 0.7)'
    );
    const borderColors = types.map(t =>
        t.returnPercentage >= 0 ? '#4CAF50' : '#F44336'
    );

    profitabilityChart = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: types.map(t => t.investmentTypeName),
            datasets: [{
                label: 'Retorno (%)',
                data: types.map(t => t.returnPercentage),
                backgroundColor: backgroundColors,
                borderColor: borderColors,
                borderWidth: 1
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    display: false
                },
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            const type = types[context.dataIndex];
                            return [
                                `Retorno: ${type.returnPercentage.toFixed(2)}%`,
                                `Absoluto: ${formatCurrency(type.absoluteReturn)}`,
                                `Valor Atual: ${formatCurrency(type.currentValue)}`
                            ];
                        }
                    }
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    ticks: {
                        callback: value => value + '%'
                    }
                }
            }
        }
    });
}

function renderProfitabilityTable() {
    const tbody = document.getElementById('profitabilityTableBody');

    if (!profitabilityData || !profitabilityData.investmentsByPerformance || profitabilityData.investmentsByPerformance.length === 0) {
        tbody.innerHTML = '<tr><td colspan="6" style="text-align: center; color: #666;">Sem dados de rentabilidade</td></tr>';
        return;
    }

    const investments = profitabilityData.investmentsByPerformance;

    tbody.innerHTML = investments.map(inv => {
        const returnClass = inv.absoluteReturn >= 0 ? 'positive' : 'negative';
        const returnSign = inv.absoluteReturn >= 0 ? '+' : '';

        return `
            <tr>
                <td><strong>${escapeHtml(inv.investmentName)}</strong></td>
                <td><span class="type-badge">${escapeHtml(inv.investmentTypeName)}</span></td>
                <td>${formatCurrency(inv.initialValue)}</td>
                <td>${formatCurrency(inv.currentValue)}</td>
                <td class="${returnClass}"><strong>${returnSign}${formatCurrency(inv.absoluteReturn)}</strong></td>
                <td class="${returnClass}"><strong>${returnSign}${inv.returnPercentage.toFixed(2)}%</strong></td>
            </tr>
        `;
    }).join('');
}

function renderTopPerformers() {
    const container = document.getElementById('topPerformersContainer');

    if (!profitabilityData || !profitabilityData.investmentsByPerformance || profitabilityData.investmentsByPerformance.length === 0) {
        container.innerHTML = '<div style="text-align: center; color: #666; padding: 20px;">Sem dados de rentabilidade</div>';
        return;
    }

    const investments = profitabilityData.investmentsByPerformance;
    const topCount = Math.min(5, investments.length);

    // Sort for top performers (highest return)
    const topPerformers = [...investments].sort((a, b) => b.returnPercentage - a.returnPercentage).slice(0, topCount);

    // Sort for bottom performers (lowest return)
    const bottomPerformers = [...investments].sort((a, b) => a.returnPercentage - b.returnPercentage).slice(0, topCount);

    let html = `
        <div class="performers-section">
            <h5 class="top-title">Melhores Desempenhos</h5>
            ${topPerformers.map(inv => renderPerformerItem(inv)).join('')}
        </div>
    `;

    // Only show bottom performers if there are enough investments and some have negative returns
    if (investments.length > 1 && bottomPerformers.some(p => p.returnPercentage < 0)) {
        html += `
            <div class="performers-section" style="margin-top: 25px;">
                <h5 class="bottom-title">Piores Desempenhos</h5>
                ${bottomPerformers.map(inv => renderPerformerItem(inv)).join('')}
            </div>
        `;
    }

    container.innerHTML = html;
}

function renderPerformerItem(inv) {
    const returnClass = inv.returnPercentage >= 0 ? 'positive' : 'negative';
    const returnSign = inv.returnPercentage >= 0 ? '+' : '';

    return `
        <div class="performer-item">
            <div class="performer-info">
                <span class="performer-name">${escapeHtml(inv.investmentName)}</span>
                <span class="performer-type">${escapeHtml(inv.investmentTypeName)}</span>
            </div>
            <div class="performer-return">
                <div class="percentage ${returnClass}">${returnSign}${inv.returnPercentage.toFixed(2)}%</div>
                <div class="amount">${formatCurrency(inv.absoluteReturn)}</div>
            </div>
        </div>
    `;
}
