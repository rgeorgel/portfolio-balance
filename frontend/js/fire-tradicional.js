// FIRE Tradicional Calculator

const STORAGE_KEY = 'fireTradicionalData';
let fireChart = null;

// Load saved data from localStorage on page load
window.addEventListener('DOMContentLoaded', () => {
    loadSavedData();
});

// Handle form submission
document.getElementById('fireTradicionalForm').addEventListener('submit', function(e) {
    e.preventDefault();
    calculateFIRE();
});

// Load saved data from localStorage
function loadSavedData() {
    const savedData = localStorage.getItem(STORAGE_KEY);
    if (savedData) {
        try {
            const data = JSON.parse(savedData);
            document.getElementById('currentBalance').value = data.currentBalance || '';
            document.getElementById('monthlyExpenses').value = data.monthlyExpenses || '';
            document.getElementById('monthlyContribution').value = data.monthlyContribution || '';
            document.getElementById('expectedReturn').value = data.expectedReturn || '';
        } catch (error) {
            console.error('Error loading saved data:', error);
        }
    }
}

// Save data to localStorage
function saveData() {
    const data = {
        currentBalance: document.getElementById('currentBalance').value,
        monthlyExpenses: document.getElementById('monthlyExpenses').value,
        monthlyContribution: document.getElementById('monthlyContribution').value,
        expectedReturn: document.getElementById('expectedReturn').value
    };
    localStorage.setItem(STORAGE_KEY, JSON.stringify(data));
}

// Calculate FIRE metrics
function calculateFIRE() {
    // Get form values
    const currentBalance = parseFloat(document.getElementById('currentBalance').value) || 0;
    const monthlyExpenses = parseFloat(document.getElementById('monthlyExpenses').value) || 0;
    const monthlyContribution = parseFloat(document.getElementById('monthlyContribution').value) || 0;
    const annualReturn = parseFloat(document.getElementById('expectedReturn').value) || 0;

    // Validate inputs
    if (monthlyExpenses <= 0) {
        alert('Os gastos mensais devem ser maiores que zero');
        return;
    }

    // Calculate FIRE goal using the 4% rule (25x annual expenses)
    const annualExpenses = monthlyExpenses * 12;
    const fireGoal = annualExpenses * 25;

    // Calculate monthly passive income (4% annual return / 12 months)
    const passiveIncome = (fireGoal * 0.04) / 12;

    // Convert annual return to monthly rate
    const monthlyReturn = Math.pow(1 + (annualReturn / 100), 1/12) - 1;

    // Calculate time to FIRE
    let balance = currentBalance;
    let months = 0;
    let totalInvested = currentBalance;
    const monthlyData = [];
    const maxMonths = 600; // 50 years limit

    // Store initial state
    monthlyData.push({
        month: 0,
        balance: balance,
        invested: totalInvested,
        growth: 0
    });

    // Simulate month by month until reaching FIRE goal
    while (balance < fireGoal && months < maxMonths) {
        months++;

        // Add monthly contribution
        balance += monthlyContribution;
        totalInvested += monthlyContribution;

        // Apply monthly return
        const monthGrowth = balance * monthlyReturn;
        balance += monthGrowth;

        // Store data for chart (store every month for first year, then every 3 months)
        if (months <= 12 || months % 3 === 0) {
            monthlyData.push({
                month: months,
                balance: balance,
                invested: totalInvested,
                growth: balance - totalInvested
            });
        }
    }

    // Calculate final growth
    const totalGrowth = balance - totalInvested;

    // Convert months to years and remaining months
    const years = Math.floor(months / 12);
    const remainingMonths = months % 12;

    // Save data to localStorage
    saveData();

    // Display results
    displayResults(fireGoal, months, years, remainingMonths, passiveIncome, totalInvested, totalGrowth, monthlyData);
}

