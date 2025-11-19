// Dashboard functionality
let investmentTypes = [];
let investments = [];

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

// Load dashboard on page load
document.addEventListener('DOMContentLoaded', loadDashboard);
