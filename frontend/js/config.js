// API Configuration
// When running with docker-compose, both frontend (port 8080) and backend (port 5500)
// are accessible from localhost on the host machine
const API_BASE_URL = window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1'
    ? 'http://localhost:5500/api'
    : `http://${window.location.hostname}:5500/api`;

// Authentication helper functions
function getAuthToken() {
    return localStorage.getItem('authToken');
}

function setAuthToken(token) {
    localStorage.setItem('authToken', token);
}

function removeAuthToken() {
    localStorage.removeItem('authToken');
}

function getUserInfo() {
    const userInfo = localStorage.getItem('userInfo');
    return userInfo ? JSON.parse(userInfo) : null;
}

function setUserInfo(username, email) {
    localStorage.setItem('userInfo', JSON.stringify({ username, email }));
}

function removeUserInfo() {
    localStorage.removeItem('userInfo');
}

function isAuthenticated() {
    return !!getAuthToken();
}

function logout() {
    removeAuthToken();
    removeUserInfo();
    window.location.href = 'login.html';
}

function checkAuth() {
    if (!isAuthenticated()) {
        window.location.href = 'login.html';
    }
}

// Helper function to make authenticated API calls
async function fetchWithAuth(url, options = {}) {
    const token = getAuthToken();

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
        removeAuthToken();
        removeUserInfo();
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

// Helper function to handle API errors
function handleApiError(error) {
    console.error('API Error:', error);
    return 'Ocorreu um erro ao processar a requisição. Por favor, tente novamente.';
}
