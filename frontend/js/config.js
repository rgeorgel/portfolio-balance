// API Configuration
const API_BASE_URL = 'http://localhost:5000/api';

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
