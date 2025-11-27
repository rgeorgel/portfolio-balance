// Transactions page functionality
let investments = [];
let transactions = [];
let currentInvestmentFilter = '';
let currentTypeFilter = '';

async function loadData() {
    try {
        // Load investments for dropdown
        const investmentsResponse = await fetchWithAuth(`${API_BASE_URL}/investments`);
        if (!investmentsResponse) return;
        investments = await investmentsResponse.json();

        // Populate investment filter dropdown
        populateInvestmentFilter();

        // Populate modal dropdown
        populateModalInvestmentDropdown();

        // Load transactions
        await loadTransactions();
    } catch (error) {
        console.error('Error loading data:', error);
    }
}

function populateInvestmentFilter() {
    const select = document.getElementById('investmentFilter');
    select.innerHTML = '<option value="">Todos</option>';

    investments.forEach(investment => {
        const option = document.createElement('option');
        option.value = investment.id;
        option.textContent = `${investment.name} (${investment.investmentTypeName})`;
        select.appendChild(option);
    });
}

function populateModalInvestmentDropdown() {
    const select = document.getElementById('transactionInvestment');
    select.innerHTML = '<option value="">Selecione...</option>';

    investments.forEach(investment => {
        const option = document.createElement('option');
        option.value = investment.id;
        option.textContent = `${investment.name} (${investment.investmentTypeName})`;
        select.appendChild(option);
    });
}

async function loadTransactions() {
    try {
        let url = `${API_BASE_URL}/investmenttransactions`;

        // Apply filters
        if (currentInvestmentFilter) {
            url = `${API_BASE_URL}/investmenttransactions/investment/${currentInvestmentFilter}`;
        }

        const response = await fetchWithAuth(url);
        if (!response) return;
        let allTransactions = await response.json();

        // Apply type filter
        if (currentTypeFilter) {
            allTransactions = allTransactions.filter(t => t.type == currentTypeFilter);
        }

        transactions = allTransactions;
        renderTransactionsTable();
    } catch (error) {
        console.error('Error loading transactions:', error);
    }
}

function getTransactionTypeName(type) {
    switch (parseInt(type)) {
        case 1:
            return 'Aporte/Compra';
        case 2:
            return 'Resgate/Venda';
        case 3:
            return 'Dividendo';
        default:
            return 'Desconhecido';
    }
}

function getTransactionTypeClass(type) {
    switch (parseInt(type)) {
        case 1:
            return 'transaction-deposit';
        case 2:
            return 'transaction-withdrawal';
        case 3:
            return 'transaction-dividend';
        default:
            return '';
    }
}

