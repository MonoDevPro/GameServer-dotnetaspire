# Configuração do GitHub OAuth

Para habilitar a autenticação via GitHub no GameServer.AuthServer, siga os passos abaixo:

## 1. Criar um GitHub OAuth App

1. Acesse [GitHub Developer Settings](https://github.com/settings/developers)
2. Clique em "New OAuth App"
3. Preencha os dados:
   - **Application name**: `GameServer AuthServer`
   - **Homepage URL**: `https://localhost:5001` (ou sua URL de produção)
   - **Authorization callback URL**: `https://localhost:5001/signin-github`

## 2. Configurar as Credenciais

Após criar o OAuth App, você receberá:
- **Client ID**
- **Client Secret**

### Para Desenvolvimento Local:
Edite o arquivo `appsettings.Development.json`:

```json
{
  "Authentication": {
    "GitHub": {
      "ClientId": "seu_github_client_id_aqui",
      "ClientSecret": "seu_github_client_secret_aqui"
    }
  }
}
```

### Para Produção:
Configure as variáveis de ambiente ou use o Azure Key Vault:

```bash
export Authentication__GitHub__ClientId="seu_client_id"
export Authentication__GitHub__ClientSecret="seu_client_secret"
```

## 3. URLs de Callback

O AuthServer está configurado para receber callbacks do GitHub em:
- **Desenvolvimento**: `https://localhost:5001/signin-github`
- **Produção**: `https://seu-dominio.com/signin-github`

## 4. Testando a Integração

1. Execute o projeto: `dotnet run --project GameServer.AppHost`
2. Acesse a página de Login
3. Você verá o botão "Entrar com GitHub"
4. Clique e autorize a aplicação no GitHub
5. Será redirecionado de volta e logado automaticamente

## 5. Funcionalidades Implementadas

✅ **Login com GitHub**: Usuários podem entrar usando suas contas GitHub
✅ **Registro automático**: Novos usuários são criados automaticamente
✅ **Claims do GitHub**: Informações como login, avatar e URL são salvas
✅ **Visual moderno**: Botões GitHub com ícones Bootstrap
✅ **Fluxo seguro**: Gerenciamento completo de tokens e callbacks

## 6. Dados Coletados do GitHub

Durante a autenticação, coletamos:
- **Email**: Para identificação do usuário
- **Nome de usuário**: Login do GitHub
- **Avatar**: URL da foto de perfil
- **URL do perfil**: Link para o perfil público

## 7. Problemas Comuns

### Erro de Callback URL
Se você receber erro de callback, verifique se:
- A URL no GitHub OAuth App está correta
- A porta da aplicação está correta
- O protocolo (http/https) está correto

### Erro de Credenciais
Se o botão GitHub não aparecer:
- Verifique se ClientId e ClientSecret estão configurados
- Confirme que não há espaços extras nas configurações
- Verifique os logs da aplicação para erros

### Primeiro Login
No primeiro login via GitHub:
- Um novo usuário será criado automaticamente
- O email do GitHub será usado como identificador
- Claims específicas do GitHub serão adicionadas ao perfil
