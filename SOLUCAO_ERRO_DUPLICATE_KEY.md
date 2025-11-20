# Solução para Erro de Duplicate Key no InvestmentHistories

## Problema

Você encontrou o seguinte erro ao atualizar investimentos:

```
23505: duplicate key value violates unique constraint "PK_InvestmentHistories"
```

## Causa

Este erro ocorre quando a sequência do PostgreSQL (que gera os IDs automaticamente) fica dessincronizada com os dados existentes na tabela. Isso pode acontecer quando:

- Dados são inseridos manualmente com IDs específicos
- A sequência não foi resetada após importar dados
- Houve algum problema durante migrações anteriores

## Solução

Existem duas formas de corrigir este problema:

### Opção 1: Usando a Migration Automática (Recomendado)

A migration `20251120000001_FixInvestmentHistorySequence.cs` foi criada para corrigir automaticamente este problema.

**Como aplicar:**

1. Reinicie a aplicação backend. Se `AutoMigrateOnStartup` estiver habilitado no `appsettings.json`, a migration será aplicada automaticamente.

2. Ou use o endpoint da API:
   ```bash
   POST http://localhost:5500/api/database/migrate
   ```

   Você pode fazer isso via Swagger em: `http://localhost:5500/swagger`

### Opção 2: Usando o Endpoint de Fix Manual

Foi adicionado um novo endpoint específico para corrigir sequências:

**Endpoint:** `POST /api/database/fix-sequences`

**Como usar:**

1. Acesse o Swagger: `http://localhost:5500/swagger`
2. Procure pelo endpoint `POST /api/database/fix-sequences`
3. Clique em "Try it out" e depois em "Execute"

**Ou via curl:**
```bash
curl -X POST http://localhost:5500/api/database/fix-sequences
```

**Resposta esperada:**
```json
{
  "success": true,
  "message": "Successfully fixed 4 sequences",
  "sequencesFixed": [
    "InvestmentHistories",
    "Investments",
    "Users",
    "UserInvestmentTypeAllocations"
  ]
}
```

## O que a correção faz?

A correção executa o seguinte comando SQL para cada tabela:

```sql
SELECT setval(
    pg_get_serial_sequence('"InvestmentHistories"', 'Id'),
    COALESCE((SELECT MAX("Id") FROM "InvestmentHistories"), 0) + 1,
    false
);
```

Isso redefine a sequência para o próximo valor disponível baseado no maior ID existente na tabela, garantindo que não haverá conflitos.

## Prevenção

Para evitar que este problema ocorra novamente:

1. **Sempre use a API** para criar e atualizar investimentos - nunca insira dados diretamente no banco de dados
2. **Mantenha as migrations atualizadas** - sempre aplique novas migrations quando disponíveis
3. **Use o sistema de histórico corretamente** - conforme explicado nas instruções de uso

## Lançamento de Futuros Investimentos

Agora que o erro está corrigido, você pode lançar seus investimentos corretamente:

### Exemplo 1: Comprando mais ações (AUVP11)
- Atualize o investimento existente
- **Quantity**: 100 (total após a compra)
- **UnitValue**: Preço atual de mercado (ex: 110.40)
- **CurrentValue**: Quantity × UnitValue (ex: 11.040,00)

### Exemplo 2: Rendimento de Renda Fixa
- Atualize o investimento existente
- **CurrentValue**: Novo valor total (ex: 10.200,00)
- **UnitValue**: Deixe em branco ou null
- **Quantity**: Deixe em branco ou null

O sistema registrará automaticamente no histórico cada vez que você atualizar os valores!

## Precisa de Ajuda?

Se o problema persistir após aplicar as correções acima, verifique:

1. Os logs do backend para detalhes adicionais
2. Se o PostgreSQL está rodando e acessível
3. Se a string de conexão está correta no `appsettings.json`
