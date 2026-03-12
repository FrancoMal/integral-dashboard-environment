import { existsSync, mkdirSync } from "node:fs";
import { join } from "node:path";
import { spawn } from "node:child_process";
import { config } from "./config.js";
import { createProvider, listProviders } from "./providers/index.js";
import { DashboardClient } from "./dashboard-client.js";

// ── Setup ──────────────────────────────────────────────

console.log("╔══════════════════════════════════════════╗");
console.log("║       Jeff Orchestrator v1.0             ║");
console.log("╚══════════════════════════════════════════╝");
console.log(`Provider:  ${config.provider}`);
console.log(`Dashboard: ${config.dashboardUrl}`);
console.log(`Work dir:  ${config.workDir}`);
console.log(`Poll:      every ${config.pollInterval}s`);
console.log(`Available: ${listProviders().join(", ")}`);
console.log("");

if (!existsSync(config.workDir)) {
  mkdirSync(config.workDir, { recursive: true });
}

const provider = createProvider(config.provider);
const dashboard = new DashboardClient(
  config.dashboardUrl,
  config.dashboardUser,
  config.dashboardPass
);

let running = false;
let heartbeatTimer = null;

// ── Heartbeat ──────────────────────────────────────────

async function sendHeartbeat(data) {
  try {
    await dashboard.heartbeat({ provider: config.provider, ...data });
  } catch (err) {
    console.error(`[heartbeat] ${err.message}`);
  }
}

function startHeartbeat() {
  if (heartbeatTimer) clearInterval(heartbeatTimer);
  // Only send idle heartbeat if not currently running a task
  heartbeatTimer = setInterval(() => {
    if (!running) sendHeartbeat({ status: "idle" });
  }, 30_000);
}

// ── Main Loop ──────────────────────────────────────────

async function tick() {
  if (running) return;
  running = true;

  try {
    await sendHeartbeat({ status: "scanning" });

    const available = await provider.isAvailable();
    if (!available) {
      console.log(`[warn] Provider "${config.provider}" not available`);
      await sendHeartbeat({ status: "error", currentTask: "Provider not available" });
      return;
    }

    const projects = await dashboard.getProjects();
    if (!projects || projects.length === 0) {
      await sendHeartbeat({ status: "idle", currentTask: "" });
      return;
    }

    // Find first project with backlog items
    for (const project of projects) {
      const detail = await dashboard.getProjectDetail(project.id);
      if (!detail) continue;

      const backlogItems = (detail.workItems || []).filter(
        (w) => w.status === "backlog"
      );

      if (backlogItems.length === 0) continue;

      console.log(
        `[${project.name}] ${backlogItems.length} item(s) in backlog`
      );

      const item = backlogItems[0];
      console.log(`[${project.name}] Processing: "${item.title}"`);

      await processWorkItem(project, detail, item);
      break;
    }

    await sendHeartbeat({ status: "idle", currentTask: "", currentProjectName: "" });
  } catch (err) {
    console.error(`[error] Tick failed: ${err.message}`);
    await sendHeartbeat({ status: "error", currentTask: err.message });
  } finally {
    running = false;
  }
}

// ── Process a single work item ─────────────────────────

