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

        // Add individual investment allocations
        if (allocation.investmentAllocations && allocation.investmentAllocations.length > 0) {
            allocationHtml += `
                <div class="investment-allocations">
                    <h5>Distribuição entre Investimentos:</h5>
            `;

            allocation.investmentAllocations.forEach(inv => {
                allocationHtml += `
                    <div class="investment-item">
                        <div>
                            <span class="name">${inv.investmentName}</span>
                            <span style="color: #666; font-size: 12px;"> (Peso: ${inv.weight})</span>
                        </div>
                        <div>
                            <span class="amount">+${formatCurrency(inv.amountToInvest)}</span>
                        </div>
                    </div>
                `;
            });

            allocationHtml += `</div>`;
        } else if (allocation.amountToInvest > 0) {
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

// Initialize page
document.addEventListener('DOMContentLoaded', () => {
    document.getElementById('calculatorForm').addEventListener('submit', handleCalculation);
});
