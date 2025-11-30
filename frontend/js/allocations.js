// Allocations page functionality
let investmentTypes = [];
let pieChart = null;

// Default allocation percentages
const DEFAULT_ALLOCATIONS = {
    'Ações Nacionais': 20,
    'Fundos Imobiliários': 10,
    'Renda Fixa': 70,
    'Ações Internacionais': 0,
    'Criptomoedas': 0
};

// Colors for each investment type
const INVESTMENT_COLORS = {
    'Ações Internacionais': '#5DADE2',
    'Ações Nacionais': '#48C9B0',
    'Fundos Imobiliários': '#F4D03F',
    'REITs': '#BB8FCE',
    'Criptomoedas': '#85C1E2',
    'Renda Fixa': '#E59866',
    'Renda Fixa Internacional': '#A569BD'
};

async function loadInvestmentTypes() {
    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/investmenttypes`);
        if (!response) return;
        investmentTypes = await response.json();

        // Apply default allocations if allocation is 0
        investmentTypes.forEach(type => {
            if (type.allocationPercentage === 0 || type.allocationPercentage === null) {
                type.allocationPercentage = DEFAULT_ALLOCATIONS[type.name] || 0;
            }
        });

        renderAllocationInputs();
        renderPieChart();
        renderChartLegend();
    } catch (error) {
        console.error('Error loading investment types:', error);
        showMessage('Erro ao carregar tipos de investimento', 'error');
    }
}

function renderAllocationInputs() {
    const container = document.getElementById('allocationInputs');
    container.innerHTML = '';

    investmentTypes.forEach(type => {
        const color = INVESTMENT_COLORS[type.name] || '#999';
        const inputHtml = `
            <div class="allocation-slider-group">
                <div class="allocation-label">
                    <span class="color-indicator" style="background-color: ${color}"></span>
                    <label for="allocation-${type.id}">${type.name}</label>
                    <span class="allocation-value" id="value-${type.id}">${Math.round(type.allocationPercentage)}%</span>
                </div>
                <div class="slider-container">
                    <input
                        type="range"
                        id="slider-${type.id}"
                        data-type-id="${type.id}"
                        value="${type.allocationPercentage}"
                        min="0"
                        max="100"
                        step="1"
                        class="allocation-slider"
                        style="--slider-color: ${color}">
                    <div class="slider-labels">
                        <span>0%</span>
                        <span>100%</span>
                    </div>
                </div>
            </div>
        `;
        container.innerHTML += inputHtml;
    });

    // Add event listeners to update total and chart
    const sliders = document.querySelectorAll('.allocation-slider');
    sliders.forEach(slider => {
        slider.addEventListener('input', handleSliderChange);
        // Initialize the slider progress
        slider.style.setProperty('--value', `${slider.value}%`);
    });

    updateTotalAllocation();
}

function handleSliderChange(event) {
    const slider = event.target;
    const typeId = parseInt(slider.dataset.typeId);
    const value = parseFloat(slider.value);

    // Update the displayed value
    const valueDisplay = document.getElementById(`value-${typeId}`);
    valueDisplay.textContent = `${Math.round(value)}%`;

    // Update slider progress
    slider.style.setProperty('--value', `${value}%`);

    // Update the investmentTypes array
    const type = investmentTypes.find(t => t.id === typeId);
    if (type) {
        type.allocationPercentage = value;
    }

    updateTotalAllocation();
    updatePieChart();
}

function updateTotalAllocation() {
    const sliders = document.querySelectorAll('.allocation-slider');
    let total = 0;

    sliders.forEach(slider => {
        const value = parseFloat(slider.value) || 0;
        total += value;
    });

    const totalElement = document.getElementById('totalAllocation');
    totalElement.textContent = Math.round(total);

    // Change color based on validity
    if (Math.abs(total - 100) < 0.01) {
        totalElement.style.color = '#27ae60';
    } else {
        totalElement.style.color = '#e74c3c';
    }
}

function renderPieChart() {
    const canvas = document.getElementById('pieChart');
    if (!canvas) return;

    const ctx = canvas.getContext('2d');
    const centerX = canvas.width / 2;
    const centerY = canvas.height / 2;
    const radius = Math.min(centerX, centerY) - 10;

    // Clear canvas
    ctx.clearRect(0, 0, canvas.width, canvas.height);

    // Filter out types with 0 allocation
    const activeTypes = investmentTypes.filter(t => t.allocationPercentage > 0);

    if (activeTypes.length === 0) {
        // Draw empty circle
        ctx.beginPath();
        ctx.arc(centerX, centerY, radius, 0, 2 * Math.PI);
        ctx.fillStyle = '#e0e0e0';
        ctx.fill();
        ctx.strokeStyle = '#bbb';
        ctx.lineWidth = 2;
        ctx.stroke();
        return;
    }

    let currentAngle = -Math.PI / 2; // Start at top

    activeTypes.forEach(type => {
        const sliceAngle = (type.allocationPercentage / 100) * 2 * Math.PI;
        const color = INVESTMENT_COLORS[type.name] || '#999';

        // Draw slice
        ctx.beginPath();
        ctx.moveTo(centerX, centerY);
        ctx.arc(centerX, centerY, radius, currentAngle, currentAngle + sliceAngle);
        ctx.closePath();
        ctx.fillStyle = color;
        ctx.fill();
        ctx.strokeStyle = '#fff';
        ctx.lineWidth = 3;
        ctx.stroke();

        currentAngle += sliceAngle;
    });

    // Draw center circle for donut effect
    ctx.beginPath();
    ctx.arc(centerX, centerY, radius * 0.6, 0, 2 * Math.PI);
    ctx.fillStyle = '#1a1a1a';
    ctx.fill();
}

function updatePieChart() {
    renderPieChart();
}

function renderChartLegend() {
    const legendContainer = document.getElementById('chartLegend');
    if (!legendContainer) return;

    legendContainer.innerHTML = '';

    investmentTypes.forEach(type => {
        const color = INVESTMENT_COLORS[type.name] || '#999';
        const legendItem = document.createElement('div');
        legendItem.className = 'legend-item';
        legendItem.innerHTML = `
            <span class="legend-color" style="background-color: ${color}"></span>
            <span class="legend-label">${type.name}</span>
        `;
        legendContainer.appendChild(legendItem);
    });
}

function resetAllocations() {
    investmentTypes.forEach(type => {
        const defaultValue = DEFAULT_ALLOCATIONS[type.name] || 0;
        type.allocationPercentage = defaultValue;

        const slider = document.getElementById(`slider-${type.id}`);
        if (slider) {
            slider.value = defaultValue;
            slider.style.setProperty('--value', `${defaultValue}%`);
        }

        const valueDisplay = document.getElementById(`value-${type.id}`);
        if (valueDisplay) {
            valueDisplay.textContent = `${Math.round(defaultValue)}%`;
        }
    });

    updateTotalAllocation();
    updatePieChart();
}

async function handleFormSubmit(event) {
    event.preventDefault();

    const sliders = document.querySelectorAll('.allocation-slider');
    const allocations = [];
    let total = 0;

    sliders.forEach(slider => {
        const value = parseFloat(slider.value) || 0;
        total += value;
        allocations.push({
            id: parseInt(slider.dataset.typeId),
            allocationPercentage: value
        });
    });

    if (Math.abs(total - 100) > 0.01) {
        showMessage('O total de alocação deve ser 100%', 'error');
        return;
    }

    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/investmenttypes/allocations`, {
            method: 'PUT',
            body: JSON.stringify(allocations)
        });

        if (!response) return;

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
