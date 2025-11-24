// Global variables for charts
let roiByTypeChart = null;
let returnsDistributionChart = null;
let allInvestments = []; // Store all investments for filtering

// Format currency
function formatCurrency(value) {
    return new Intl.NumberFormat('pt-BR', {
        style: 'currency',
        currency: 'BRL'
    }).format(value);
}

// Format percentage
function formatPercentage(value) {
    return new Intl.NumberFormat('pt-BR', {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    }).format(value) + '%';
}

// Get date range based on period filter
function getDateRange() {
    const period = document.getElementById('periodFilter').value;
    const endDate = new Date();
    let startDate = null;

    switch (period) {
        case 'month':
            startDate = new Date();
            startDate.setMonth(startDate.getMonth() - 1);
            break;
        case '3months':
            startDate = new Date();
            startDate.setMonth(startDate.getMonth() - 3);
            break;
        case '6months':
            startDate = new Date();
            startDate.setMonth(startDate.getMonth() - 6);
            break;
        case 'year':
            startDate = new Date();
            startDate.setFullYear(startDate.getFullYear() - 1);
            break;
        case 'all':
        default:
            startDate = null;
            break;
    }

    return { startDate, endDate };
}

// Load profitability data
async function loadProfitabilityData() {
    try {
        const { startDate, endDate } = getDateRange();

        // Build query parameters
        let queryParams = '';
        if (startDate) {
            queryParams = `?startDate=${startDate.toISOString()}&endDate=${endDate.toISOString()}`;
        }

        // Fetch portfolio profitability
        const portfolioResponse = await fetchWithAuth(`${API_BASE_URL}/profitability/portfolio${queryParams}`);
        const portfolioData = await portfolioResponse.json();

        // Fetch top performers
        const performersResponse = await fetchWithAuth(`${API_BASE_URL}/profitability/top-performers?limit=5`);
        const performersData = await performersResponse.json();

        // Update summary cards
        updateSummaryCards(portfolioData);

        // Update charts
        updateRoiByTypeChart(portfolioData.profitabilityByType);
        updateReturnsDistributionChart(portfolioData.investmentsByPerformance);

        // Update top/bottom performers
        updatePerformersLists(performersData);

        // Update investments table
        updateInvestmentsTable(portfolioData.investmentsByPerformance);

    } catch (error) {
        console.error('Error loading profitability data:', error);
        alert('Erro ao carregar dados de rentabilidade. Por favor, tente novamente.');
    }
}

// Update summary cards
function updateSummaryCards(data) {
    const totalReturnPercentage = document.getElementById('totalReturnPercentage');
    const totalReturnAbsolute = document.getElementById('totalReturnAbsolute');
    const annualizedReturn = document.getElementById('annualizedReturn');
    const initialValue = document.getElementById('initialValue');
    const currentValue = document.getElementById('currentValue');

    // Update values
    totalReturnPercentage.textContent = formatPercentage(data.totalReturnPercentage);
    totalReturnAbsolute.textContent = formatCurrency(data.totalAbsoluteReturn);
    annualizedReturn.textContent = formatPercentage(data.totalAnnualizedReturn);
    initialValue.textContent = formatCurrency(data.totalInitialValue);
    currentValue.textContent = formatCurrency(data.totalCurrentValue);

    // Color code the return percentage
    if (data.totalReturnPercentage > 0) {
        totalReturnPercentage.style.color = '#28a745';
        totalReturnAbsolute.style.color = '#28a745';
    } else if (data.totalReturnPercentage < 0) {
        totalReturnPercentage.style.color = '#dc3545';
        totalReturnAbsolute.style.color = '#dc3545';
    } else {
        totalReturnPercentage.style.color = '#6c757d';
        totalReturnAbsolute.style.color = '#6c757d';
    }

    // Color code annualized return
    if (data.totalAnnualizedReturn > 0) {
        annualizedReturn.style.color = '#28a745';
    } else if (data.totalAnnualizedReturn < 0) {
        annualizedReturn.style.color = '#dc3545';
    } else {
        annualizedReturn.style.color = '#6c757d';
    }
}

// Update ROI by Type chart
function updateRoiByTypeChart(profitabilityByType) {
    const ctx = document.getElementById('roiByTypeChart').getContext('2d');

    // Destroy existing chart
    if (roiByTypeChart) {
        roiByTypeChart.destroy();
    }

    // Prepare data
    const labels = profitabilityByType.map(t => t.investmentTypeName);
    const data = profitabilityByType.map(t => t.returnPercentage);
    const colors = data.map(value => value >= 0 ? 'rgba(40, 167, 69, 0.8)' : 'rgba(220, 53, 69, 0.8)');

    roiByTypeChart = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [{
                label: 'ROI %',
                data: data,
                backgroundColor: colors,
                borderColor: colors.map(c => c.replace('0.8', '1')),
                borderWidth: 1
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            plugins: {
                legend: {
                    display: false
                },
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            return `ROI: ${formatPercentage(context.parsed.y)}`;
                        }
                    }
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    ticks: {
                        callback: function(value) {
                            return value + '%';
                        }
                    }
                }
            }
        }
    });
}

