# Portfolio Balance

Uma aplicação de balanceamento de portfólio de investimentos que ajuda você a distribuir seus investimentos de acordo com as alocações percentuais desejadas.

## Características

- **Backend**: C# com ASP.NET Core 8.0
- **Frontend**: HTML, CSS e JavaScript puro
- **Banco de Dados**: PostgreSQL com Entity Framework Core

## Funcionalidades

1. **Autenticação e Autorização**: Sistema completo de registro e login com JWT tokens
2. **Configuração de Alocações**: Defina a porcentagem desejada para cada tipo de investimento (personalizada por usuário)
3. **Gerenciamento de Investimentos**: Registre e gerencie seus investimentos individuais
4. **Calculadora de Balanceamento**: Calcule como distribuir novos investimentos para manter o balanceamento do portfólio
5. **Dashboard**: Visualize o resumo do seu portfólio
6. **Histórico de Investimentos**: Acompanhe a evolução dos valores dos seus investimentos ao longo do tempo com gráficos interativos
7. **Isolamento de Dados**: Cada usuário vê apenas seus próprios investimentos e alocações

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

### 0. Autenticação

#### Primeiro Acesso - Registro

1. Ao acessar `http://localhost:8080`, você será redirecionado para a página de login
2. Clique em "Registre-se" para criar uma nova conta
3. Preencha os dados:
   - **Usuário**: Nome de usuário (mínimo 3 caracteres)
   - **Email**: Endereço de email válido
   - **Senha**: Senha segura (mínimo 6 caracteres)
   - **Confirmar Senha**: Repita a senha
4. Clique em "Registrar"
5. Após o registro, você será automaticamente autenticado e redirecionado para o dashboard
6. As alocações padrão serão criadas automaticamente (20% para cada tipo de investimento)

#### Login

1. Acesse `http://localhost:8080/login.html`
2. Insira seu usuário e senha
3. Clique em "Entrar"
4. Você será redirecionado para o dashboard

#### Logout

- Clique no botão "Sair" no canto superior direito de qualquer página

**Nota**: Todos os seus dados (investimentos e alocações) são privados e isolados. Outros usuários não podem ver ou modificar seus dados.

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

### 4. Visualizar Histórico de Investimentos

1. Acesse a página "Histórico"
2. Escolha entre:
   - **Portfólio Total**: Visualize o valor total do seu portfólio ao longo do tempo
   - **Investimento Individual**: Selecione um investimento específico para ver sua evolução
3. Use filtros de data para visualizar períodos específicos
4. O histórico é registrado automaticamente:
   - Ao criar um novo investimento
   - Sempre que o valor é atualizado
