# 👨‍🏫 Avaliação UCSAL - Sistema de Avaliação de Professores

Sistema de avaliação de professores da UCSAL migrado e integrado em **C# (.NET 8)** com **Entity Framework Core (PostgreSQL)** e interface web responsiva integrada (Razor Pages).

---

## 🏗️ Arquitetura Unificada

O frontend e o backend agora são executados a partir de um único processo do .NET. O backend fornece tanto os endpoints de API REST quanto a página web de visualização:

```
prof-eval-csharp/
├── ProfEval.Api/                # Projeto Único (.NET 8 Web SDK)
│   ├── Controllers/             # Controladores da API REST
│   ├── Models/                  # Entidades (Student, Professor, Evaluation, VerificationCode)
│   ├── Data/                    # DbContext do Entity Framework Core
│   ├── Repositories/            # Implementação do Padrão Repository
│   ├── Services/                # Serviços de Autenticação (AuthService)
│   ├── Pages/                   # Páginas Razor (C# HTML Pre-renderizado)
│   │   ├── Index.cshtml         # Página principal da aplicação
│   │   └── Index.cshtml.cs      # Modelo de dados da página Index (C#)
│   ├── wwwroot/                 # Arquivos estáticos do Frontend
│   │   ├── css/styles.css       # Estilos da página
│   │   └── js/                  # Orquestrador, componentes e serviços de API
│   ├── Program.cs               # Configuração e inicialização do servidor C#
│   └── appsettings.json         # Configuração de conexões e banco de dados
├── docker-compose.yml           # Inicialização do banco PostgreSQL local
└── README.md                    # Este arquivo (Instruções)
```

---

## 🚀 Como Executar o Projeto

Siga estes passos simples para rodar a aplicação unificada localmente:

### 1. Inicializar o Banco de Dados (PostgreSQL)
Certifique-se de que possui o Docker instalado e execute na raiz do projeto:
```bash
docker compose up -d
```
Isso iniciará o container do PostgreSQL na porta `5432` com as configurações necessárias.

### 2. Atualizar as Migrations (EF Core)
Crie e atualize as tabelas no banco de dados executando o seguinte a partir da pasta raiz:
```bash
cd ProfEval.Api
dotnet ef database update
```

### 3. Executar o Projeto C#
Inicie o servidor do backend que servirá a API e a página web (a partir do diretório raiz ou da pasta `ProfEval.Api`):
```bash
dotnet run --project ProfEval.Api
```

A aplicação estará disponível nos endereços:
- **Página Web Integrada (Frontend)**: [http://localhost:5172](http://localhost:5172) ou [https://localhost:7284](https://localhost:7284)
- **Documentação Interativa (Swagger)**: [https://localhost:7284/swagger](https://localhost:7284/swagger)

---

## 🔑 Fluxo de Autenticação

Para realizar avaliações, o usuário precisa estar logado com um e-mail institucional da UCSAL:

1. Acesse [http://localhost:5172](http://localhost:5172) e clique em **Login** ou **Acessar**.
2. Insira apenas seu e-mail do domínio `@ucsal.edu.br` (ex: `aluno.teste@ucsal.edu.br`) e clique em **Solicitar Código**.
3. **Obtenção do Código (OTP)**:
   * **Desenvolvimento**: O código gerado de 6 dígitos é exibido diretamente na interface (abaixo do campo de digitação), no console do desenvolvedor do navegador (F12) e nos logs do terminal da API.
   * **Produção (SMTP)**: O código é disparado diretamente para a caixa de entrada do e-mail informado (caso as configurações SMTP estejam preenchidas).
4. Digite o código recebido de 6 dígitos e clique em **Verificar**.

---

## ✉️ Configuração de Envio de E-mails (SMTP)

No arquivo `appsettings.json`, você pode configurar as credenciais do servidor SMTP para enviar e-mails de acesso reais:

```json
  "SmtpSettings": {
    "Server": "smtp.provedor.com",
    "Port": 587,
    "SenderName": "Avaliação UCSAL",
    "SenderEmail": "noreply@ucsal.edu.br",
    "Username": "seu_usuario",
    "Password": "sua_password",
    "EnableSsl": true
  }
```
*Se a chave `"Server"` for deixada em branco, o sistema automaticamente operará em modo **Mock** e exibirá o código nos logs do console (ideal para desenvolvimento).*

---

## 🛡️ Arquitetura de Anonimato

O processo de voto foi projetado para ser **estritamente anônimo**:
* A coluna `student_id` na tabela `evaluations` foi marcada como `nullable` e é mantida como `NULL` no banco de dados.
* A correspondência de voto único por aluno/professor é mantida por meio de um hash determinístico `SHA-256` (`anonymous_token`).
* Não há rastreabilidade reversa das avaliações no banco de dados.

---

## 📦 Preparando e Subindo para o GitHub

Para subir o projeto no GitHub de forma limpa e segura, siga estes passos:

### 1. Verificar Arquivos Ignorados
O projeto já conta com um arquivo `.gitignore` configurado na raiz para evitar que arquivos de compilação (`bin/`, `obj/`), segredos locais (`.suo`, `*.user`), bancos SQLite de desenvolvimento e pastas de cache (`.gemini/`) sejam publicados.

### 2. Inicializar o Repositório Git
Na raiz do projeto, execute no terminal:
```bash
git init
git add .
git commit -m "feat: refactor frontend/backend with UCSAL standard and anonymous evaluations"
```

### 3. Vincular ao Repositório do GitHub
Crie um repositório vazio no GitHub (sem inicializar com README ou .gitignore) e execute:
```bash
git branch -M main
git remote add origin https://github.com/seu-usuario/seu-repositorio.git
git push -u origin main
```
