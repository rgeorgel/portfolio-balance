// API Configuration for Advisor
const API_BASE_URL = `${window.location.protocol}//${window.location.host}/api`;

// Advisor Authentication helper functions
function getAdvisorToken() {
    return localStorage.getItem('advisorToken');
}

function setAdvisorToken(token) {
    localStorage.setItem('advisorToken', token);
}

function removeAdvisorToken() {
    localStorage.removeItem('advisorToken');
}

function getAdvisorInfo() {
    const advisorInfo = localStorage.getItem('advisorInfo');
    return advisorInfo ? JSON.parse(advisorInfo) : null;
}

function setAdvisorInfo(username, email, fullName) {
    localStorage.setItem('advisorInfo', JSON.stringify({ username, email, fullName }));
}

function removeAdvisorInfo() {
    localStorage.removeItem('advisorInfo');
}

function isAdvisorAuthenticated() {
    return !!getAdvisorToken();
}

function advisorLogout() {
    removeAdvisorToken();
    removeAdvisorInfo();
    removeSelectedClient();
    window.location.href = 'login.html';
}

function checkAdvisorAuth() {
    if (!isAdvisorAuthenticated()) {
        window.location.href = 'login.html';
    }
}

// Client selection functions
function getSelectedClient() {
    const client = localStorage.getItem('selectedClient');
    return client ? JSON.parse(client) : null;
}

function setSelectedClient(client) {
    localStorage.setItem('selectedClient', JSON.stringify(client));
}

function removeSelectedClient() {
    localStorage.removeItem('selectedClient');
}

// Helper function to make authenticated API calls for advisor
async function fetchWithAdvisorAuth(url, options = {}) {
    const token = getAdvisorToken();

    if (!token) {
        window.location.href = 'login.html';
        return;
    }

    const headers = {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`,
        ...options.headers
    };

    const response = await fetch(url, { ...options, headers });

    if (response.status === 401) {
        // Token expired or invalid
        removeAdvisorToken();
        removeAdvisorInfo();
        removeSelectedClient();
        window.location.href = 'login.html';
        return;
    }

    return response;
}

// Helper function to format currency
function formatCurrency(value) {
    return new Intl.NumberFormat('pt-BR', {
        style: 'currency',
        currency: 'BRL'
    }).format(value);
}

// Helper function to format percentage
function formatPercentage(value) {
    return value.toFixed(2) + '%';
}

// Helper function to format date
function formatDate(dateString) {
    const date = new Date(dateString);
    return date.toLocaleDateString('pt-BR');
}

// Helper function to handle API errors
function handleApiError(error) {
    console.error('API Error:', error);
    return 'Ocorreu um erro ao processar a requisição. Por favor, tente novamente.';
}
