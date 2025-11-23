// Investments page functionality
let investmentTypes = [];
let investments = [];
let currentFilter = '';

async function loadData() {
    try {
        // Load investment types
        const typesResponse = await fetchWithAuth(`${API_BASE_URL}/investmenttypes`);
        if (!typesResponse) return;
        investmentTypes = await typesResponse.json();

        // Populate filter dropdown
        populateTypeFilter();

        // Populate modal dropdown
        populateModalTypeDropdown();

        // Load investments
        await loadInvestments();
    } catch (error) {
        console.error('Error loading data:', error);
    }
}

function populateTypeFilter() {
    const select = document.getElementById('typeFilter');
    select.innerHTML = '<option value="">Todos</option>';

    investmentTypes.forEach(type => {
        const option = document.createElement('option');
        option.value = type.id;
        option.textContent = type.name;
        select.appendChild(option);
    });
}

function populateModalTypeDropdown() {
    const select = document.getElementById('investmentType');
    select.innerHTML = '<option value="">Selecione...</option>';

    investmentTypes.forEach(type => {
        const option = document.createElement('option');
        option.value = type.id;
        option.textContent = type.name;
        select.appendChild(option);
    });
}

async function loadInvestments() {
    try {
        const url = currentFilter
            ? `${API_BASE_URL}/investments?investmentTypeId=${currentFilter}`
            : `${API_BASE_URL}/investments`;

        const response = await fetchWithAuth(url);
        if (!response) return;
        investments = await response.json();
        renderInvestmentsTable();
    } catch (error) {
        console.error('Error loading investments:', error);
    }
}

function renderInvestmentsTable() {
    const container = document.getElementById('investmentsList');

    if (investments.length === 0) {
        container.innerHTML = '<p>Nenhum investimento encontrado.</p>';
        return;
    }

    let tableHtml = `
        <table>
            <thead>
                <tr>
                    <th>Nome</th>
                    <th>Tipo</th>
                    <th>Valor Atual</th>
                    <th>Peso</th>
                    <th>Data de Criação</th>
                    <th>Última Atualização</th>
                    <th>Ações</th>
                </tr>
            </thead>
            <tbody>
    `;

    investments.forEach(investment => {
        const type = investmentTypes.find(t => t.id === investment.investmentTypeId);
        const createdDate = new Date(investment.createdDate).toLocaleDateString('pt-BR');
        const lastUpdatedDate = new Date(investment.lastUpdatedDate).toLocaleDateString('pt-BR');

        tableHtml += `
            <tr>
                <td>${investment.name}</td>
                <td>${type ? type.name : 'Desconhecido'}</td>
                <td>${formatCurrency(investment.currentValue)}</td>
                <td>${investment.weight}</td>
                <td>${createdDate}</td>
                <td>${lastUpdatedDate}</td>
                <td>
                    <button class="btn btn-secondary" onclick="viewHistory(${investment.id})">Histórico</button>
                    <button class="btn btn-edit" onclick="editInvestment(${investment.id})">Editar</button>
                    <button class="btn btn-danger" onclick="deleteInvestment(${investment.id})">Excluir</button>
                </td>
            </tr>
        `;
    });

    tableHtml += `
            </tbody>
        </table>
    `;

    container.innerHTML = tableHtml;
}

function calculateInvestmentValue() {
    const unitValue = parseFloat(document.getElementById('unitValue').value) || 0;
    const quantity = parseFloat(document.getElementById('quantity').value) || 0;
    const investmentValueField = document.getElementById('investmentValue');
    const calculatedNote = document.getElementById('calculatedNote');

    if (unitValue > 0 && quantity > 0) {
        const total = unitValue * quantity;
        investmentValueField.value = total.toFixed(2);
        investmentValueField.readOnly = true;
        calculatedNote.style.display = 'block';
    } else {
        investmentValueField.readOnly = false;
        calculatedNote.style.display = 'none';
    }
}