// Update returns distribution chart (horizontal bar chart)
function updateReturnsDistributionChart(investmentsByPerformance) {
    const ctx = document.getElementById('returnsDistributionChart').getContext('2d');

    // Destroy existing chart
    if (returnsDistributionChart) {
        returnsDistributionChart.destroy();
    }

    // Take top 10 investments by performance (sorted)
    const topInvestments = investmentsByPerformance.slice(0, 10);

    const labels = topInvestments.map(i => i.investmentName);
    const data = topInvestments.map(i => i.returnPercentage);
    const colors = data.map(value => value >= 0 ? 'rgba(40, 167, 69, 0.8)' : 'rgba(220, 53, 69, 0.8)');

    returnsDistributionChart = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [{
                label: 'ROI %',
                data: data,
                backgroundColor: colors,
                borderColor: colors.map(c => c.replace('0.8', '1')),
                borderWidth: 1
            }]
        },
        options: {
            indexAxis: 'y',
            responsive: true,
            maintainAspectRatio: true,
            plugins: {
                legend: {
                    display: false
                },
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            return `ROI: ${formatPercentage(context.parsed.x)}`;
                        }
                    }
                }
            },
            scales: {
                x: {
                    beginAtZero: true,
                    ticks: {
                        callback: function(value) {
                            return value + '%';
                        }
                    }
                }
            }
        }
    });
}

// Update top and bottom performers lists
function updatePerformersLists(data) {
    const topPerformersList = document.getElementById('topPerformersList');
    const bottomPerformersList = document.getElementById('bottomPerformersList');

    // Clear existing content
    topPerformersList.innerHTML = '';
    bottomPerformersList.innerHTML = '';

    // Populate top performers
    data.topPerformers.forEach((investment, index) => {
        const item = createPerformerItem(investment, index + 1, true);
        topPerformersList.appendChild(item);
    });

    // Populate bottom performers
    data.bottomPerformers.forEach((investment, index) => {
        const item = createPerformerItem(investment, index + 1, false);
        bottomPerformersList.appendChild(item);
    });
}

// Create performer item element
function createPerformerItem(investment, rank, isTop) {
    const div = document.createElement('div');
    div.className = 'performer-item';

    const returnClass = investment.returnPercentage >= 0 ? 'positive' : 'negative';
    const returnColor = investment.returnPercentage >= 0 ? '#28a745' : '#dc3545';

    div.innerHTML = `
        <div class="performer-rank">${rank}</div>
        <div class="performer-details">
            <div class="performer-name">${investment.investmentName}</div>
            <div class="performer-type">${investment.investmentTypeName}</div>
        </div>
        <div class="performer-return" style="color: ${returnColor}">
            <div class="performer-percentage">${formatPercentage(investment.returnPercentage)}</div>
            <div class="performer-absolute">${formatCurrency(investment.absoluteReturn)}</div>
        </div>
    `;

    return div;
}

// Update investments table
function updateInvestmentsTable(investments) {
    // Store all investments globally for filtering
    allInvestments = investments;

    // Populate investment type filter
    populateInvestmentTypeFilter(investments);

    // Display the table
    displayInvestmentsTable(investments);
}

// Populate investment type filter dropdown
function populateInvestmentTypeFilter(investments) {
    const typeFilter = document.getElementById('investmentTypeFilter');
    const currentValue = typeFilter.value;

    // Get unique investment types
    const types = [...new Set(investments.map(inv => inv.investmentTypeName))].sort();

    // Clear and repopulate options
    typeFilter.innerHTML = '<option value="">Todos os tipos</option>';
    types.forEach(type => {
        const option = document.createElement('option');
        option.value = type;
        option.textContent = type;
        typeFilter.appendChild(option);
    });

    // Restore previous selection if it still exists
    if (currentValue && types.includes(currentValue)) {
        typeFilter.value = currentValue;
    }
}

// Filter investments table based on selected type
function filterInvestmentsTable() {
    const selectedType = document.getElementById('investmentTypeFilter').value;

    let filteredInvestments = allInvestments;
    if (selectedType) {
        filteredInvestments = allInvestments.filter(inv => inv.investmentTypeName === selectedType);
    }

    displayInvestmentsTable(filteredInvestments);
}

// Display investments in the table
function displayInvestmentsTable(investments) {
    const tbody = document.getElementById('investmentsTableBody');
    tbody.innerHTML = '';

    if (investments.length === 0) {
        const row = tbody.insertRow();
        const cell = row.insertCell(0);
        cell.colSpan = 8;
        cell.textContent = 'Nenhum investimento encontrado';
        cell.style.textAlign = 'center';
        return;
    }

    investments.forEach(investment => {
        const row = tbody.insertRow();

        const returnColor = investment.returnPercentage >= 0 ? '#28a745' : '#dc3545';

        row.innerHTML = `
            <td>${investment.investmentName}</td>
            <td>${investment.investmentTypeName}</td>
            <td>${formatCurrency(investment.initialValue)}</td>
            <td>${formatCurrency(investment.currentValue)}</td>
            <td style="color: ${returnColor}; font-weight: bold;">${formatCurrency(investment.absoluteReturn)}</td>
            <td style="color: ${returnColor}; font-weight: bold;">${formatPercentage(investment.returnPercentage)}</td>
            <td>${formatPercentage(investment.annualizedReturn)}</td>
            <td>${investment.investmentPeriodDays}</td>
        `;
    });
}

// Initialize page
document.addEventListener('DOMContentLoaded', function() {
    loadProfitabilityData();
});
