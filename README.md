# Portfolio Balance

Uma aplicação de balanceamento de portfólio de investimentos que ajuda você a distribuir seus investimentos de acordo com as alocações percentuais desejadas.

## Características

- **Backend**: C# com ASP.NET Core 8.0
- **Frontend**: HTML, CSS e JavaScript puro
- **Banco de Dados**: PostgreSQL com Entity Framework Core

## Funcionalidades

1. **Configuração de Alocações**: Defina a porcentagem desejada para cada tipo de investimento
2. **Gerenciamento de Investimentos**: Registre e gerencie seus investimentos individuais
3. **Calculadora de Balanceamento**: Calcule como distribuir novos investimentos para manter o balanceamento do portfólio
4. **Dashboard**: Visualize o resumo do seu portfólio

## Tipos de Investimento Pré-configurados

- Ações Nacionais
- Fundos Imobiliários
- Criptomoedas
- Renda Fixa
- Ações Internacionais

## Pré-requisitos

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [PostgreSQL](https://www.postgresql.org/download/) (versão 12 ou superior)
- Um navegador web moderno

## Configuração

### 1. Configurar o Banco de Dados

1. Instale o PostgreSQL
2. Crie um banco de dados chamado `portfoliobalance`:

```sql
CREATE DATABASE portfoliobalance;
```

3. Atualize a string de conexão no arquivo `backend/PortfolioBalance/appsettings.json` se necessário:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=portfoliobalance;Username=postgres;Password=suasenha"
  }
}
```

### 2. Configurar o Backend

1. Navegue até o diretório do backend:

```bash
cd backend/PortfolioBalance
```

2. Restaure as dependências:

```bash
dotnet restore
```

3. Execute as migrações do banco de dados:

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

4. Execute o backend:

```bash
dotnet run
```

O backend estará rodando em `http://localhost:5000`

### 3. Configurar o Frontend

1. Navegue até o diretório frontend:

```bash
cd frontend
```

2. Se necessário, atualize a URL da API no arquivo `js/config.js`:

```javascript
const API_BASE_URL = 'http://localhost:5000/api';
```

3. Abra o arquivo `index.html` em um navegador web, ou use um servidor web local:

**Opção 1: Usar um servidor HTTP simples com Python**
```bash
# Python 3
python -m http.server 8080
```

**Opção 2: Usar Node.js com http-server**
```bash
npx http-server -p 8080
```

Acesse: `http://localhost:8080`

## Uso

### 1. Configurar Alocações

1. Acesse a página "Alocações"
2. Defina a porcentagem desejada para cada tipo de investimento
3. Certifique-se de que o total soma 100%
4. Clique em "Salvar Alocações"

### 2. Registrar Investimentos

1. Acesse a página "Investimentos"
2. Clique em "Adicionar Investimento"
3. Preencha os dados:
   - **Tipo de Investimento**: Categoria do investimento
   - **Nome**: Nome do investimento (ex: "PETR4", "BTCUSD", etc.)
   - **Valor Atual**: Valor atual investido
   - **Peso**: Peso para distribuição dentro do tipo (padrão: 1.0)
4. Clique em "Salvar"

**Sobre o Peso**: O peso determina como os novos investimentos serão distribuídos entre os investimentos do mesmo tipo. Por exemplo:
- Se você tem 2 investimentos com peso 1.0 cada, o valor será dividido igualmente
- Se um tem peso 2.0 e outro 1.0, o primeiro receberá 2/3 do valor

### 3. Calcular Novo Investimento

1. Acesse a página "Calculadora"
2. Informe o valor que deseja investir
3. Clique em "Calcular Distribuição"
4. A aplicação mostrará:
   - Quanto investir em cada tipo
   - Como distribuir entre os investimentos individuais
   - Estado do portfólio antes e depois do investimento

## Estrutura do Projeto

```
portfolio-balance/
├── backend/
│   └── PortfolioBalance/
│       ├── Controllers/          # API Controllers
│       ├── Data/                 # DbContext
│       ├── DTOs/                 # Data Transfer Objects
│       ├── Models/               # Entity Models
│       ├── Services/             # Business Logic
│       ├── Program.cs            # Entry Point
│       └── PortfolioBalance.csproj
├── frontend/
│   ├── css/
│   │   └── styles.css           # Estilos
│   ├── js/
│   │   ├── config.js            # Configurações
│   │   ├── dashboard.js         # Dashboard
│   │   ├── allocations.js       # Alocações
│   │   ├── investments.js       # Investimentos
│   │   └── calculator.js        # Calculadora
│   ├── index.html               # Dashboard
│   ├── allocations.html         # Página de Alocações
│   ├── investments.html         # Página de Investimentos
│   └── calculator.html          # Calculadora
└── README.md
```

## API Endpoints

### Investment Types
- `GET /api/investmenttypes` - Listar tipos de investimento
- `GET /api/investmenttypes/{id}` - Obter tipo específico
- `PUT /api/investmenttypes/{id}/allocation` - Atualizar alocação de um tipo
- `PUT /api/investmenttypes/allocations` - Atualizar todas as alocações

### Investments
- `GET /api/investments` - Listar investimentos
- `GET /api/investments/{id}` - Obter investimento específico
- `POST /api/investments` - Criar investimento
- `PUT /api/investments/{id}` - Atualizar investimento
- `DELETE /api/investments/{id}` - Excluir investimento

### Portfolio
- `POST /api/portfolio/calculate-balance` - Calcular balanceamento

## Tecnologias Utilizadas

### Backend
- ASP.NET Core 8.0
- Entity Framework Core 8.0
- Npgsql (PostgreSQL Provider)
- Swagger/OpenAPI

### Frontend
- HTML5
- CSS3
- Vanilla JavaScript (ES6+)

## Solução de Problemas

### Erro de Conexão com o Banco de Dados
- Verifique se o PostgreSQL está rodando
- Confirme as credenciais na string de conexão
- Certifique-se de que o banco de dados foi criado

### CORS Error no Frontend
- Verifique se o backend está rodando
- Confirme que a URL da API no `config.js` está correta
- O backend já está configurado para aceitar requisições de qualquer origem em desenvolvimento

### Migrações do Entity Framework
Se você precisar criar novas migrações:

```bash
# Criar migração
dotnet ef migrations add NomeDaMigracao

# Aplicar migrações
dotnet ef database update

# Reverter última migração
dotnet ef migrations remove
```

## Melhorias Futuras

- Autenticação e autorização de usuários
- Gráficos visuais do portfólio
- Histórico de investimentos
- Exportação de relatórios
- Integração com APIs de cotações
- Aplicativo mobile

## Licença

Este projeto está sob a licença MIT.

## Contribuindo

Contribuições são bem-vindas! Sinta-se à vontade para abrir issues ou pull requests.
