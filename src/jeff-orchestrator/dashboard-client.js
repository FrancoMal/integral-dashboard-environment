/**
 * Client for the Jeff Ops Dashboard API.
 * Handles auth and provides methods for the orchestrator to
 * read work items, report activity, and send heartbeats.
 */
export class DashboardClient {
  #baseUrl;
  #user;
  #pass;
  #token;

  constructor(baseUrl, user, pass) {
    this.#baseUrl = baseUrl.replace(/\/$/, "");
    this.#user = user;
    this.#pass = pass;
    this.#token = null;
  }

  async login() {
    const res = await fetch(`${this.#baseUrl}/api/auth/login`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ username: this.#user, password: this.#pass }),
    });
    if (!res.ok) throw new Error(`Login failed: ${res.status}`);
    const data = await res.json();
    this.#token = data.token;
    return data;
  }

  async getProjects() {
    return (await this.#get("/api/github/projects")) || [];
  }

  async getProjectDetail(projectId) {
    return await this.#get(`/api/github/projects/${projectId}/detail`);
  }

  async updateWorkItemStatus(projectId, workItemId, newStatus, errorMessage = null) {
    const body = { status: newStatus };
    if (errorMessage) body.errorMessage = errorMessage;
    return await this.#post(
      `/api/github/projects/${projectId}/workitems/${workItemId}/status`,
      body
    );
  }

  async logActivity(activity) {
    return await this.#post("/api/dashboard/activity-log", activity);
  }

  async heartbeat(data) {
    return await this.#post("/api/dashboard/orchestrator/heartbeat", data);
  }

  async #get(path) {
    await this.#ensureToken();
    const res = await fetch(`${this.#baseUrl}${path}`, {
      headers: { Authorization: `Bearer ${this.#token}` },
    });
    if (res.status === 401) {
      await this.login();
      return this.#get(path);
    }
    if (!res.ok) return null;
    return res.json();
  }

  async #post(path, body) {
    await this.#ensureToken();
    const res = await fetch(`${this.#baseUrl}${path}`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${this.#token}`,
      },
      body: JSON.stringify(body),
    });
    if (res.status === 401) {
      await this.login();
      return this.#post(path, body);
    }
    if (!res.ok) return null;
    const text = await res.text();
    return text ? JSON.parse(text) : null;
  }

  async #ensureToken() {
    if (!this.#token) await this.login();
  }
}