5. Você também pode adicionar entradas manuais de histórico
6. Os gráficos são gerados automaticamente usando Chart.js

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
│   │   ├── config.js            # Configurações e autenticação
│   │   ├── auth.js              # Login e registro
│   │   ├── dashboard.js         # Dashboard
│   │   ├── allocations.js       # Alocações
│   │   ├── investments.js       # Investimentos
│   │   ├── calculator.js        # Calculadora
│   │   └── history.js           # Histórico
│   ├── login.html               # Página de Login
│   ├── register.html            # Página de Registro
│   ├── index.html               # Dashboard
│   ├── allocations.html         # Página de Alocações
│   ├── investments.html         # Página de Investimentos
│   ├── calculator.html          # Calculadora
│   └── history.html             # Histórico
└── README.md
```

## API Endpoints

### Authentication (Não requer autenticação)
- `POST /api/auth/register` - Registrar novo usuário
- `POST /api/auth/login` - Fazer login e obter token JWT

### Investment Types (Requer autenticação)
- `GET /api/investmenttypes` - Listar tipos de investimento com alocações do usuário
- `GET /api/investmenttypes/{id}` - Obter tipo específico com alocação do usuário
- `PUT /api/investmenttypes/{id}/allocation` - Atualizar alocação de um tipo
- `PUT /api/investmenttypes/allocations` - Atualizar todas as alocações do usuário

### Investments (Requer autenticação)
- `GET /api/investments` - Listar investimentos do usuário
- `GET /api/investments/{id}` - Obter investimento específico do usuário
- `POST /api/investments` - Criar investimento
- `PUT /api/investments/{id}` - Atualizar investimento do usuário
- `DELETE /api/investments/{id}` - Excluir investimento do usuário

### Portfolio (Requer autenticação)
- `POST /api/portfolio/calculate-balance` - Calcular balanceamento para o portfólio do usuário

### Investment History (Requer autenticação)
- `GET /api/investmenthistory/investment/{investmentId}` - Obter histórico de um investimento específico
- `GET /api/investmenthistory/portfolio` - Obter histórico agregado do portfólio
- `POST /api/investmenthistory` - Adicionar entrada manual de histórico
- `DELETE /api/investmenthistory/{id}` - Excluir entrada de histórico

### Database Management (Requer autenticação)
- `GET /api/database/migration-status` - Verificar status das migrações do banco de dados
- `POST /api/database/migrate` - Aplicar migrações pendentes manualmente

**Nota**: Todos os endpoints marcados com "Requer autenticação" precisam incluir o header `Authorization: Bearer {token}` nas requisições.

## Tecnologias Utilizadas

### Backend
- ASP.NET Core 8.0
- Entity Framework Core 8.0
- Npgsql (PostgreSQL Provider)
- JWT Bearer Authentication
- Swagger/OpenAPI

### Frontend
- HTML5
- CSS3
- Vanilla JavaScript (ES6+)
- LocalStorage para gerenciamento de tokens

## Solução de Problemas

### Erro de Conexão com o Banco de Dados
- Verifique se o PostgreSQL está rodando
- Confirme as credenciais na string de conexão
- Certifique-se de que o banco de dados foi criado

### CORS Error no Frontend
- Verifique se o backend está rodando
- Confirme que a URL da API no `config.js` está correta
- O backend já está configurado para aceitar requisições de qualquer origem em desenvolvimento

### Erro de Autenticação (401 Unauthorized)
- Certifique-se de que você está logado (verifique se há um token no localStorage)
- O token JWT expira após 7 dias - faça login novamente se necessário
- Limpe o localStorage do navegador e faça login novamente: `localStorage.clear()`

### Redirecionamento constante para login
- Limpe o localStorage do navegador
- Certifique-se de que o backend está rodando e acessível
- Verifique o console do navegador para erros de rede

### Migrações do Entity Framework

#### Migrações Automáticas
Por padrão, a aplicação verifica e aplica migrações automaticamente ao iniciar. Se o banco de dados foi criado sem usar o sistema de migrações, ele será automaticamente recriado com o rastreamento adequado.

Para desabilitar a migração automática no startup, configure `Database:AutoMigrateOnStartup` para `false` no `appsettings.json`:

```json
{
  "Database": {
    "AutoMigrateOnStartup": false
  }
}
```

Isso é útil para ambientes de produção onde você deseja controlar manualmente quando as migrações são aplicadas.

#### Migrações Manuais via API
Você também pode gerenciar migrações manualmente através de endpoints da API:

**1. Verificar status das migrações:**
```bash
curl -X GET http://localhost:5000/api/database/migration-status \
  -H "Authorization: Bearer SEU_TOKEN_JWT"
```

**2. Aplicar migrações pendentes:**
```bash
curl -X POST http://localhost:5000/api/database/migrate \
  -H "Authorization: Bearer SEU_TOKEN_JWT"
```

**Resposta esperada:**
```json
{
  "success": true,
  "message": "Aplicando 1 migração(ões) pendente(s).",
  "action": "Apply migrations",
  "databaseExists": true,
  "appliedMigrations": [],
  "pendingMigrationsBefore": ["20251119031600_InitialCreate"],
  "migrationsApplied": ["20251119031600_InitialCreate"]
}
```

#### Criando Novas Migrações
Se você precisar criar novas migrações durante o desenvolvimento:

```bash
# Criar migração
dotnet ef migrations add NomeDaMigracao

# Aplicar migrações via CLI
dotnet ef database update

# Reverter última migração
dotnet ef migrations remove
```

**Nota**: Os endpoints de migração requerem autenticação. Em produção, considere adicionar autorização baseada em roles (admin apenas).

## Segurança

A aplicação implementa as seguintes medidas de segurança:

- **Autenticação JWT**: Tokens seguros com expiração de 7 dias
- **Hash de Senhas**: Senhas são armazenadas usando SHA-256
- **Isolamento de Dados**: Cada usuário tem acesso apenas aos seus próprios dados
- **Validação de Entrada**: Validação de dados no backend usando Data Annotations
- **HTTPS Recomendado**: Em produção, sempre use HTTPS para proteger os tokens

**Nota de Segurança**: A chave secreta JWT está no arquivo `appsettings.json` para desenvolvimento. Em produção, use variáveis de ambiente ou Azure Key Vault para armazenar secrets.

## Melhorias Futuras

- Exportação de relatórios
- Integração com APIs de cotações
- Aplicativo mobile
- Autenticação de dois fatores (2FA)
- Recuperação de senha por email
- Notificações de mudanças significativas no portfólio
- Análise de rentabilidade e ROI

## Licença

Este projeto está sob a licença MIT.

## Contribuindo

Contribuições são bem-vindas! Sinta-se à vontade para abrir issues ou pull requests.
