import { readFileSync } from "node:fs";
import { resolve } from "node:path";

// Try to read .env file from project root
function loadEnv() {
  try {
    const envPath = resolve(import.meta.dirname, "../../.env");
    const content = readFileSync(envPath, "utf-8");
    // Only load specific keys we need, not API keys that could interfere with Claude auth
    const allowedKeys = new Set(["GITHUB_TOKEN", "DASHBOARD_URL", "DASHBOARD_USER", "DASHBOARD_PASS", "AGENT_PROVIDER", "WORK_DIR", "POLL_INTERVAL", "GIT_NAME", "GIT_EMAIL"]);
    for (const line of content.split("\n")) {
      const trimmed = line.trim();
      if (!trimmed || trimmed.startsWith("#")) continue;
      const eqIdx = trimmed.indexOf("=");
      if (eqIdx === -1) continue;
      const key = trimmed.slice(0, eqIdx).trim();
      const val = trimmed.slice(eqIdx + 1).trim();
      if (allowedKeys.has(key) && !process.env[key] && val && !val.includes("your-api-key")) {
        process.env[key] = val;
      }
    }
  } catch {}
}

loadEnv();

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

  // GitHub token for push
  githubToken: process.env.GITHUB_TOKEN || "",

  // Git config for commits
  gitName: process.env.GIT_NAME || "FrancoMal",
  gitEmail: process.env.GIT_EMAIL || "103511238+FrancoMal@users.noreply.github.com",
};
