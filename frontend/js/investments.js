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
                    <th>Ações</th>
                </tr>
            </thead>
            <tbody>
    `;

    investments.forEach(investment => {
        const type = investmentTypes.find(t => t.id === investment.investmentTypeId);
        const date = new Date(investment.createdDate).toLocaleDateString('pt-BR');

        tableHtml += `
            <tr>
                <td>${investment.name}</td>
                <td>${type ? type.name : 'Desconhecido'}</td>
                <td>${formatCurrency(investment.currentValue)}</td>
                <td>${investment.weight}</td>
                <td>${date}</td>
                <td>
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
        }
    } else {
        // Add mode
        title.textContent = 'Adicionar Investimento';
        document.getElementById('investmentWeight').value = '1';
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
