/**
 * Evaluation Component
 * Gerencia o perfil do professor selecionado e o módulo interativo de avaliação
 */

class EvaluationComponent {
  constructor() {
    this.selectedProfessor = null;
    this.currentStudent = null;
    this.selectedRating = 0;
    this.hasEvaluated = false;
    this.previousRating = 0;
    this.previousComment = "";
    this.evaluations = [];
    this.init();
  }

  init() {
    this.setupEventListeners();
    this.loadStoredStudent();
  }

  setupEventListeners() {
    window.addEventListener("professorSelected", (e) => this.onProfessorSelected(e));
    window.addEventListener("authChanged", (e) => this.onAuthChanged(e));
  }

  loadStoredStudent() {
    const stored = localStorage.getItem("currentStudent");
    if (stored) {
      this.currentStudent = JSON.parse(stored);
    } else {
      this.currentStudent = null;
    }
  }

  onAuthChanged(event) {
    this.currentStudent = event.detail;
    // Recarrega os dados de avaliação do professor atual se houver um selecionado
    if (this.selectedProfessor) {
      this.loadProfessorEvaluationData();
    } else {
      this.render();
    }
  }

  async onProfessorSelected(event) {
    this.selectedProfessor = event.detail;
    this.selectedRating = 0;
    this.hasEvaluated = false;
    this.previousRating = 0;
    this.previousComment = "";
    
    await this.loadProfessorEvaluationData();
  }

  async loadProfessorEvaluationData() {
    if (!this.selectedProfessor) return;

    try {
      showLoading(true);
      // Carrega todas as avaliações deste professor para as estatísticas
      this.evaluations = await apiService.getEvaluationsByProfessor(this.selectedProfessor.id);
      
      // Se estiver logado, verifica se o aluno já avaliou este professor
      if (this.currentStudent) {
        const check = await apiService.checkVote(this.currentStudent.id, this.selectedProfessor.id);
        if (check.hasEvaluated) {
          this.hasEvaluated = true;
          this.previousRating = check.score;
          this.previousComment = check.comment || "";
          
          // Preenche a nota e o comentário atuais para edição
          this.selectedRating = check.score;
        }
      }
      this.render();
    } catch (error) {
      console.error("Erro ao carregar dados de avaliação:", error);
      this.evaluations = [];
      this.render();
    } finally {
      showLoading(false);
    }
  }

  calculateAverageRating() {
    if (this.evaluations.length === 0) return 0.0;
    const sum = this.evaluations.reduce((acc, e) => acc + e.score, 0);
    return sum / this.evaluations.length;
  }

  setRating(score) {
    this.selectedRating = score;
    this.renderInteractiveStars();
  }

  async submitEvaluation() {
    if (!this.currentStudent) {
      showError("Você precisa fazer login para avaliar.");
      return;
    }

    if (!this.selectedProfessor) {
      showError("Selecione um professor.");
      return;
    }

    if (this.selectedRating === 0) {
      showError("Selecione uma nota clicando nas estrelas.");
      return;
    }

    const comment = document.getElementById("evaluationComment")?.value.trim() || "";

    try {
      showLoading(true);

      const evaluation = {
        studentId: this.currentStudent.id,
        professorId: this.selectedProfessor.id,
        score: this.selectedRating,
        comment: comment
      };

      await apiService.createEvaluation(evaluation);

      // Dispara evento para atualizar a lista lateral de professores (e suas médias)
      window.dispatchEvent(new CustomEvent("evaluationSubmitted"));

      // Recarrega os dados locais atualizados
      await this.loadProfessorEvaluationData();
      
      showNotification(this.hasEvaluated ? "Avaliação atualizada com sucesso!" : "Avaliação enviada com sucesso!");
    } catch (error) {
      showError("Erro ao enviar avaliação: " + error.message);
      console.error(error);
    } finally {
      showLoading(false);
    }
  }

  renderStars(rating, isReadOnly = true) {
    const stars = [];
    const roundedRating = Math.round(rating);
    
    if (isReadOnly) {
      for (let i = 1; i <= 5; i++) {
        stars.push(`<span class="star-icon-read ${i <= roundedRating ? "filled" : "empty"}">★</span>`);
      }
    } else {
      // Loop reverso de 5 para 1 para habilitar o truque de hover/active do CSS
      for (let i = 5; i >= 1; i--) {
        stars.push(`
          <button type="button" 
                  class="star-interactive-btn ${i === this.selectedRating ? "active" : ""}" 
                  data-rating="${i}"
                  onclick="evaluationComponent.setRating(${i}); return false;">
              ★
          </button>
        `);
      }
    }
    return stars.join("");
  }

  renderInteractiveStars() {
    const container = document.getElementById("interactiveStarsContainer");
    if (container) {
      container.innerHTML = this.renderStars(this.selectedRating, false);
    }
  }

  getInitials(name) {
    if (!name) return "?";
    const cleanName = name.replace(/^(Prof\.\s+|Profa\.\s+)/i, "").trim();
    const parts = cleanName.split(/\s+/);
    if (parts.length === 1) return parts[0].charAt(0).toUpperCase();
    return (parts[0].charAt(0) + parts[parts.length - 1].charAt(0)).toUpperCase();
  }

