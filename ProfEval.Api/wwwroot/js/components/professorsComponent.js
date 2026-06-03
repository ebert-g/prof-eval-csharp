/**
 * Professors Component
 * Gerencia a listagem e seleção de professores na barra lateral
 */

class ProfessorsComponent {
  constructor() {
    this.professors = [];
    this.selectedProfessor = null;
    this.filteredProfessors = [];
    this.init();
  }

  async init() {
    this.setupEventListeners();
    await this.loadProfessors();
  }

  setupEventListeners() {
    const searchInput = document.getElementById("searchProfessor");
    searchInput?.addEventListener("input", (e) => this.handleSearch(e));

    // Recarrega professores quando uma avaliação é submetida para atualizar a média
    window.addEventListener("evaluationSubmitted", () => this.loadProfessors(false));
  }

  async loadProfessors(showLoadingOverlay = true) {
    try {
      if (showLoadingOverlay) showLoading(true);
      
      // Sempre carrega da API para obter a nota média em tempo real
      this.professors = await apiService.getProfessors();
      this.filteredProfessors = [...this.professors];
      
      // Se já houver um professor selecionado, atualiza a referência dele
      if (this.selectedProfessor) {
        const updated = this.professors.find(p => p.id === this.selectedProfessor.id);
        if (updated) {
          this.selectedProfessor = updated;
        }
      }

      this.render();
    } catch (error) {
      showError("Erro ao carregar lista de professores: " + error.message);
      console.error(error);
    } finally {
      if (showLoadingOverlay) showLoading(false);
    }
  }

  handleSearch(event) {
    const query = event.target.value.toLowerCase();

    this.filteredProfessors = this.professors.filter(
      (prof) =>
        prof.name.toLowerCase().includes(query) ||
        (prof.department && prof.department.toLowerCase().includes(query))
    );

    this.render();
  }

  selectProfessor(professorId) {
    this.selectedProfessor = this.professors.find((p) => p.id === professorId);
    this.render();

    // Dispara evento para o componente de avaliação atualizar
    window.dispatchEvent(
      new CustomEvent("professorSelected", {
        detail: this.selectedProfessor,
      }),
    );
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
    // Tons de azul e cinza institucionais baseados no nome do professor
    const hue = Math.abs(hash % 40) + 200; // Hue entre 200 (azul) e 240 (azul escuro)
    const saturation = Math.abs(hash % 20) + 45; // Saturação entre 45% e 65%
    const lightness = Math.abs(hash % 15) + 35; // Lightness entre 35% e 50%
    return `hsl(${hue}, ${saturation}%, ${lightness}%)`;
  }

  render() {
    const container = document.getElementById("professorList");
    if (!container) return;

    if (this.filteredProfessors.length === 0) {
      container.innerHTML =
        '<div class="loading-state">Nenhum docente encontrado</div>';
      return;
    }

    container.innerHTML = this.filteredProfessors
      .map(
        (prof) => {
          const initials = this.getInitials(prof.name);
          const avatarColor = this.getColorForName(prof.name);
          const ratingText = prof.averageRating > 0 ? prof.averageRating.toFixed(1) : "—";
          const hasRating = prof.averageRating > 0;

          return `
            <div class="professor-card ${this.selectedProfessor?.id === prof.id ? "active" : ""}" 
                 data-id="${prof.id}"
                 onclick="professorsComponent.selectProfessor(${prof.id})">
                <div class="prof-avatar-circle" style="background-color: ${avatarColor}">
                    ${initials}
                </div>
                <div class="prof-info-block">
                    <div class="prof-name-text">${prof.name}</div>
                    <div class="prof-dept-text">${prof.department || "Docente"}</div>
                </div>
                <div class="prof-rating-badge ${hasRating ? 'has-rating' : 'no-rating'}">
                    <span class="star-badge-icon">★</span>
                    <span class="star-badge-value">${ratingText}</span>
                </div>
            </div>
          `;
        }
      )
      .join("");
  }
}

// Instância global do componente
let professorsComponent;
