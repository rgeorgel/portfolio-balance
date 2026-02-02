// Advisor Login Form Handler
document.addEventListener('DOMContentLoaded', function() {
    const loginForm = document.getElementById('advisorLoginForm');
    const registerForm = document.getElementById('advisorRegisterForm');
    const errorMessage = document.getElementById('errorMessage');

    // Check if already authenticated
    if (isAdvisorAuthenticated()) {
        window.location.href = 'index.html';
        return;
    }

    if (loginForm) {
        loginForm.addEventListener('submit', async function(e) {
            e.preventDefault();
            errorMessage.style.display = 'none';

            const usernameOrEmail = document.getElementById('usernameOrEmail').value;
            const password = document.getElementById('password').value;

            try {
                const response = await fetch(`${API_BASE_URL}/advisor/auth/login`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({ usernameOrEmail, password })
                });

                const data = await response.json();

                if (!response.ok) {
                    throw new Error(data.message || 'Falha no login');
                }

                // Store token and advisor info
                setAdvisorToken(data.token);
                setAdvisorInfo(data.username, data.email, data.fullName);

                // Redirect to dashboard
                window.location.href = 'index.html';
            } catch (error) {
                errorMessage.textContent = error.message;
                errorMessage.style.display = 'block';
            }
        });
    }

    if (registerForm) {
        registerForm.addEventListener('submit', async function(e) {
            e.preventDefault();
            errorMessage.style.display = 'none';

            const username = document.getElementById('username').value;
            const email = document.getElementById('email').value;
            const fullName = document.getElementById('fullName').value;
            const password = document.getElementById('password').value;
            const confirmPassword = document.getElementById('confirmPassword').value;

            if (password !== confirmPassword) {
                errorMessage.textContent = 'As senhas não coincidem';
                errorMessage.style.display = 'block';
                return;
            }

            try {
                const response = await fetch(`${API_BASE_URL}/advisor/auth/register`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({ username, email, fullName, password })
                });

                const data = await response.json();

                if (!response.ok) {
                    throw new Error(data.message || 'Falha no registro');
                }

                // Store token and advisor info
                setAdvisorToken(data.token);
                setAdvisorInfo(data.username, data.email, data.fullName);

                // Redirect to dashboard
                window.location.href = 'index.html';
            } catch (error) {
                errorMessage.textContent = error.message;
                errorMessage.style.display = 'block';
            }
        });
    }
});
