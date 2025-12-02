// Calculator page functionality

function incrementAmount(value) {
    const input = document.getElementById('newInvestmentAmount');
    const currentValue = parseFloat(input.value) || 0;
    input.value = (currentValue + value).toFixed(2);
}

async function handleCalculation(event) {
    event.preventDefault();

    const newInvestmentAmount = parseFloat(document.getElementById('newInvestmentAmount').value);

    if (newInvestmentAmount <= 0) {
        alert('Por favor, informe um valor válido');
        return;
    }

    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/portfolio/calculate-balance`, {
            method: 'POST',
            body: JSON.stringify({
                newInvestmentAmount: newInvestmentAmount
            })
        });

        if (response && response.ok) {
            const result = await response.json();
            displayResults(result);
        } else {
            alert('Erro ao calcular balanceamento');
        }
    } catch (error) {
        console.error('Error calculating balance:', error);
        alert('Erro ao calcular balanceamento');
    }
}

function displayResults(result) {
    // Update summary
    document.getElementById('currentTotal').textContent = formatCurrency(result.totalPortfolioValue);
    document.getElementById('newInvestment').textContent = formatCurrency(result.newInvestmentAmount);
    document.getElementById('totalAfter').textContent = formatCurrency(result.totalAfterInvestment);

    // Render allocation results
    const container = document.getElementById('allocationResults');
    container.innerHTML = '';

    result.allocations.forEach(allocation => {
        // Filter investment allocations to only show values >= 100
        let validInvestmentAllocations = [];
        if (allocation.investmentAllocations && allocation.investmentAllocations.length > 0) {
            validInvestmentAllocations = allocation.investmentAllocations.filter(inv => inv.amountToInvest >= 100);
        }

        // Skip this allocation type if there are no valid suggestions (>= 100)
        if (validInvestmentAllocations.length === 0 && allocation.amountToInvest < 100) {
            return;
        }

        let allocationHtml = `
            <div class="allocation-result">
                <h4>${allocation.investmentTypeName}</h4>

                <div class="allocation-details">
                    <div class="detail-item">
                        <span class="label">Percentual Alvo</span>
                        <span class="value">${formatPercentage(allocation.targetPercentage)}</span>
                    </div>
                    <div class="detail-item">
                        <span class="label">Valor Atual</span>
                        <span class="value">${formatCurrency(allocation.currentValue)}</span>
                    </div>
                    <div class="detail-item">
                        <span class="label">Percentual Atual</span>
                        <span class="value">${formatPercentage(allocation.currentPercentage)}</span>
                    </div>
                    <div class="detail-item">
                        <span class="label">Valor a Investir</span>
                        <span class="value" style="color: #27ae60;">${formatCurrency(allocation.amountToInvest)}</span>
                    </div>
                    <div class="detail-item">
                        <span class="label">Valor Após Investimento</span>
                        <span class="value">${formatCurrency(allocation.valueAfterInvestment)}</span>
                    </div>
                    <div class="detail-item">
                        <span class="label">Percentual Após Investimento</span>
                        <span class="value">${formatPercentage(allocation.percentageAfterInvestment)}</span>
                    </div>
                </div>
        `;

        // Add individual investment allocations (only those >= 100)
        if (validInvestmentAllocations.length > 0) {
            allocationHtml += `
                <div class="investment-allocations">
                    <h5>Distribuição entre Investimentos:</h5>
            `;

            validInvestmentAllocations.forEach(inv => {
                const canCalculateQuantity = inv.unitValue && inv.unitValue > 0;
                const quantityToBuy = canCalculateQuantity ? (inv.amountToInvest / inv.unitValue).toFixed(4) : '-';

                allocationHtml += `
                    <div class="investment-item">
                        <div class="investment-info">
                            <div>
                                <span class="name">${inv.investmentName}</span>
                                <span style="color: #666; font-size: 12px;"> (Peso: ${inv.weight})</span>
                            </div>
                            <div class="investment-amounts">
                                <span class="amount">Valor: +${formatCurrency(inv.amountToInvest)}</span>
                                <span class="quantity">Quantidade: ${quantityToBuy}</span>
                            </div>
                        </div>
                        <div class="investment-actions">
                            <button class="btn btn-deposit" onclick="openDepositModal(${inv.investmentId}, '${inv.investmentName}', ${inv.amountToInvest}, ${inv.unitValue || 0}, ${quantityToBuy === '-' ? 0 : quantityToBuy})">Aportar</button>
                        </div>
                    </div>
                `;
            });

            allocationHtml += `</div>`;
        } else if (allocation.amountToInvest >= 100) {
            allocationHtml += `
                <div class="investment-allocations">
                    <p style="color: #e67e22; font-style: italic;">
                        Atenção: Não há investimentos cadastrados neste tipo.
                        Cadastre investimentos para ver a distribuição detalhada.
                    </p>
                </div>
            `;
        }

        allocationHtml += `</div>`;

        container.innerHTML += allocationHtml;
    });

    // Show results section
    document.getElementById('calculationResults').style.display = 'block';
}

// Deposit Modal functionality
let currentDepositInvestment = null;

function openDepositModal(investmentId, investmentName, suggestedAmount, unitValue, suggestedQuantity) {
    currentDepositInvestment = {
        id: investmentId,
        name: investmentName
    };

    const modal = document.getElementById('depositModal');
    document.getElementById('depositInvestmentName').textContent = investmentName;
    document.getElementById('depositAmount').value = suggestedAmount.toFixed(2);
    document.getElementById('depositUnitValue').value = unitValue > 0 ? unitValue.toFixed(2) : '';
    document.getElementById('depositQuantity').value = suggestedQuantity > 0 ? suggestedQuantity : '';

    modal.style.display = 'block';
}

function closeDepositModal() {
    const modal = document.getElementById('depositModal');
    modal.style.display = 'none';
    currentDepositInvestment = null;
}

function calculateDepositAmount() {
    const unitValue = parseFloat(document.getElementById('depositUnitValue').value) || 0;
    const quantity = parseFloat(document.getElementById('depositQuantity').value) || 0;
    const amountField = document.getElementById('depositAmount');

    if (unitValue > 0 && quantity > 0) {
        const total = unitValue * quantity;
        amountField.value = total.toFixed(2);
    }
}

async function saveDeposit(event) {
    event.preventDefault();

    if (!currentDepositInvestment) {
        alert('Erro: investimento não selecionado');
        return;
    }

    const amount = parseFloat(document.getElementById('depositAmount').value);
    const unitValue = parseFloat(document.getElementById('depositUnitValue').value) || null;
    const quantity = parseFloat(document.getElementById('depositQuantity').value) || null;

    if (amount <= 0) {
        alert('Por favor, informe um valor válido');
        return;
    }

    const data = {
        investmentId: currentDepositInvestment.id,
        type: 1, // Aporte/Compra
        amount: amount,
        unitValue: unitValue,
        quantity: quantity,
        transactionDate: new Date().toISOString(),
        notes: `Aporte via calculadora de distribuição`
    };

    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/investmenttransactions`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(data)
        });

        if (response && response.ok) {
            alert('Aporte registrado com sucesso!');
            closeDepositModal();
            // Recalcular distribuição
            document.getElementById('calculatorForm').dispatchEvent(new Event('submit'));
        } else {
            alert('Erro ao registrar aporte');
        }
    } catch (error) {
        console.error('Error saving deposit:', error);
        alert('Erro ao registrar aporte');
    }
}

// Initialize page
document.addEventListener('DOMContentLoaded', () => {
    document.getElementById('calculatorForm').addEventListener('submit', handleCalculation);

    // Deposit modal event listeners
    const depositForm = document.getElementById('depositForm');
    if (depositForm) {
        depositForm.addEventListener('submit', saveDeposit);
    }

    const depositUnitValue = document.getElementById('depositUnitValue');
    const depositQuantity = document.getElementById('depositQuantity');
    if (depositUnitValue && depositQuantity) {
        depositUnitValue.addEventListener('input', calculateDepositAmount);
        depositQuantity.addEventListener('input', calculateDepositAmount);
    }

    const depositCancelBtn = document.getElementById('depositCancelBtn');
    if (depositCancelBtn) {
        depositCancelBtn.addEventListener('click', closeDepositModal);
    }

    // Close modal on X click
    const closeBtn = document.querySelector('#depositModal .close');
    if (closeBtn) {
        closeBtn.addEventListener('click', closeDepositModal);
    }

    // Close modal on outside click
    window.addEventListener('click', (event) => {
        const modal = document.getElementById('depositModal');
        if (modal && event.target === modal) {
            closeDepositModal();
        }
    });
});
