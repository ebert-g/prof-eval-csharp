/**
 * Auth Component - UCSAL Passwordless Login with Verification Code
 * Gerencia a autenticação por e-mail institucional e verificação de código OTP
 */

class AuthComponent {
  constructor() {
    this.currentStudent = null;
    this.pendingEmail = null;
    this.init();
  }

  init() {
    this.loadStoredStudent();
    this.updateAuthUI();
  }

  loadStoredStudent() {
    const stored = localStorage.getItem("currentStudent");
    if (stored) {
      this.currentStudent = JSON.parse(stored);
    }
  }

  showLoginModal() {
    const modal = document.getElementById("loginModal");
    if (modal) {
      modal.style.display = "flex";
      this.showEmailStep();
    }
  }

  hideLoginModal() {
    const modal = document.getElementById("loginModal");
    if (modal) {
      modal.style.display = "none";
    }
  }

  showEmailStep() {
    const emailStep = document.getElementById("loginEmailStep");
    const codeStep = document.getElementById("loginCodeStep");
    if (emailStep && codeStep) {
      emailStep.style.display = "block";
      codeStep.style.display = "none";
    }
    const emailInput = document.getElementById("studentEmail");
    if (emailInput) {
      emailInput.value = this.pendingEmail || "";
      emailInput.focus();
    }
  }

  showCodeStep() {
    const emailStep = document.getElementById("loginEmailStep");
    const codeStep = document.getElementById("loginCodeStep");
    const emailPreview = document.getElementById("emailPreview");
    if (emailStep && codeStep) {
      emailStep.style.display = "none";
      codeStep.style.display = "block";
    }
    if (emailPreview) {
      emailPreview.textContent = this.pendingEmail;
    }
    const codeInput = document.getElementById("verificationCode");
    if (codeInput) {
      codeInput.value = "";
      codeInput.focus();
    }
  }

  deriveNameFromEmail(email) {
    const prefix = email.split('@')[0];
    const parts = prefix.split(/[\._\-]/).filter(p => p.length > 0);
    return parts.map(p => p.charAt(0).toUpperCase() + p.slice(1).toLowerCase()).join(" ");
  }

  async handleRequestCode(event) {
    if (event) event.preventDefault();
    const email = document.getElementById("studentEmail")?.value.trim();

    if (!email) {
      showError("Por favor, informe seu e-mail.");
      return;
    }

    if (!email.toLowerCase().endsWith("@ucsal.edu.br")) {
      showError("Acesso exclusivo para e-mails institucionais (@ucsal.edu.br).");
      return;
    }

    const derivedName = this.deriveNameFromEmail(email);

    try {
      showLoading(true);
      const response = await apiService.requestVerificationCode({
        email,
        name: derivedName
      });

      if (response.success) {
        this.pendingEmail = email;
        this.showCodeStep();
        
        // Extrai o código do desenvolvimento se presente
        const match = response.message.match(/\[CÓDIGO:\s*(\d+)\]/);
        const codeValue = match ? match[1] : null;

        showNotification("Código enviado! Verifique seu console do navegador.");
        console.log("🔐 CÓDIGO DE VERIFICAÇÃO (DEV):", codeValue || response.message);
        
        if (codeValue) {
          const hint = document.querySelector("#loginCodeStep .form-hint");
          if (hint) {
            hint.innerHTML = `Código de teste (Dev): <strong style="color: var(--success-color); font-size: 15px; background: rgba(16, 185, 129, 0.1); padding: 2px 6px; border-radius: 4px;">${codeValue}</strong>`;
          }
        }
      } else {
        showError(response.message || "Erro ao solicitar o código.");
      }
    } catch (error) {
      showError("Erro na conexão com o servidor. Tente novamente.");
      console.error(error);
    } finally {
      showLoading(false);
    }
  }

  async handleVerifyCode(event) {
    if (event) event.preventDefault();
    const code = document.getElementById("verificationCode")?.value.trim();

    if (!code || code.length !== 6) {
      showError("O código de verificação deve ter 6 dígitos.");
      return;
    }

    if (!this.pendingEmail) {
      showError("E-mail pendente não encontrado. Digite o e-mail novamente.");
      this.showEmailStep();
      return;
    }

    try {
      showLoading(true);
      const response = await apiService.verifyCode({
        email: this.pendingEmail,
        code: code
      });

      if (response.success && response.student) {
        this.currentStudent = response.student;
        localStorage.setItem("currentStudent", JSON.stringify(this.currentStudent));

        // Dispara evento para outros componentes reagirem à mudança de login
        window.dispatchEvent(new CustomEvent("authChanged", { detail: this.currentStudent }));

        this.hideLoginModal();
        this.updateAuthUI();
        showNotification("Login efetuado com sucesso!");
      } else {
        showError(response.message || "Código inválido ou expirado.");
      }
    } catch (error) {
      showError("Erro na verificação. Tente novamente.");
      console.error(error);
    } finally {
      showLoading(false);
    }
  }

  handleLogout() {
    localStorage.removeItem("currentStudent");
    this.currentStudent = null;
    this.pendingEmail = null;

    // Dispara evento de logout
    window.dispatchEvent(new CustomEvent("authChanged", { detail: null }));

    this.updateAuthUI();
    showNotification("Você saiu da sessão.");
  }

  updateAuthUI() {
    const profileHeader = document.getElementById("userProfileHeader");
    if (!profileHeader) return;

    if (this.currentStudent) {
      const email = this.currentStudent.email;
      const initial = email.charAt(0).toUpperCase();

      profileHeader.innerHTML = `
        <div class="profile-logged">
          <div class="user-avatar" title="${email}">${initial}</div>
          <span class="user-email-text" title="${email}">${email}</span>
          <button class="btn btn-outline-danger btn-sm" onclick="authComponent.handleLogout()">Sair</button>
        </div>
      `;
    } else {
      profileHeader.innerHTML = `
        <div class="profile-visitor">
          <span class="visitor-avatar">👤</span>
          <span class="visitor-text">Visitante</span>
          <button id="loginBtn" class="btn btn-secondary btn-sm" onclick="authComponent.showLoginModal()">Login</button>
        </div>
      `;
    }
  }
}

// Declarado globalmente, instanciado no app.js
let authComponent;
