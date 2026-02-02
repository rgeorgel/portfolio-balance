// Advisor Dashboard Script
let clients = [];

document.addEventListener('DOMContentLoaded', function() {
    // Check authentication
    checkAdvisorAuth();

    // Display advisor name
    const advisorInfo = getAdvisorInfo();
    if (advisorInfo) {
        const nameDisplay = advisorInfo.fullName || advisorInfo.username;
        document.getElementById('advisorName').textContent = nameDisplay;
    }

    // Load clients
    loadClients();

    // Setup add client form
    const addClientForm = document.getElementById('addClientForm');
    addClientForm.addEventListener('submit', handleAddClient);
});

async function loadClients() {
    const container = document.getElementById('clientsContainer');

    try {
        const response = await fetchWithAdvisorAuth(`${API_BASE_URL}/advisor/clients`);

        if (!response || !response.ok) {
            throw new Error('Falha ao carregar clientes');
        }

        clients = await response.json();
        renderClients();
    } catch (error) {
        container.innerHTML = `
            <div class="empty-state">
                <svg viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                    <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-2h2v2zm0-4h-2V7h2v6z"/>
                </svg>
                <h3>Erro ao carregar clientes</h3>
                <p>Por favor, tente novamente mais tarde.</p>
            </div>
        `;
    }
}

function renderClients() {
    const container = document.getElementById('clientsContainer');

    if (clients.length === 0) {
        container.innerHTML = `
            <div class="empty-state">
                <svg viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                    <path d="M16 11c1.66 0 2.99-1.34 2.99-3S17.66 5 16 5c-1.66 0-3 1.34-3 3s1.34 3 3 3zm-8 0c1.66 0 2.99-1.34 2.99-3S9.66 5 8 5C6.34 5 5 6.34 5 8s1.34 3 3 3zm0 2c-2.33 0-7 1.17-7 3.5V19h14v-2.5c0-2.33-4.67-3.5-7-3.5zm8 0c-.29 0-.62.02-.97.05 1.16.84 1.97 1.97 1.97 3.45V19h6v-2.5c0-2.33-4.67-3.5-7-3.5z"/>
                </svg>
                <h3>Nenhum cliente cadastrado</h3>
                <p>Adicione clientes usando o usuario ou email deles.</p>
            </div>
        `;
        container.className = '';
        return;
    }

    container.className = 'clients-grid';
    container.innerHTML = clients.map(client => `
        <div class="client-card" onclick="viewClient(${client.userId})">
            <div class="client-header">
                <div>
                    <div class="client-name">${escapeHtml(client.username)}</div>
                    <div class="client-email">${escapeHtml(client.email)}</div>
                </div>
                <button class="btn-remove" onclick="event.stopPropagation(); removeClient(${client.userId}, '${escapeHtml(client.username)}')">
                    Remover
                </button>
            </div>
            <div class="client-stats">
                <div class="stat-item">
                    <div class="stat-value">${formatCurrency(client.totalPortfolioValue)}</div>
                    <div class="stat-label">Valor Total</div>
                </div>
                <div class="stat-item">
                    <div class="stat-value">${client.totalInvestments}</div>
                    <div class="stat-label">Investimentos</div>
                </div>
            </div>
        </div>
    `).join('');
}

async function handleAddClient(e) {
    e.preventDefault();

    const errorMessage = document.getElementById('errorMessage');
    const successMessage = document.getElementById('successMessage');
    const usernameOrEmail = document.getElementById('clientUsernameOrEmail').value.trim();

    errorMessage.style.display = 'none';
    successMessage.style.display = 'none';

    if (!usernameOrEmail) {
        errorMessage.textContent = 'Por favor, informe o usuario ou email do cliente.';
        errorMessage.style.display = 'block';
        return;
    }

    try {
        const response = await fetchWithAdvisorAuth(`${API_BASE_URL}/advisor/clients`, {
            method: 'POST',
            body: JSON.stringify({ usernameOrEmail })
        });

        const data = await response.json();

        if (!response.ok) {
            throw new Error(data.message || 'Falha ao adicionar cliente');
        }

        successMessage.textContent = `Cliente "${data.username}" adicionado com sucesso!`;
        successMessage.style.display = 'block';
        document.getElementById('clientUsernameOrEmail').value = '';

        // Reload clients
        await loadClients();
    } catch (error) {
        errorMessage.textContent = error.message;
        errorMessage.style.display = 'block';
    }
}

async function removeClient(userId, username) {
    if (!confirm(`Tem certeza que deseja remover o cliente "${username}"?`)) {
        return;
    }

    try {
        const response = await fetchWithAdvisorAuth(`${API_BASE_URL}/advisor/clients/${userId}`, {
            method: 'DELETE'
        });

        if (!response.ok) {
            throw new Error('Falha ao remover cliente');
        }

        // Reload clients
        await loadClients();
    } catch (error) {
        alert('Erro ao remover cliente: ' + error.message);
    }
}

function viewClient(userId) {
    const client = clients.find(c => c.userId === userId);
    if (client) {
        setSelectedClient(client);
        window.location.href = `client.html?id=${userId}`;
    }
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}