function renderTransactionsTable() {
    const container = document.getElementById('transactionsList');

    if (transactions.length === 0) {
        container.innerHTML = '<p>Nenhuma transação encontrada. Adicione transações para calcular rentabilidade com mais precisão.</p>';
        return;
    }

    let tableHtml = `
        <table>
            <thead>
                <tr>
                    <th>Data</th>
                    <th>Investimento</th>
                    <th>Tipo</th>
                    <th>Valor</th>
                    <th>Quantidade</th>
                    <th>Valor Unitário</th>
                    <th>Observações</th>
                    <th>Ações</th>
                </tr>
            </thead>
            <tbody>
    `;

    transactions.forEach(transaction => {
        const date = new Date(transaction.transactionDate).toLocaleString('pt-BR');
        const typeClass = getTransactionTypeClass(transaction.type);

        tableHtml += `
            <tr class="${typeClass}">
                <td>${date}</td>
                <td>${transaction.investmentName}</td>
                <td>${getTransactionTypeName(transaction.type)}</td>
                <td>${formatCurrency(transaction.amount)}</td>
                <td>${transaction.quantity !== null ? transaction.quantity.toFixed(4) : '-'}</td>
                <td>${transaction.unitValue !== null ? formatCurrency(transaction.unitValue) : '-'}</td>
                <td>${transaction.notes || '-'}</td>
                <td>
                    <button class="btn btn-edit" onclick="editTransaction(${transaction.id})">Editar</button>
                    <button class="btn btn-danger" onclick="deleteTransaction(${transaction.id})">Excluir</button>
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

function calculateTransactionAmount() {
    const unitValue = parseFloat(document.getElementById('transactionUnitValue').value) || 0;
    const quantity = parseFloat(document.getElementById('transactionQuantity').value) || 0;
    const amountField = document.getElementById('transactionAmount');

    if (unitValue > 0 && quantity > 0) {
        const total = unitValue * quantity;
        amountField.value = total.toFixed(2);
    }
}

function updateTypeHint() {
    const typeSelect = document.getElementById('transactionType');
    const typeHint = document.getElementById('typeHint');

    switch (typeSelect.value) {
        case '1':
            typeHint.textContent = 'Aporte: dinheiro que você está investindo (compra)';
            break;
        case '2':
            typeHint.textContent = 'Resgate: dinheiro que você está retirando (venda)';
            break;
        case '3':
            typeHint.textContent = 'Dividendo: rendimento recebido (não altera quantidade de cotas)';
            break;
        default:
            typeHint.textContent = '';
    }
}

function openModal(transactionId = null) {
    const modal = document.getElementById('transactionModal');
    const form = document.getElementById('transactionForm');
    const title = document.getElementById('modalTitle');

    form.reset();
    document.getElementById('transactionId').value = '';
    document.getElementById('typeHint').textContent = '';

    if (transactionId) {
        // Edit mode
        const transaction = transactions.find(t => t.id === transactionId);
        if (transaction) {
            title.textContent = 'Editar Transação';
            document.getElementById('transactionId').value = transaction.id;
            document.getElementById('transactionInvestment').value = transaction.investmentId;
            document.getElementById('transactionType').value = transaction.type;
            document.getElementById('transactionAmount').value = transaction.amount;

            if (transaction.unitValue !== null) {
                document.getElementById('transactionUnitValue').value = transaction.unitValue;
            }
            if (transaction.quantity !== null) {
                document.getElementById('transactionQuantity').value = transaction.quantity;
            }

            // Convert UTC date to local datetime-local format
            const date = new Date(transaction.transactionDate);
            const localDate = new Date(date.getTime() - (date.getTimezoneOffset() * 60000))
                .toISOString()
                .slice(0, 16);
            document.getElementById('transactionDate').value = localDate;

            if (transaction.notes) {
                document.getElementById('transactionNotes').value = transaction.notes;
            }

            updateTypeHint();
        }
    } else {
        // Add mode
        title.textContent = 'Adicionar Transação';

        // Set default date to now
        const now = new Date();
        const localDate = new Date(now.getTime() - (now.getTimezoneOffset() * 60000))
            .toISOString()
            .slice(0, 16);
        document.getElementById('transactionDate').value = localDate;
    }

    modal.style.display = 'block';
}

function closeModal() {
    const modal = document.getElementById('transactionModal');
    modal.style.display = 'none';
}

async function saveTransaction(event) {
    event.preventDefault();

    const transactionId = document.getElementById('transactionId').value;
    const investmentId = parseInt(document.getElementById('transactionInvestment').value);
    const type = parseInt(document.getElementById('transactionType').value);
    const amount = parseFloat(document.getElementById('transactionAmount').value);
    const unitValue = parseFloat(document.getElementById('transactionUnitValue').value) || null;
    const quantity = parseFloat(document.getElementById('transactionQuantity').value) || null;
    const transactionDate = new Date(document.getElementById('transactionDate').value).toISOString();
    const notes = document.getElementById('transactionNotes').value || null;

    const data = {
        investmentId,
        type,
        amount,
        unitValue,
        quantity,
        transactionDate,
        notes
    };

    try {
        let response;
        if (transactionId) {
            // Update
            response = await fetchWithAuth(`${API_BASE_URL}/investmenttransactions/${transactionId}`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(data)
            });
        } else {
            // Create
            response = await fetchWithAuth(`${API_BASE_URL}/investmenttransactions`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(data)
            });
        }

        if (response && response.ok) {
            closeModal();
            await loadTransactions();
        } else {
            alert('Erro ao salvar transação');
        }
    } catch (error) {
        console.error('Error saving transaction:', error);
        alert('Erro ao salvar transação');
    }
}

async function deleteTransaction(transactionId) {
    if (!confirm('Tem certeza que deseja excluir esta transação?')) {
        return;
    }

    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/investmenttransactions/${transactionId}`, {
            method: 'DELETE'
        });

        if (response && response.ok) {
            await loadTransactions();
        } else {
            alert('Erro ao excluir transação');
        }
    } catch (error) {
        console.error('Error deleting transaction:', error);
        alert('Erro ao excluir transação');
    }
}

function editTransaction(transactionId) {
    openModal(transactionId);
}

// Event listeners
document.getElementById('addTransactionBtn').addEventListener('click', () => openModal());
document.getElementById('transactionForm').addEventListener('submit', saveTransaction);
document.getElementById('cancelBtn').addEventListener('click', closeModal);

// Close modal on X click
document.querySelector('.close').addEventListener('click', closeModal);

// Close modal on outside click
window.addEventListener('click', (event) => {
    const modal = document.getElementById('transactionModal');
    if (event.target === modal) {
        closeModal();
    }
});

// Filter listeners
document.getElementById('investmentFilter').addEventListener('change', (e) => {
    currentInvestmentFilter = e.target.value;
    loadTransactions();
});

document.getElementById('typeFilter').addEventListener('change', (e) => {
    currentTypeFilter = e.target.value;
    loadTransactions();
});

// Calculate amount when unit value or quantity changes
document.getElementById('transactionUnitValue').addEventListener('input', calculateTransactionAmount);
document.getElementById('transactionQuantity').addEventListener('input', calculateTransactionAmount);

// Update type hint when type changes
document.getElementById('transactionType').addEventListener('change', updateTypeHint);

// Load data on page load
loadData();