  getColorForName(name) {
    let hash = 0;
    for (let i = 0; i < name.length; i++) {
      hash = name.charCodeAt(i) + ((hash << 5) - hash);
    }
    const hue = Math.abs(hash % 40) + 200;
    const saturation = Math.abs(hash % 20) + 45;
    const lightness = Math.abs(hash % 15) + 35;
    return `hsl(${hue}, ${saturation}%, ${lightness}%)`;
  }

  render() {
    const container = document.getElementById("professorDetailCard");
    if (!container) return;

    if (!this.selectedProfessor) {
      container.innerHTML = `
        <div class="card-placeholder">
          <p>Selecione um docente para avaliar</p>
        </div>
      `;
      return;
    }

    const avgRating = this.calculateAverageRating();
    const avgText = avgRating > 0 ? avgRating.toFixed(1) : "—";
    const initials = this.getInitials(this.selectedProfessor.name);
    const avatarColor = this.getColorForName(this.selectedProfessor.name);
    const isLoggedIn = !!this.currentStudent;

    container.innerHTML = `
      <div class="professor-detail-card">
        <!-- Cabeçalho do Professor com foto e nota média -->
        <div class="prof-detail-header">
          <div class="prof-detail-avatar" style="background-color: ${avatarColor}">
            ${initials}
          </div>
          <div class="prof-detail-info">
            <h2>${this.selectedProfessor.name}</h2>
            <div class="prof-detail-dept">${this.selectedProfessor.department || "Docente"}</div>
            ${this.selectedProfessor.specialization ? `<div class="prof-detail-spec">${this.selectedProfessor.specialization}</div>` : ""}
            
            <div class="prof-detail-score-container">
              <span class="rating-value-large">${avgText}</span>
              <div class="stars-read-only-wrapper">
                ${this.renderStars(avgRating, true)}
              </div>
              <span class="rating-count-label">(${this.evaluations.length} avaliações)</span>
            </div>
          </div>
        </div>

        <!-- Módulo de Avaliação Interativo -->
        <div class="evaluation-module">
          ${
            isLoggedIn
              ? `
              <form class="evaluation-interactive-form" onsubmit="evaluationComponent.submitEvaluation(); return false;">
                <div class="eval-form-header">
                  <h3>${this.hasEvaluated ? "Editar sua avaliação" : "Avaliar este professor"}</h3>
                  <span class="anon-shield-badge">🛡️ Avaliação Anônima</span>
                </div>
                
                <div class="interactive-stars-wrapper">
                  <div id="interactiveStarsContainer" class="interactive-stars-container">
                    ${this.renderStars(this.selectedRating, false)}
                  </div>
                </div>

                <div class="form-group">
                  <label for="evaluationComment">Comentário (opcional):</label>
                  <textarea id="evaluationComment" 
                            placeholder="Adicione um comentário construtivo..."
                            maxlength="1000">${this.previousComment}</textarea>
                  <div class="comment-hint">Sua opinião ajuda a melhorar o ensino. Máximo 1000 caracteres.</div>
                </div>

                <button type="submit" class="btn btn-primary btn-block btn-submit-rating">
                  ${this.hasEvaluated ? "Editar nota anterior" : "Enviar Avaliação"}
                </button>
              </form>
            `
              : `
              <div class="evaluation-visitor-prompt">
                <span class="lock-icon">🔒</span>
                <p class="visitor-message">Para avaliar realize o login</p>
                <button class="btn btn-primary btn-access-login" onclick="authComponent.showLoginModal()">Acessar</button>
              </div>
            `
          }
        </div>

        <!-- Comentários Recentes -->
        <div class="comments-section">
          <h3>Comentários de Estudantes</h3>
          ${
            this.evaluations.length === 0
              ? `<div class="no-comments-box">Nenhuma avaliação enviada ainda.</div>`
              : `
              <div class="comments-list-box">
                ${this.evaluations
                  .map((evalItem) => {
                    const starsHTML = this.renderStars(evalItem.score, true);
                    const dateStr = evalItem.evaluationDate
                      ? new Date(evalItem.evaluationDate).toLocaleDateString("pt-BR")
                      : "";
                    const commentContent = evalItem.comment
                      ? evalItem.comment
                      : `<span class="empty-comment-label">Avaliado sem comentário escrito.</span>`;

                    return `
                      <div class="comment-item-card">
                        <div class="comment-item-header">
                          <div class="comment-item-user">
                            <span class="voter-icon">🛡️</span>
                            <span class="voter-text">Estudante UCSAL</span>
                          </div>
                          <div class="comment-item-stars">${starsHTML}</div>
                          <div class="comment-item-date">${dateStr}</div>
                        </div>
                        <div class="comment-item-text">${commentContent}</div>
                      </div>
                    `;
                  })
                  .join("")}
              </div>
            `
          }
        </div>
      </div>
    `;
  }
}

// Instância global do componente
let evaluationComponent;
