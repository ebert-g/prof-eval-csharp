# Guia Interno de Desenvolvimento e Testes
## Sistema de Avaliação de Professores - UCSAL

Este documento é privado e contém instruções técnicas sobre o fluxo de desenvolvimento, testes locais de autenticação e detalhes sobre conexões de banco de dados e SMTP.

---

## 🔑 Fluxo de Autenticação para Testes Locais (Sem Servidor SMTP)

Para testar a autenticação de e-mail localmente sem configurar um servidor SMTP real, o sistema opera em modo **Mock/Desenvolvimento**:

1.  Acesse o site em [http://localhost:5172](http://localhost:5172).
2.  Clique em **Login** ou **Acessar**.
3.  Digite qualquer e-mail institucional terminado em `@ucsal.edu.br` (ex: `aluno@ucsal.edu.br`) e clique em **Solicitar Código**.
4.  **Obtenção do Código OTP de Teste**:
    *   **Na Interface**: O código é injetado diretamente na interface de Step 2 no modal de login, logo abaixo do campo de entrada.
    *   **No Console do Navegador**: Abra a ferramenta de desenvolvedor do navegador (F12) e verifique o console. O código estará registrado como:
        `🔐 CÓDIGO DE VERIFICAÇÃO (DEV): XXXXXX`
    *   **Nos Logs do Servidor**: No terminal onde o backend está rodando, verifique o log formatado como:
        `warn: ProfEval.Api.Infrastructure.Services.EmailService[0] [MOCK EMAIL] Para: aluno@ucsal.edu.br | Código OTP Gerado: XXXXXX`
5.  Digite o código de 6 dígitos obtido e clique em **Verificar**.

---

## ✉️ Configuração do Servidor SMTP de Produção

No arquivo `appsettings.json`, preencha as chaves da seção `SmtpSettings` com as credenciais do seu provedor SMTP de envio (ex: SendGrid, Mailgun, Amazon SES ou servidor de e-mail corporativo da UCSAL):

```json
  "SmtpSettings": {
    "Server": "smtp.provedor.com",
    "Port": 587,
    "SenderName": "Avaliação UCSAL",
    "SenderEmail": "noreply@ucsal.edu.br",
    "Username": "seu_usuario_smtp",
    "Password": "sua_senha_smtp",
    "EnableSsl": true
  }
```
*   **Modo de Produção**: O envio de e-mails reais será ativado automaticamente assim que a propriedade `"Server"` for preenchida.
*   **Modo de Desenvolvimento**: Se o `"Server"` for mantido em branco, o serviço entra em modo Mock e apenas loga os códigos OTP gerados.

---

## 🗄️ Credenciais e Conexões do Banco de Dados

O banco de dados PostgreSQL é inicializado localmente via Docker. As credenciais padrões configuradas para ambiente de testes são:
*   **String de Conexão (`appsettings.json`)**:
    `Host=localhost;Port=5432;Database=avaliacao_db;Username=user_ucsal;Password=password123`
*   **Porta Exposta**: `5432`
*   **Nome do Database**: `avaliacao_db`

Para alterar ou fortalecer essas credenciais em ambiente de produção, certifique-se de atualizar tanto o `docker-compose.yml` quanto a conexão correspondente em `appsettings.json`.
