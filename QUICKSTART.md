# Quick Start Guide

## 🚀 Opção 1: Docker Compose - Aplicação Completa (Recomendado)

Inicie **toda a aplicação** (PostgreSQL + Backend + Frontend) com um único comando:

```bash
docker compose up -d
```

Acesse a aplicação:
- **Frontend**: http://localhost:8080
- **Backend API**: http://localhost:5000
- **Swagger**: http://localhost:5000/swagger

Para mais detalhes sobre Docker, veja [DOCKER.md](DOCKER.md)

Para parar os serviços:
```bash
docker compose down
```

---

## Opção 2: Docker apenas para PostgreSQL

### 1. Iniciar o PostgreSQL com Docker

```bash
docker compose up -d postgres
```

Isso iniciará apenas o container PostgreSQL na porta 5432.

### 2. Configurar e Rodar o Backend

```bash
cd backend/PortfolioBalance

# Restaurar dependências
dotnet restore

# Criar e aplicar migrações
dotnet ef migrations add InitialCreate
dotnet ef database update

# Rodar o backend
dotnet run
```

O backend estará disponível em: `http://localhost:5000`
Swagger UI disponível em: `http://localhost:5000/swagger`

### 3. Rodar o Frontend

Em outro terminal:

```bash
cd frontend

# Opção A: Python 3
python -m http.server 8080

# Opção B: Python 2
python -m SimpleHTTPServer 8080

# Opção C: Node.js
npx http-server -p 8080

# Opção D: PHP
php -S localhost:8080
```

Acesse: `http://localhost:8080`

---

## Opção 3: PostgreSQL Local

### 1. Instalar PostgreSQL

Baixe e instale o PostgreSQL: https://www.postgresql.org/download/

### 2. Criar o Banco de Dados

```bash
psql -U postgres
CREATE DATABASE portfoliobalance;
\q
```

### 3. Configurar a String de Conexão

Edite `backend/PortfolioBalance/appsettings.json` e atualize a senha:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=portfoliobalance;Username=postgres;Password=SUA_SENHA_AQUI"
  }
}
```

### 4. Seguir passos 2 e 3 da Opção 1

## Primeiro Uso

1. Acesse o Dashboard (`http://localhost:8080`)
2. Vá para "Alocações" e configure as porcentagens (ex: 20% para cada tipo)
3. Vá para "Investimentos" e adicione seus investimentos
4. Use a "Calculadora" para ver como distribuir novos investimentos

## Comandos Úteis

### Entity Framework

```bash
# Criar nova migração
dotnet ef migrations add NomeDaMigracao

# Aplicar migrações
dotnet ef database update

# Remover última migração
dotnet ef migrations remove

# Ver SQL que será executado
dotnet ef migrations script
```

### Docker

```bash
# Iniciar PostgreSQL
docker-compose up -d

# Parar PostgreSQL
docker-compose down

# Ver logs
docker-compose logs -f

# Parar e remover volumes (APAGA DADOS!)
docker-compose down -v
```

## Solução Rápida de Problemas

### Backend não conecta ao banco
```bash
# Verificar se PostgreSQL está rodando
docker-compose ps
# ou
pg_isctl status
```

### Erro de migração
```bash
# Resetar banco (APAGA DADOS!)
dotnet ef database drop
dotnet ef database update
```

### CORS error no frontend
- Certifique-se de que o backend está rodando
- Verifique `frontend/js/config.js` se a URL está correta
- Não abra o HTML direto do sistema de arquivos, use um servidor HTTP

## Dados de Teste

Após configurar, você pode adicionar alguns investimentos de exemplo:

**Ações Nacionais:**
- PETR4 - R$ 5.000
- VALE3 - R$ 3.000

**Fundos Imobiliários:**
- HGLG11 - R$ 2.500
- MXRF11 - R$ 2.500

**Renda Fixa:**
- Tesouro Selic 2029 - R$ 10.000

Depois use a calculadora com um valor como R$ 1.000 para ver a distribuição!
