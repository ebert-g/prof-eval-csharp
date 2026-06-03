/**
 * Main Application
 * Orquestrador da aplicação
 */

class App {
  async init() {
    try {
      // Inicializar componentes
      professorsComponent = new ProfessorsComponent();
      evaluationComponent = new EvaluationComponent();
      authComponent = new AuthComponent();

      console.log("✅ Aplicação inicializada com sucesso!");
    } catch (error) {
      console.error("❌ Erro ao inicializar aplicação:", error);
      showError("Erro ao inicializar aplicação: " + error.message);
    }
  }
}

/**
 * Utilitários globais
 */

function showLoading(visible) {
  const loadingOverlay = document.getElementById("loading");
  if (loadingOverlay) {
    loadingOverlay.style.display = visible ? "flex" : "none";
  }
}

function showError(message) {
  const errorModal = document.getElementById("errorModal");
  const errorMessage = document.getElementById("errorMessage");

  if (errorMessage) {
    errorMessage.textContent = message;
  }

  if (errorModal) {
    errorModal.style.display = "flex";
  }

  // Fechar modal ao clicar no X
  const closeBtn = errorModal?.querySelector(".modal-close");
  closeBtn?.addEventListener("click", () => {
    errorModal.style.display = "none";
  });

  // Fechar modal ao clicar fora
  errorModal?.addEventListener("click", (e) => {
    if (e.target === errorModal) {
      errorModal.style.display = "none";
    }
  });
}

function showNotification(message) {
  // Simples notificação via console
  console.log("✅ " + message);

  // Opcional: Criar um toast visual
  const toast = document.createElement("div");
  toast.style.cssText = `
        position: fixed;
        bottom: 20px;
        right: 20px;
        background-color: #4CAF50;
        color: white;
        padding: 15px 20px;
        border-radius: 8px;
        box-shadow: 0 2px 8px rgba(0, 0, 0, 0.2);
        z-index: 10000;
        animation: slideIn 0.3s ease-out;
    `;
  toast.textContent = message;
  document.body.appendChild(toast);

  setTimeout(() => {
    toast.style.animation = "slideOut 0.3s ease-out";
    setTimeout(() => toast.remove(), 300);
  }, 3000);
}

// Adicionar animações de toast
const style = document.createElement("style");
style.textContent = `
    @keyframes slideIn {
        from {
            transform: translateX(400px);
            opacity: 0;
        }
        to {
            transform: translateX(0);
            opacity: 1;
        }
    }
    @keyframes slideOut {
        from {
            transform: translateX(0);
            opacity: 1;
        }
        to {
            transform: translateX(400px);
            opacity: 0;
        }
    }
`;
document.head.appendChild(style);

/**
 * Inicializar aplicação quando DOM estiver pronto
 */
document.addEventListener("DOMContentLoaded", () => {
  const app = new App();
  app.init();
});
