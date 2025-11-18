// Allocations page functionality
let investmentTypes = [];

async function loadInvestmentTypes() {
    try {
        const response = await fetch(`${API_BASE_URL}/investmenttypes`);
        investmentTypes = await response.json();
        renderAllocationInputs();
    } catch (error) {
        console.error('Error loading investment types:', error);
        showMessage('Erro ao carregar tipos de investimento', 'error');
    }
}

function renderAllocationInputs() {
    const container = document.getElementById('allocationInputs');
    container.innerHTML = '';

    investmentTypes.forEach(type => {
        const inputHtml = `
            <div class="allocation-input-group">
                <div class="form-group">
                    <label for="allocation-${type.id}">${type.name}</label>
                </div>
                <div class="form-group">
                    <input
                        type="number"
                        id="allocation-${type.id}"
                        data-type-id="${type.id}"
                        value="${type.allocationPercentage}"
                        step="0.01"
                        min="0"
                        max="100"
                        class="allocation-input">
                    <small>%</small>
                </div>
            </div>
        `;
        container.innerHTML += inputHtml;
    });

    // Add event listeners to update total
    const inputs = document.querySelectorAll('.allocation-input');
    inputs.forEach(input => {
        input.addEventListener('input', updateTotalAllocation);
    });

    updateTotalAllocation();
}

function updateTotalAllocation() {
    const inputs = document.querySelectorAll('.allocation-input');
    let total = 0;

    inputs.forEach(input => {
        const value = parseFloat(input.value) || 0;
        total += value;
    });

    const totalElement = document.getElementById('totalAllocation');
    totalElement.textContent = total.toFixed(2);

    // Change color based on validity
    if (Math.abs(total - 100) < 0.01) {
        totalElement.style.color = '#27ae60';
    } else {
        totalElement.style.color = '#e74c3c';
    }
}

async function handleFormSubmit(event) {
    event.preventDefault();

    const inputs = document.querySelectorAll('.allocation-input');
    const allocations = [];
    let total = 0;

    inputs.forEach(input => {
        const value = parseFloat(input.value) || 0;
        total += value;
        allocations.push({
            id: parseInt(input.dataset.typeId),
            allocationPercentage: value
        });
    });

    if (Math.abs(total - 100) > 0.01) {
        showMessage('O total de alocação deve ser 100%', 'error');
        return;
    }

    try {
        const response = await fetch(`${API_BASE_URL}/investmenttypes/allocations`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(allocations)
        });

        if (response.ok) {
            showMessage('Alocações salvas com sucesso!', 'success');
        } else {
            const errorText = await response.text();
            showMessage(errorText || 'Erro ao salvar alocações', 'error');
        }
    } catch (error) {
        console.error('Error saving allocations:', error);
        showMessage('Erro ao salvar alocações', 'error');
    }
}

function showMessage(text, type) {
    const messageElement = document.getElementById('message');
    messageElement.textContent = text;
    messageElement.className = `message ${type}`;
    messageElement.style.display = 'block';

    setTimeout(() => {
        messageElement.style.display = 'none';
    }, 5000);
}

// Initialize page
document.addEventListener('DOMContentLoaded', () => {
    loadInvestmentTypes();
    document.getElementById('allocationsForm').addEventListener('submit', handleFormSubmit);
});