async function processWorkItem(project, detail, item) {
  const repoDir = join(config.workDir, project.name);

  try {
    // Report start
    await sendHeartbeat({
      status: "working",
      currentProjectId: project.id,
      currentProjectName: project.name,
      currentTask: item.title,
      clearOutput: true,
    });

    await dashboard.logActivity({
      projectId: project.id,
      projectName: project.name,
      action: "execute",
      title: `Ejecutando: ${item.title}`,
      detail: `Provider: ${config.provider}`,
      source: "orquestador",
      status: "in_progress",
    });

    // Update work item to in_progress
    await dashboard.updateWorkItemStatus(project.id, item.id, "in_progress");

    // Clone or pull
    await ensureRepo(project, repoDir);
    await sendHeartbeat({ outputAppend: `> Repo listo: ${project.name}\n> Ejecutando tarea...\n\n` });

    // Run the agent with output streaming
    const result = await provider.execute({
      prompt: `${item.title}\n\n${item.notes || ""}`,
      cwd: repoDir,
      context: {
        projectName: project.name,
        language: detail.project?.language,
        repoUrl: detail.project?.htmlUrl,
      },
      onOutput: (chunk) => {
        // Stream chunks to dashboard (debounced)
        sendHeartbeat({ outputAppend: chunk });
      },
    });

    if (result.success) {
      const hasChanges = await gitHasChanges(repoDir);
      if (hasChanges) {
        await gitCommit(
          repoDir,
          `${item.title}\n\nEjecutado por Jeff Orchestrator via ${config.provider}`
        );
        await sendHeartbeat({ outputAppend: "\n> Cambios commiteados\n" });
        console.log(`[${project.name}] Committed changes`);
      }

      await dashboard.updateWorkItemStatus(project.id, item.id, "done");

      await dashboard.logActivity({
        projectId: project.id,
        projectName: project.name,
        action: "complete",
        title: `Completado: ${item.title}`,
        detail: result.output?.slice(0, 500) || "Tarea ejecutada",
        source: "orquestador",
        status: "done",
      });

      await sendHeartbeat({
        status: "idle",
        outputAppend: "\n\n✓ Tarea completada\n",
      });

      console.log(`[${project.name}] Done: "${item.title}"`);
    } else {
      await dashboard.logActivity({
        projectId: project.id,
        projectName: project.name,
        action: "error",
        title: `Error en: ${item.title}`,
        detail: result.error?.slice(0, 500) || "Error desconocido",
        source: "orquestador",
        status: "done",
      });

      await sendHeartbeat({
        status: "idle",
        outputAppend: `\n\n✗ Error: ${result.error?.slice(0, 200)}\n`,
      });

      console.error(
        `[${project.name}] Failed: "${item.title}" — ${result.error}`
      );
    }
  } catch (err) {
    console.error(
      `[${project.name}] Error processing "${item.title}": ${err.message}`
    );
  }
}

// ── Git helpers ────────────────────────────────────────

async function ensureRepo(project, repoDir) {
  if (existsSync(join(repoDir, ".git"))) {
    await git(repoDir, ["pull", "--rebase"]);
  } else {
    const url = project.htmlUrl?.endsWith(".git")
      ? project.htmlUrl
      : `${project.htmlUrl}.git`;
    await git(config.workDir, ["clone", url, project.name]);
  }
}

async function gitHasChanges(cwd) {
  const result = await git(cwd, ["status", "--porcelain"]);
  return result.trim().length > 0;
}

async function gitCommit(cwd, message) {
  await git(cwd, ["add", "-A"]);
  await git(cwd, [
    "-c", `user.name=${config.gitName}`,
    "-c", `user.email=${config.gitEmail}`,
    "commit", "-m", message,
  ]);
}

function git(cwd, args) {
  return new Promise((resolve, reject) => {
    const child = spawn("git", args, {
      cwd,
      stdio: ["ignore", "pipe", "pipe"],
    });
    let stdout = "";
    let stderr = "";
    child.stdout.on("data", (d) => (stdout += d.toString()));
    child.stderr.on("data", (d) => (stderr += d.toString()));
    child.on("close", (code) => {
      if (code !== 0 && !stderr.includes("nothing to commit")) {
        reject(new Error(`git ${args[0]} failed: ${stderr}`));
      } else {
        resolve(stdout);
      }
    });
    child.on("error", reject);
  });
}

// ── Start ──────────────────────────────────────────────

async function main() {
  try {
    await dashboard.login();
    console.log("[auth] Logged in to dashboard");
  } catch (err) {
    console.error(`[auth] Login failed: ${err.message}`);
    process.exit(1);
  }

  const providerOk = await provider.isAvailable();
  console.log(
    `[provider] ${config.provider}: ${providerOk ? "available" : "NOT available"}`
  );

  if (!providerOk) {
    console.error(`Provider "${config.provider}" not available. Exiting.`);
    process.exit(1);
  }

  // Start heartbeat
  await sendHeartbeat({ status: "idle", clearOutput: true });
  startHeartbeat();

  console.log("\n[loop] Starting orchestrator loop...\n");

  await tick();
  setInterval(tick, config.pollInterval * 1000);
}

// Graceful shutdown
process.on("SIGINT", async () => {
  console.log("\n[shutdown] Stopping...");
  await sendHeartbeat({ status: "offline", currentTask: "" });
  process.exit(0);
});

process.on("SIGTERM", async () => {
  await sendHeartbeat({ status: "offline", currentTask: "" });
  process.exit(0);
});

main().catch((err) => {
  console.error("Fatal:", err);
  process.exit(1);
});