// Display calculation results
function displayResults(fireGoal, totalMonths, years, months, passiveIncome, totalInvested, totalGrowth, monthlyData) {
    // Update FIRE goal
    document.getElementById('fireGoal').textContent = formatCurrency(fireGoal);

    // Update time to FIRE
    const timeText = years > 0 ? `${years} ${years === 1 ? 'ano' : 'anos'}` : '';
    const monthsText = months > 0 ? `${months} ${months === 1 ? 'mês' : 'meses'}` : '';
    document.getElementById('timeToFire').textContent = timeText || monthsText || '0 meses';

    // Update subtitle with detailed breakdown
    const timeElement = document.querySelector('#timeToFire').nextElementSibling;
    if (years > 0 && months > 0) {
        timeElement.textContent = monthsText;
    } else {
        timeElement.textContent = `${totalMonths} ${totalMonths === 1 ? 'mês' : 'meses'} no total`;
    }

    // Update passive income
    document.getElementById('passiveIncome').textContent = formatCurrency(passiveIncome);

    // Update total invested
    document.getElementById('totalInvested').textContent = formatCurrency(totalInvested);

    // Update growth
    document.getElementById('totalGrowth').textContent = formatCurrency(totalGrowth);

    // Create/update chart
    createChart(monthlyData, fireGoal);

    // Show results section
    document.getElementById('calculationResults').style.display = 'block';

    // Scroll to results
    document.getElementById('calculationResults').scrollIntoView({
        behavior: 'smooth',
        block: 'start'
    });
}

// Create or update the FIRE projection chart
function createChart(monthlyData, fireGoal) {
    const ctx = document.getElementById('fireChart').getContext('2d');

    // Prepare data for the chart
    const labels = monthlyData.map(d => {
        if (d.month === 0) return 'Hoje';
        const years = Math.floor(d.month / 12);
        const months = d.month % 12;
        if (years === 0) return `${months}m`;
        if (months === 0) return `${years}a`;
        return `${years}a ${months}m`;
    });

    const balanceData = monthlyData.map(d => d.balance);
    const investedData = monthlyData.map(d => d.invested);
    const growthData = monthlyData.map(d => d.growth);

    // Create FIRE goal line
    const fireGoalLine = new Array(monthlyData.length).fill(fireGoal);

    // Destroy previous chart if it exists
    if (fireChart) {
        fireChart.destroy();
    }

    // Create new chart
    fireChart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels,
            datasets: [
                {
                    label: 'Meta FIRE',
                    data: fireGoalLine,
                    borderColor: '#ef4444',
                    backgroundColor: 'rgba(239, 68, 68, 0.1)',
                    borderWidth: 2,
                    borderDash: [10, 5],
                    tension: 0,
                    fill: false,
                    pointRadius: 0
                },
                {
                    label: 'Saldo Total',
                    data: balanceData,
                    borderColor: '#3b82f6',
                    backgroundColor: 'rgba(59, 130, 246, 0.1)',
                    borderWidth: 3,
                    tension: 0.1,
                    fill: true
                },
                {
                    label: 'Total Investido',
                    data: investedData,
                    borderColor: '#10b981',
                    backgroundColor: 'rgba(16, 185, 129, 0.1)',
                    borderWidth: 2,
                    tension: 0.1,
                    fill: true
                },
                {
                    label: 'Crescimento',
                    data: growthData,
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
                        maxTicksLimit: 15
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
                            return 'R$ ' + value.toLocaleString('pt-BR', { maximumFractionDigits: 0 });
                        }
                    }
                }
            }
        }
    });
}

// Clear form and localStorage
function clearForm() {
    document.getElementById('fireTradicionalForm').reset();
    localStorage.removeItem(STORAGE_KEY);
    document.getElementById('calculationResults').style.display = 'none';
}

// Format number as currency
function formatCurrency(value) {
    return new Intl.NumberFormat('pt-BR', {
        style: 'currency',
        currency: 'BRL',
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    }).format(value);
}

// Tab functionality
document.addEventListener('DOMContentLoaded', function() {
    const tabButtons = document.querySelectorAll('.tab-button');
    const tabPanels = document.querySelectorAll('.tab-panel');

    tabButtons.forEach(button => {
        button.addEventListener('click', () => {
            const tabName = button.getAttribute('data-tab');

            // Remove active class from all buttons and panels
            tabButtons.forEach(btn => btn.classList.remove('active'));
            tabPanels.forEach(panel => panel.classList.remove('active'));

            // Add active class to clicked button and corresponding panel
            button.classList.add('active');
            const activePanel = document.getElementById(`tab-${tabName}`);
            if (activePanel) {
                activePanel.classList.add('active');
            }
        });
    });
});