function openModal(investmentId = null) {
    const modal = document.getElementById('investmentModal');
    const form = document.getElementById('investmentForm');
    const title = document.getElementById('modalTitle');

    form.reset();
    document.getElementById('investmentId').value = '';
    document.getElementById('investmentValue').readOnly = false;
    document.getElementById('calculatedNote').style.display = 'none';

    // Reset stock lookup UI
    document.getElementById('stockLookupGroup').style.display = 'none';
    document.getElementById('tickerHint').style.display = 'none';
    document.getElementById('tickerHintAcoes').style.display = 'none';
    document.getElementById('tickerHintFiis').style.display = 'none';
    document.getElementById('stockQuoteInfo').style.display = 'none';
    document.getElementById('stockQuoteError').style.display = 'none';

    if (investmentId) {
        // Edit mode
        const investment = investments.find(i => i.id === investmentId);
        if (investment) {
            title.textContent = 'Editar Investimento';
            document.getElementById('investmentId').value = investment.id;
            document.getElementById('investmentType').value = investment.investmentTypeId;
            document.getElementById('investmentName').value = investment.name;
            document.getElementById('investmentValue').value = investment.currentValue;
            document.getElementById('investmentWeight').value = investment.weight;

            // Set investment date (convert from ISO format to date input format)
            const createdDate = new Date(investment.createdDate);
            document.getElementById('investmentDate').value = createdDate.toISOString().split('T')[0];
            // Disable date field in edit mode (can't change creation date)
            document.getElementById('investmentDate').disabled = true;

            // Load unit value and quantity if available
            if (investment.unitValue !== null && investment.unitValue !== undefined) {
                document.getElementById('unitValue').value = investment.unitValue;
            }
            if (investment.quantity !== null && investment.quantity !== undefined) {
                document.getElementById('quantity').value = investment.quantity;
            }

            // Trigger calculation if both values are present
            if (investment.unitValue && investment.quantity) {
                calculateInvestmentValue();
            }

            // Show stock lookup if "Ações Nacionais"
            toggleStockLookup();
        }
    } else {
        // Add mode
        title.textContent = 'Adicionar Investimento';
        document.getElementById('investmentWeight').value = '1';

        // Set default date to today
        const today = new Date().toISOString().split('T')[0];
        document.getElementById('investmentDate').value = today;
        // Enable date field in add mode
        document.getElementById('investmentDate').disabled = false;
    }

    modal.style.display = 'block';
}

function closeModal() {
    const modal = document.getElementById('investmentModal');
    modal.style.display = 'none';
}

async function handleFormSubmit(event) {
    event.preventDefault();

    const investmentId = document.getElementById('investmentId').value;
    const unitValue = document.getElementById('unitValue').value;
    const quantity = document.getElementById('quantity').value;

    const data = {
        investmentTypeId: parseInt(document.getElementById('investmentType').value),
        name: document.getElementById('investmentName').value,
        currentValue: parseFloat(document.getElementById('investmentValue').value),
        weight: parseFloat(document.getElementById('investmentWeight').value)
    };

    // Add unit value and quantity if provided
    if (unitValue !== '' && unitValue !== null) {
        data.unitValue = parseFloat(unitValue);
    }
    if (quantity !== '' && quantity !== null) {
        data.quantity = parseFloat(quantity);
    }

    // Add created date when creating a new investment
    if (!investmentId) {
        const investmentDate = document.getElementById('investmentDate').value;
        if (investmentDate) {
            // Convert to ISO format with time (beginning of day in UTC)
            data.createdDate = new Date(investmentDate + 'T00:00:00Z').toISOString();
        }
    }

    try {
        let response;

        if (investmentId) {
            // Update existing investment
            response = await fetchWithAuth(`${API_BASE_URL}/investments/${investmentId}`, {
                method: 'PUT',
                body: JSON.stringify(data)
            });
        } else {
            // Create new investment
            response = await fetchWithAuth(`${API_BASE_URL}/investments`, {
                method: 'POST',
                body: JSON.stringify(data)
            });
        }

        if (response && response.ok) {
            closeModal();
            await loadInvestments();
        } else {
            alert('Erro ao salvar investimento');
        }
    } catch (error) {
        console.error('Error saving investment:', error);
        alert('Erro ao salvar investimento');
    }
}

function editInvestment(id) {
    openModal(id);
}

