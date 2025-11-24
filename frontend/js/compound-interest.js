// Compound Interest Calculator

const STORAGE_KEY = 'compoundInterestData';
let interestChart = null;

// Load saved data from localStorage on page load
window.addEventListener('DOMContentLoaded', () => {
    loadSavedData();
});

// Handle form submission
document.getElementById('compoundInterestForm').addEventListener('submit', function(e) {
    e.preventDefault();
    calculateCompoundInterest();
});

// Load saved data from localStorage
function loadSavedData() {
    const savedData = localStorage.getItem(STORAGE_KEY);
    if (savedData) {
        try {
            const data = JSON.parse(savedData);
            document.getElementById('initialValue').value = data.initialValue || '';
            document.getElementById('monthlyValue').value = data.monthlyValue || '';
            document.getElementById('interestRate').value = data.interestRate || '';
            document.getElementById('period').value = data.period || '';
            document.getElementById('periodType').value = data.periodType || 'months';
        } catch (error) {
            console.error('Error loading saved data:', error);
        }
    }
}

// Save data to localStorage
function saveData() {
    const data = {
        initialValue: document.getElementById('initialValue').value,
        monthlyValue: document.getElementById('monthlyValue').value,
        interestRate: document.getElementById('interestRate').value,
        period: document.getElementById('period').value,
        periodType: document.getElementById('periodType').value
    };
    localStorage.setItem(STORAGE_KEY, JSON.stringify(data));
}

// Calculate compound interest
function calculateCompoundInterest() {
    // Get form values
    const initialValue = parseFloat(document.getElementById('initialValue').value) || 0;
    const monthlyValue = parseFloat(document.getElementById('monthlyValue').value) || 0;
    const annualInterestRate = parseFloat(document.getElementById('interestRate').value) || 0;
    const periodValue = parseInt(document.getElementById('period').value) || 0;
    const periodType = document.getElementById('periodType').value;

    // Validate inputs
    if (periodValue < 1) {
        alert('O período deve ser maior que 0');
        return;
    }

    // Convert period to months if needed
    const periodInMonths = periodType === 'years' ? periodValue * 12 : periodValue;

    // Convert annual interest rate to monthly rate
    // Formula: (1 + annual_rate)^(1/12) - 1
    const monthlyInterestRate = Math.pow(1 + (annualInterestRate / 100), 1/12) - 1;

    // Save data to localStorage
    saveData();

    // Calculate month by month
    const monthlyData = [];
    let currentBalance = initialValue;
    let totalInvested = initialValue;

    // Month 0 (initial value)
    monthlyData.push({
        month: 0,
        contribution: initialValue,
        interest: 0,
        balance: initialValue
    });

    // Calculate for each month
    for (let month = 1; month <= periodInMonths; month++) {
        // Add monthly contribution
        currentBalance += monthlyValue;
        totalInvested += monthlyValue;

        // Calculate interest using monthly rate
        const monthInterest = currentBalance * monthlyInterestRate;
        currentBalance += monthInterest;

        monthlyData.push({
            month: month,
            contribution: monthlyValue,
            interest: monthInterest,
            balance: currentBalance
        });
    }

    // Calculate totals
    const finalTotal = currentBalance;
    const totalInterest = finalTotal - totalInvested;

    // Display results
    displayResults(finalTotal, totalInvested, totalInterest, monthlyData);
}

// Display calculation results
function displayResults(finalTotal, totalInvested, totalInterest, monthlyData) {
    // Update summary
    document.getElementById('finalTotal').textContent = formatCurrency(finalTotal);
    document.getElementById('totalInvested').textContent = formatCurrency(totalInvested);
    document.getElementById('totalInterest').textContent = formatCurrency(totalInterest);

    // Create/update chart
    createChart(monthlyData, totalInvested);

    // Generate monthly table
    const tableBody = document.getElementById('monthlyTableBody');
    tableBody.innerHTML = '';

    monthlyData.forEach(data => {
        const row = document.createElement('tr');
        row.innerHTML = `
            <td>${data.month}</td>
            <td>${formatCurrency(data.contribution)}</td>
            <td>${formatCurrency(data.interest)}</td>
            <td class="highlight">${formatCurrency(data.balance)}</td>
        `;
        tableBody.appendChild(row);
    });

    // Show results section
    document.getElementById('calculationResults').style.display = 'block';

    // Scroll to results
    document.getElementById('calculationResults').scrollIntoView({
        behavior: 'smooth',
        block: 'start'
    });
}

// Create or update the evolution chart
function createChart(monthlyData, finalTotalInvested) {
    const ctx = document.getElementById('interestChart').getContext('2d');

    // Prepare data for the chart
    const labels = monthlyData.map(d => `Mês ${d.month}`);

    // Calculate accumulated invested value for each month
    const initialValue = monthlyData[0].contribution;
    const monthlyContribution = monthlyData.length > 1 ? monthlyData[1].contribution : 0;

    const investedValues = monthlyData.map((d, index) => {
        return initialValue + (monthlyContribution * index);
    });

    // Calculate interest values for each month
    const interestValues = monthlyData.map((d, index) => {
        return d.balance - investedValues[index];
    });

    const totalAccumulated = monthlyData.map(d => d.balance);

    // Destroy previous chart if it exists
    if (interestChart) {
        interestChart.destroy();
    }

    // Create new chart
    interestChart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels,
            datasets: [
                {
                    label: 'Total Acumulado',
                    data: totalAccumulated,
                    borderColor: '#3b82f6',
                    backgroundColor: 'rgba(59, 130, 246, 0.1)',
                    borderWidth: 2,
                    tension: 0.1,
                    fill: true
                },
                {
                    label: 'Valor Investido',
                    data: investedValues,
                    borderColor: '#10b981',
                    backgroundColor: 'rgba(16, 185, 129, 0.1)',
                    borderWidth: 2,
                    tension: 0.1,
                    fill: true
                },
                {
                    label: 'Total em Juros',
                    data: interestValues,
                    borderColor: '#f59e0b',
                    backgroundColor: 'rgba(245, 158, 11, 0.1)',
                    borderWidth: 2,
                    tension: 0.1,
                    fill: true
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            aspectRatio: 2,
            interaction: {
                mode: 'index',
                intersect: false
            },
            plugins: {
                legend: {
                    position: 'top',
                    labels: {
                        usePointStyle: true,
                        padding: 15
                    }
                },
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            let label = context.dataset.label || '';
                            if (label) {
                                label += ': ';
                            }
                            label += formatCurrency(context.parsed.y);
                            return label;
                        }
                    }
                }
            },
            scales: {
                x: {
                    display: true,
                    title: {
                        display: true,
                        text: 'Tempo'
                    },
                    ticks: {
                        maxTicksLimit: 12
                    }
                },
                y: {
                    display: true,
                    title: {
                        display: true,
                        text: 'Valor (R$)'
                    },
                    ticks: {
                        callback: function(value) {
                            return 'R$ ' + value.toLocaleString('pt-BR');
                        }
                    }
                }
            }
        }
    });
}

// Clear form and localStorage
function clearForm() {
    document.getElementById('compoundInterestForm').reset();
    localStorage.removeItem(STORAGE_KEY);
    document.getElementById('calculationResults').style.display = 'none';
}

// Format number as currency
function formatCurrency(value) {
    return new Intl.NumberFormat('pt-BR', {
        style: 'currency',
        currency: 'BRL'
    }).format(value);
}
