// Check if already authenticated (for login/register pages)
if (window.location.pathname.includes('login.html') || window.location.pathname.includes('register.html')) {
    if (isAuthenticated()) {
        window.location.href = 'index.html';
    }
}

// Login form handler
const loginForm = document.getElementById('loginForm');
if (loginForm) {
    loginForm.addEventListener('submit', async (e) => {
        e.preventDefault();

        const usernameOrEmail = document.getElementById('usernameOrEmail').value;
        const password = document.getElementById('password').value;
        const errorMessage = document.getElementById('errorMessage');

        try {
            const response = await fetch(`${API_BASE_URL}/auth/login`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ usernameOrEmail, password })
            });

            if (!response.ok) {
                const error = await response.json();
                errorMessage.textContent = error.message || 'Usuário/email ou senha inválidos';
                errorMessage.style.display = 'block';
                return;
            }

            const data = await response.json();
            setAuthToken(data.token);
            setUserInfo(data.username, data.email);
            window.location.href = 'index.html';

        } catch (error) {
            console.error('Login error:', error);
            errorMessage.textContent = 'Erro ao fazer login. Por favor, tente novamente.';
            errorMessage.style.display = 'block';
        }
    });
}

// Register form handler
const registerForm = document.getElementById('registerForm');
if (registerForm) {
    registerForm.addEventListener('submit', async (e) => {
        e.preventDefault();

        const username = document.getElementById('username').value;
        const email = document.getElementById('email').value;
        const password = document.getElementById('password').value;
        const confirmPassword = document.getElementById('confirmPassword').value;
        const errorMessage = document.getElementById('errorMessage');
        const successMessage = document.getElementById('successMessage');

        // Hide previous messages
        errorMessage.style.display = 'none';
        successMessage.style.display = 'none';

        // Validate password match
        if (password !== confirmPassword) {
            errorMessage.textContent = 'As senhas não coincidem';
            errorMessage.style.display = 'block';
            return;
        }

        try {
            const response = await fetch(`${API_BASE_URL}/auth/register`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ username, email, password })
            });

            if (!response.ok) {
                const error = await response.json();
                errorMessage.textContent = error.message || 'Erro ao registrar usuário';
                errorMessage.style.display = 'block';
                return;
            }

            const data = await response.json();
            setAuthToken(data.token);
            setUserInfo(data.username, data.email);

            successMessage.textContent = 'Registro realizado com sucesso! Redirecionando...';
            successMessage.style.display = 'block';

            setTimeout(() => {
                window.location.href = 'index.html';
            }, 1500);

        } catch (error) {
            console.error('Register error:', error);
            errorMessage.textContent = 'Erro ao registrar. Por favor, tente novamente.';
            errorMessage.style.display = 'block';
        }
    });
}