async function deleteInvestment(id) {
    if (!confirm('Tem certeza que deseja excluir este investimento?')) {
        return;
    }

    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/investments/${id}`, {
            method: 'DELETE'
        });

        if (response && response.ok) {
            await loadInvestments();
        } else {
            alert('Erro ao excluir investimento');
        }
    } catch (error) {
        console.error('Error deleting investment:', error);
        alert('Erro ao excluir investimento');
    }
}

function viewHistory(id) {
    // Redirect to history page with the investment selected
    window.location.href = `history.html?investmentId=${id}`;
}

// Stock lookup functionality for "Ações Nacionais" and "Fundos Imobiliários"
function toggleStockLookup() {
    const investmentTypeId = parseInt(document.getElementById('investmentType').value);
    const stockLookupGroup = document.getElementById('stockLookupGroup');
    const tickerHint = document.getElementById('tickerHint');
    const tickerHintAcoes = document.getElementById('tickerHintAcoes');
    const tickerHintFiis = document.getElementById('tickerHintFiis');
    const stockQuoteInfo = document.getElementById('stockQuoteInfo');
    const stockQuoteError = document.getElementById('stockQuoteError');

    // ID 1 is "Ações Nacionais", ID 2 is "Fundos Imobiliários"
    if (investmentTypeId === 1 || investmentTypeId === 2) {
        stockLookupGroup.style.display = 'block';
        tickerHint.style.display = 'block';

        // Show appropriate hint based on investment type
        if (investmentTypeId === 1) {
            tickerHintAcoes.style.display = 'inline';
            tickerHintFiis.style.display = 'none';
        } else if (investmentTypeId === 2) {
            tickerHintAcoes.style.display = 'none';
            tickerHintFiis.style.display = 'inline';
        }
    } else {
        stockLookupGroup.style.display = 'none';
        tickerHint.style.display = 'none';
        tickerHintAcoes.style.display = 'none';
        tickerHintFiis.style.display = 'none';
        stockQuoteInfo.style.display = 'none';
        stockQuoteError.style.display = 'none';
    }
}

async function fetchStockQuote() {
    const ticker = document.getElementById('investmentName').value.trim();
    const fetchBtnText = document.getElementById('fetchBtnText');
    const fetchBtnLoading = document.getElementById('fetchBtnLoading');
    const stockQuoteInfo = document.getElementById('stockQuoteInfo');
    const stockQuoteError = document.getElementById('stockQuoteError');

    if (!ticker) {
        stockQuoteError.textContent = 'Digite o código da ação primeiro';
        stockQuoteError.style.display = 'inline';
        stockQuoteInfo.style.display = 'none';
        return;
    }

    // Show loading state
    fetchBtnText.style.display = 'none';
    fetchBtnLoading.style.display = 'inline';
    stockQuoteInfo.style.display = 'none';
    stockQuoteError.style.display = 'none';
    document.getElementById('fetchStockBtn').disabled = true;

    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/stockquotes/${ticker}`);

        if (!response) {
            throw new Error('Falha na requisição');
        }

        if (!response.ok) {
            // Try to parse error message from response
            let errorMessage = 'Erro ao buscar cotação';

            try {
                const errorData = await response.json();
                if (errorData.message) {
                    errorMessage = errorData.message;
                }
            } catch (e) {
                // If can't parse JSON, use default error messages
                if (response.status === 404) {
                    errorMessage = 'Ação não encontrada. Verifique o código digitado.';
                } else if (response.status === 402) {
                    errorMessage = 'Token da API Brapi necessário para esta ação. Apenas PETR4, MGLU3, VALE3 e ITUB4 estão disponíveis sem token.';
                } else if (response.status === 401 || response.status === 403) {
                    errorMessage = 'Token da API Brapi inválido ou expirado.';
                }
            }

            throw new Error(errorMessage);
        }

        const quote = await response.json();

        // Update unit value with stock price
        document.getElementById('unitValue').value = quote.regularMarketPrice.toFixed(2);

        // Show success message
        const changePercent = quote.regularMarketChangePercent.toFixed(2);
        const changeSign = quote.regularMarketChangePercent >= 0 ? '+' : '';
        stockQuoteInfo.innerHTML = `
            <strong>${quote.longName}</strong> -
            R$ ${quote.regularMarketPrice.toFixed(2)}
            (${changeSign}${changePercent}%)
        `;
        stockQuoteInfo.style.display = 'inline';

        // Trigger calculation if quantity is filled
        calculateInvestmentValue();
    } catch (error) {
        console.error('Error fetching stock quote:', error);
        stockQuoteError.textContent = error.message || 'Erro ao buscar cotação';
        stockQuoteError.style.display = 'inline';
    } finally {
        // Reset loading state
        fetchBtnText.style.display = 'inline';
        fetchBtnLoading.style.display = 'none';
        document.getElementById('fetchStockBtn').disabled = false;
    }
}

