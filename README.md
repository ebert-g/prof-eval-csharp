# 🎓 Avaliação UCSAL - Sistema de Avaliação de Professores

Sistema para avaliação acadêmica de docentes da Universidade Católica do Salvador (UCSAL). O projeto foi desenvolvido com uma arquitetura moderna e unificada utilizando **C# (.NET 8)**, **Entity Framework Core (PostgreSQL)** e uma interface de página única (SPA) responsiva integrada.

---

## 🚀 Funcionalidades Principais

*   **SPA (Single Page Application)**: Interface rápida e dinâmica integrada diretamente no servidor .NET, sem necessidade de dependências pesadas de frontend.
*   **Design Institucional UCSAL**: Layout acadêmico limpo seguindo o padrão cromático institucional (tons de azul corporativo, branco e bordas em cinza suave).
*   **Avaliações Anônimas**: Arquitetura projetada para desvincular estritamente a identidade do estudante do voto gravado na base de dados (utilizando tokens hash SHA-256 unidirecionais).
*   **Autenticação por E-mail (OTP)**: Login simplificado e sem senha, operando exclusivamente com e-mails institucionais do domínio `@ucsal.edu.br` e verificação via código de acesso descartável.
*   **Histórico de Comentários**: Seção dedicada para visualização de feedbacks enviados por outros estudantes para o docente selecionado.
*   **Médias Dinâmicas**: Notas e médias de estrelas atualizadas em tempo real na barra lateral e na visualização central do perfil.

---

## 🛠️ Tecnologias Utilizadas

### Backend
*   **ASP.NET Core 8.0 Web API**
*   **Entity Framework Core 8.0**
*   **PostgreSQL** (via Docker)
*   **Npgsql.EntityFrameworkCore.PostgreSQL**

### Frontend
*   **HTML5 & CSS3** (Vanilla CSS com Design System baseado em variáveis)
*   **JavaScript (ES6)** (Component Pattern assíncrono para consumo de APIs REST)
*   **Razor Pages** (Pre-rendering inicial de templates no servidor)

---

## 📦 Estrutura do Projeto

```text
prof-eval-csharp/
├── ProfEval.Api/                # Projeto Principal (.NET 8 Web SDK)
│   ├── Controllers/             # Endpoints REST (Auth, Professors, Evaluations)
│   ├── Domain/                  # Entidades de Domínio e Contratos
│   │   └── Entities/            # Modelagem (Student, Professor, Evaluation)
│   ├── Infrastructure/          # Persistência e Acesso a Dados
│   │   ├── Persistence/         # DbContext do Entity Framework Core
│   │   └── Services/            # Serviços de Infraestrutura (E-mail SMTP)
│   ├── Pages/                   # Razor Pages (Template SPA Principal)
│   └── wwwroot/                 # Recursos Estáticos (Vanilla CSS, JS e Componentes)
├── docker-compose.yml           # Configuração do PostgreSQL em container
├── prof-eval-csharp.sln         # Arquivo de Solução do Visual Studio
└── README.md                    # Documentação do projeto
```

---

## ⚡ Como Executar o Projeto

Siga as instruções abaixo para configurar e rodar a aplicação localmente:

### Pré-requisitos
*   [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) instalado.
*   [Docker](https://www.docker.com/) e Docker Compose instalados.

### 1. Inicializar o Banco de Dados (PostgreSQL)
Inicialize o container do PostgreSQL a partir da raiz do projeto executando:
```bash
docker compose up -d
```

### 2. Executar as Migrações do EF Core
Para criar e atualizar as tabelas do banco de dados, certifique-se de instalar as ferramentas de CLI do EF e execute:
```bash
cd ProfEval.Api
dotnet ef database update
```

### 3. Iniciar o Servidor C#
Execute a aplicação a partir do diretório raiz ou da pasta `ProfEval.Api`:
```bash
dotnet run --project ProfEval.Api
```

A aplicação estará disponível nos endereços:
*   **Portal da SPA (Interface Web)**: [http://localhost:5172](http://localhost:5172)
*   **Documentação Swagger (API)**: [https://localhost:7284/swagger](https://localhost:7284/swagger) (se configurado HTTPS)

---

## 📄 Licença

Este projeto é desenvolvido para fins de avaliação institucional da UCSAL.
