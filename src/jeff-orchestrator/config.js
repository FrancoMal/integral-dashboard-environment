export const config = {
  // Dashboard API
  dashboardUrl: process.env.DASHBOARD_URL || "http://localhost:3000",
  dashboardUser: process.env.DASHBOARD_USER || "admin",
  dashboardPass: process.env.DASHBOARD_PASS || "admin123",

  // Agent provider: "claude-code" | "codex" | "openclaw"
  provider: process.env.AGENT_PROVIDER || "claude-code",

  // Work directory for cloned repos
  workDir: process.env.WORK_DIR || "/tmp/jeff-orchestrator/repos",

  // Polling interval in seconds
  pollInterval: parseInt(process.env.POLL_INTERVAL || "60", 10),

  // Max concurrent tasks
  maxConcurrent: parseInt(process.env.MAX_CONCURRENT || "1", 10),

  // Git config for commits
  gitName: process.env.GIT_NAME || "Jeff Orchestrator",
  gitEmail: process.env.GIT_EMAIL || "jeff@orchestrator.local",
};
