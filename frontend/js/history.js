// History page functionality
let investments = [];
let historyData = [];
let currentChart = null;
let selectedInvestmentId = '';

async function loadData() {
    try {
        // Load investments for filter dropdown
        const investmentsResponse = await fetchWithAuth(`${API_BASE_URL}/investments`);
        if (!investmentsResponse) return;
        investments = await investmentsResponse.json();

        // Populate filter dropdowns
        populateInvestmentFilter();
        populateHistoryInvestmentDropdown();

        // Check for URL parameter to pre-select investment
        const urlParams = new URLSearchParams(window.location.search);
        const investmentId = urlParams.get('investmentId');
        if (investmentId) {
            document.getElementById('investmentFilter').value = investmentId;
        }

        // Load initial history
        await loadHistory();
    } catch (error) {
        console.error('Error loading data:', error);
    }
}

function populateInvestmentFilter() {
    const select = document.getElementById('investmentFilter');
    select.innerHTML = '<option value="">Portfólio Total</option>';

    investments.forEach(inv => {
        const option = document.createElement('option');
        option.value = inv.id;
        option.textContent = inv.name;
        select.appendChild(option);
    });
}

function populateHistoryInvestmentDropdown() {
    const select = document.getElementById('historyInvestment');
    select.innerHTML = '<option value="">Selecione...</option>';

    investments.forEach(inv => {
        const option = document.createElement('option');
        option.value = inv.id;
        option.textContent = inv.name;
        select.appendChild(option);
    });
}

async function loadHistory() {
    try {
        selectedInvestmentId = document.getElementById('investmentFilter').value;
        const startDate = document.getElementById('startDate').value;
        const endDate = document.getElementById('endDate').value;

        let url;
        let params = new URLSearchParams();

        if (startDate) params.append('startDate', new Date(startDate).toISOString());
        if (endDate) params.append('endDate', new Date(endDate).toISOString());

        if (selectedInvestmentId) {
            // Load individual investment history
            url = `${API_BASE_URL}/investmenthistory/investment/${selectedInvestmentId}`;
            if (params.toString()) url += `?${params.toString()}`;

            const response = await fetchWithAuth(url);
            if (!response) return;
            historyData = await response.json();
            renderInvestmentHistory();
        } else {
            // Load portfolio history
            url = `${API_BASE_URL}/investmenthistory/portfolio`;
            if (params.toString()) url += `?${params.toString()}`;

            const response = await fetchWithAuth(url);
            if (!response) return;
            historyData = await response.json();
            renderPortfolioHistory();
        }
    } catch (error) {
        console.error('Error loading history:', error);
    }
}

function renderInvestmentHistory() {
    const container = document.getElementById('historyTable');

    if (historyData.length === 0) {
        container.innerHTML = '<p>Nenhum histórico encontrado.</p>';
        renderChart([]);
        return;
    }

    let tableHtml = `
        <h3>Histórico - ${historyData[0].investmentName}</h3>
        <table>
            <thead>
                <tr>
                    <th>Data</th>
                    <th>Valor</th>
                    <th>Valor Unitário</th>
                    <th>Quantidade</th>
                    <th>Notas</th>
                    <th>Ações</th>
                </tr>
            </thead>
            <tbody>
    `;

    historyData.forEach(entry => {
        const date = new Date(entry.recordedDate).toLocaleString('pt-BR');

        tableHtml += `
            <tr>
                <td>${date}</td>
                <td>${formatCurrency(entry.value)}</td>
                <td>${entry.unitValue ? formatCurrency(entry.unitValue) : '-'}</td>
                <td>${entry.quantity ? entry.quantity.toFixed(4) : '-'}</td>
                <td>${entry.notes || '-'}</td>
                <td>
                    <button class="btn btn-danger" onclick="deleteHistoryEntry(${entry.id})">Excluir</button>
                </td>
            </tr>
        `;
    });

    tableHtml += `
            </tbody>
        </table>
    `;

    container.innerHTML = tableHtml;

    // Render chart
    const chartData = historyData.map(entry => ({
        x: new Date(entry.recordedDate),
        y: entry.value
    }));
    renderChart(chartData, historyData[0].investmentName);
}

function renderPortfolioHistory() {
    const container = document.getElementById('historyTable');

    if (historyData.length === 0) {
        container.innerHTML = '<p>Nenhum histórico encontrado.</p>';
        renderChart([]);
        return;
    }

    let tableHtml = `
        <h3>Histórico do Portfólio Total</h3>
        <table>
            <thead>
                <tr>
                    <th>Data</th>
                    <th>Valor Total</th>
                </tr>
            </thead>
            <tbody>
    `;

    historyData.forEach(entry => {
        const date = new Date(entry.date).toLocaleDateString('pt-BR');

        tableHtml += `
            <tr>
                <td>${date}</td>
                <td>${formatCurrency(entry.totalValue)}</td>
            </tr>
        `;
    });

    tableHtml += `
            </tbody>
        </table>
    `;

    container.innerHTML = tableHtml;

    // Render chart
    const chartData = historyData.map(entry => ({
        x: new Date(entry.date),
        y: entry.totalValue
    }));
    renderChart(chartData, 'Portfólio Total');
}