async function updateAllStocks() {
    const updateBtn = document.getElementById('updateAllStocksBtn');

    // Get all investments that are stocks (type 1) or FIIs (type 2)
    const stockInvestments = investments.filter(inv =>
        inv.investmentTypeId === 1 || inv.investmentTypeId === 2
    );

    if (stockInvestments.length === 0) {
        alert('Nenhuma ação ou FII encontrado para atualizar.');
        return;
    }

    // Confirm with user
    if (!confirm(`Deseja atualizar ${stockInvestments.length} ações/FIIs? Isso pode levar alguns segundos.`)) {
        return;
    }

    updateBtn.disabled = true;
    updateBtn.textContent = 'Atualizando...';

    let successCount = 0;
    let errorCount = 0;
    const errors = [];

    for (const investment of stockInvestments) {
        try {
            // Fetch current quote
            const response = await fetchWithAuth(`${API_BASE_URL}/stockquotes/${investment.name}`);

            if (!response.ok) {
                if (response.status === 402) {
                    errors.push(`${investment.name}: Token da API necessário`);
                } else if (response.status === 404) {
                    errors.push(`${investment.name}: Cotação não encontrada`);
                } else {
                    errors.push(`${investment.name}: Erro ${response.status}`);
                }
                errorCount++;
                continue;
            }

            const quote = await response.json();

            // Update investment with new price
            const updateData = {
                name: investment.name,
                currentValue: investment.quantity ? quote.regularMarketPrice * investment.quantity : quote.regularMarketPrice,
                unitValue: quote.regularMarketPrice,
                quantity: investment.quantity || 1,
                weight: investment.weight
            };

            const updateResponse = await fetchWithAuth(`${API_BASE_URL}/investments/${investment.id}`, {
                method: 'PUT',
                body: JSON.stringify(updateData)
            });

            if (updateResponse && updateResponse.ok) {
                successCount++;
            } else {
                errors.push(`${investment.name}: Erro ao salvar atualização`);
                errorCount++;
            }

        } catch (error) {
            console.error(`Error updating ${investment.name}:`, error);
            errors.push(`${investment.name}: ${error.message}`);
            errorCount++;
        }
    }

    // Show results
    let message = `Atualização concluída!\n\n`;
    message += `✓ ${successCount} investimento(s) atualizado(s) com sucesso\n`;
    if (errorCount > 0) {
        message += `✗ ${errorCount} erro(s)\n\n`;
        message += `Erros:\n${errors.join('\n')}`;
    }
    alert(message);

    // Reload investments to show updated values
    await loadInvestments();

    updateBtn.disabled = false;
    updateBtn.textContent = 'Atualizar Todas as Ações e FIIs';
}

// Initialize page
document.addEventListener('DOMContentLoaded', () => {
    loadData();

    // Event listeners
    document.getElementById('addInvestmentBtn').addEventListener('click', () => openModal());
    document.getElementById('investmentForm').addEventListener('submit', handleFormSubmit);
    document.getElementById('cancelBtn').addEventListener('click', closeModal);
    document.querySelector('.close').addEventListener('click', closeModal);
    document.getElementById('typeFilter').addEventListener('change', (e) => {
        currentFilter = e.target.value;
        loadInvestments();
    });

    // Add event listeners for automatic calculation
    document.getElementById('unitValue').addEventListener('input', calculateInvestmentValue);
    document.getElementById('quantity').addEventListener('input', calculateInvestmentValue);

    // Add event listener for investment type change (stock lookup toggle)
    document.getElementById('investmentType').addEventListener('change', toggleStockLookup);

    // Add event listener for stock quote fetch button
    document.getElementById('fetchStockBtn').addEventListener('click', fetchStockQuote);

    // Add event listener for update all stocks button
    document.getElementById('updateAllStocksBtn').addEventListener('click', updateAllStocks);

    // Close modal when pressing Esc key
    document.addEventListener('keydown', (event) => {
        if (event.key === 'Escape') {
            const modal = document.getElementById('investmentModal');
            if (modal.style.display === 'block') {
                closeModal();
            }
        }
    });
});
