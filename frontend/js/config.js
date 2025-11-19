// API Configuration
// When running with docker-compose, both frontend (port 8080) and backend (port 5000)
// are accessible from localhost on the host machine
const API_BASE_URL = window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1'
    ? 'http://localhost:5000/api'
    : `http://${window.location.hostname}:5000/api`;

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