function renderChart(data, label = 'Valor') {
    const ctx = document.getElementById('historyChart');

    // Destroy existing chart if it exists
    if (currentChart) {
        currentChart.destroy();
    }

    if (data.length === 0) {
        ctx.style.display = 'none';
        return;
    }

    ctx.style.display = 'block';

    currentChart = new Chart(ctx, {
        type: 'line',
        data: {
            datasets: [{
                label: label,
                data: data,
                borderColor: '#007bff',
                backgroundColor: 'rgba(0, 123, 255, 0.1)',
                tension: 0.1,
                fill: true
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            aspectRatio: 2,
            plugins: {
                legend: {
                    display: true,
                    position: 'top'
                },
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            return `${context.dataset.label}: ${formatCurrency(context.parsed.y)}`;
                        }
                    }
                }
            },
            scales: {
                x: {
                    type: 'time',
                    time: {
                        unit: 'day',
                        displayFormats: {
                            day: 'dd/MM/yyyy'
                        }
                    },
                    title: {
                        display: true,
                        text: 'Data'
                    }
                },
                y: {
                    beginAtZero: false,
                    title: {
                        display: true,
                        text: 'Valor (R$)'
                    },
                    ticks: {
                        callback: function(value) {
                            return formatCurrency(value);
                        }
                    }
                }
            }
        }
    });
}

async function deleteHistoryEntry(id) {
    if (!confirm('Tem certeza que deseja excluir esta entrada do histórico?')) {
        return;
    }

    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/investmenthistory/${id}`, {
            method: 'DELETE'
        });

        if (response && (response.ok || response.status === 204)) {
            await loadHistory();
            alert('Entrada excluída com sucesso!');
        }
    } catch (error) {
        console.error('Error deleting history entry:', error);
        alert('Erro ao excluir entrada do histórico.');
    }
}

function openHistoryModal() {
    const modal = document.getElementById('historyModal');
    const form = document.getElementById('historyForm');

    form.reset();

    // Set default date to now
    const now = new Date();
    const localDateTime = new Date(now.getTime() - now.getTimezoneOffset() * 60000).toISOString().slice(0, 16);
    document.getElementById('historyDate').value = localDateTime;

    modal.style.display = 'block';
}

function closeHistoryModal() {
    document.getElementById('historyModal').style.display = 'none';
}

// Event Listeners
document.addEventListener('DOMContentLoaded', function() {
    // Set default date range (01/01/2025 - current date)
    const startDateInput = document.getElementById('startDate');
    const endDateInput = document.getElementById('endDate');

    startDateInput.value = '2025-01-01';

    const today = new Date();
    const year = today.getFullYear();
    const month = String(today.getMonth() + 1).padStart(2, '0');
    const day = String(today.getDate()).padStart(2, '0');
    endDateInput.value = `${year}-${month}-${day}`;

    loadData();

    // Filter button
    document.getElementById('filterBtn').addEventListener('click', loadHistory);

    // Add history button
    document.getElementById('addHistoryBtn').addEventListener('click', openHistoryModal);

    // Modal close buttons
    document.querySelector('#historyModal .close').addEventListener('click', closeHistoryModal);
    document.getElementById('cancelHistoryBtn').addEventListener('click', closeHistoryModal);

    // Close modal when clicking outside
    window.addEventListener('click', function(event) {
        const modal = document.getElementById('historyModal');
        if (event.target === modal) {
            closeHistoryModal();
        }
    });

    // History form submission
    document.getElementById('historyForm').addEventListener('submit', async function(e) {
        e.preventDefault();

        const data = {
            investmentId: parseInt(document.getElementById('historyInvestment').value),
            value: parseFloat(document.getElementById('historyValue').value),
            unitValue: document.getElementById('historyUnitValue').value ? parseFloat(document.getElementById('historyUnitValue').value) : null,
            quantity: document.getElementById('historyQuantity').value ? parseFloat(document.getElementById('historyQuantity').value) : null,
            recordedDate: new Date(document.getElementById('historyDate').value).toISOString(),
            notes: document.getElementById('historyNotes').value || null
        };

        try {
            const response = await fetchWithAuth(`${API_BASE_URL}/investmenthistory`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(data)
            });

            if (response && response.ok) {
                closeHistoryModal();
                await loadHistory();
                alert('Entrada adicionada com sucesso!');
            }
        } catch (error) {
            console.error('Error saving history entry:', error);
            alert('Erro ao salvar entrada do histórico.');
        }
    });
});
