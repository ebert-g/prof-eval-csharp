/**
 * API Service
 * Camada de abstração para comunicação com o backend
 * Padrão: Service Layer
 */

class ApiService {
  constructor(baseUrl = "/api") {
    this.baseUrl = baseUrl;
  }

  /**
   * Realiza uma requisição HTTP
   * @param {string} endpoint
   * @param {string} method
   * @param {object} data
   * @returns {Promise}
   */
  async request(endpoint, method = "GET", data = null) {
    const url = `${this.baseUrl}${endpoint}`;

    const config = {
      method,
      headers: {
        "Content-Type": "application/json",
      },
    };

    if (data) {
      config.body = JSON.stringify(data);
    }

    try {
      const response = await fetch(url, config);

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
      }

      if (response.status === 204) {
        return null;
      }

      return await response.json();
    } catch (error) {
      console.error("API Error:", error);
      throw error;
    }
  }

  // ===== STUDENTS =====
  getStudents() {
    return this.request("/students");
  }

  getStudent(id) {
    return this.request(`/students/${id}`);
  }

  createStudent(student) {
    return this.request("/students", "POST", student);
  }

  updateStudent(id, student) {
    return this.request(`/students/${id}`, "PUT", student);
  }

  deleteStudent(id) {
    return this.request(`/students/${id}`, "DELETE");
  }

  // ===== PROFESSORS =====
  getProfessors() {
    return this.request("/professors");
  }

  getProfessor(id) {
    return this.request(`/professors/${id}`);
  }

  createProfessor(professor) {
    return this.request("/professors", "POST", professor);
  }

  updateProfessor(id, professor) {
    return this.request(`/professors/${id}`, "PUT", professor);
  }

  deleteProfessor(id) {
    return this.request(`/professors/${id}`, "DELETE");
  }

  // ===== EVALUATIONS =====
  getEvaluations() {
    return this.request("/evaluations");
  }

  getEvaluation(id) {
    return this.request(`/evaluations/${id}`);
  }

  createEvaluation(evaluation) {
    return this.request("/evaluations", "POST", evaluation);
  }

  updateEvaluation(id, evaluation) {
    return this.request(`/evaluations/${id}`, "PUT", evaluation);
  }

  deleteEvaluation(id) {
    return this.request(`/evaluations/${id}`, "DELETE");
  }

  getEvaluationsByStudent(studentId) {
    return this.request(`/evaluations/student/${studentId}`);
  }

  getEvaluationsByProfessor(professorId) {
    return this.request(`/evaluations/professor/${professorId}`);
  }

  // ===== AUTH =====
  requestVerificationCode(data) {
    return this.request("/auth/request-code", "POST", data);
  }

  verifyCode(data) {
    return this.request("/auth/verify-code", "POST", data);
  }

  getStudentByEmail(email) {
    return this.request(`/auth/student/${email}`);
  }

  loginPasswordless(email) {
    return this.request("/auth/login-passwordless", "POST", { email });
  }

  checkVote(studentId, professorId) {
    return this.request(`/evaluations/check-vote?studentId=${studentId}&professorId=${professorId}`);
  }
}

// Instância global do serviço
const apiService = new ApiService();
