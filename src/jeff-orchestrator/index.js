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
console.log(`Provider:    ${config.provider}`);
console.log(`Dashboard:   ${config.dashboardUrl}`);
console.log(`Work dir:    ${config.workDir}`);
console.log(`Poll:        every ${config.pollInterval}s`);
console.log(`Git push:    ${config.githubToken ? "enabled" : "disabled (no GITHUB_TOKEN)"}`);
console.log(`Git author:  ${config.gitName} <${config.gitEmail}>`);
console.log(`Providers:   ${listProviders().join(", ")}`);
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

let isWorking = false;
let currentWorkItem = null; // Track current item for graceful shutdown
let currentProjectId = null;
const failCounts = new Map(); // workItemId → number of failures

// ── Heartbeat ──────────────────────────────────────────

async function heartbeat(data) {
  try {
    await dashboard.heartbeat({ provider: config.provider, ...data });
  } catch (err) {
    console.error(`[heartbeat] ${err.message}`);
  }
}

// Send idle heartbeat every 20s when not working
setInterval(() => {
  if (!isWorking) heartbeat({ status: "idle" });
}, 20_000);

// ── Main Loop ──────────────────────────────────────────

async function tick() {
  if (isWorking) return;

  try {
    await heartbeat({ status: "scanning", currentTask: "Buscando tareas..." });

    const available = await provider.isAvailable();
    if (!available) {
      console.log(`[warn] Provider "${config.provider}" not available`);
      await heartbeat({ status: "error", currentTask: "Provider no disponible" });
      return;
    }

    const projects = await dashboard.getProjects();
    if (!projects?.length) {
      await heartbeat({ status: "idle", currentTask: "" });
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

      console.log(`[${project.name}] ${backlogItems.length} item(s) in backlog`);

      // Skip items that have failed recently (tracked by failCounts)
      const item = backlogItems.find(i => (failCounts.get(i.id) || 0) < 2) || null;
      if (!item) {
        console.log(`[${project.name}] All backlog items have failed, skipping`);
        continue;
      }
      console.log(`[${project.name}] → "${item.title}"`);

      await processWorkItem(project, detail, item);
      return; // One task per tick
    }

    await heartbeat({ status: "idle", currentTask: "Sin tareas pendientes" });
  } catch (err) {
    console.error(`[error] Tick: ${err.message}`);
    await heartbeat({ status: "error", currentTask: err.message });
  }
}

// ── Process Work Item ──────────────────────────────────

async function processWorkItem(project, detail, item) {
  const repoDir = join(config.workDir, project.name);
  isWorking = true;

  try {
    // ── Report start ──
    await heartbeat({
      status: "working",
      currentProjectId: project.id,
      currentProjectName: project.name,
      currentTask: item.title,
      clearOutput: true,
    });

    await dashboard.logActivity({
      projectId: project.id,
      projectName: project.name,
      action: "execute_start",
      title: `Iniciando: ${item.title}`,
      detail: `Provider: ${config.provider}`,
      source: "orquestador",
      status: "in_progress",
    });

    // Track current item for graceful shutdown
    currentWorkItem = item;
    currentProjectId = project.id;

    await dashboard.updateWorkItemStatus(project.id, item.id, "in_progress");

    // ── Clone / pull repo ──
    console.log(`[${project.name}] Preparing repo at ${repoDir}`);
    await heartbeat({ outputAppend: `⏳ Preparando repo ${project.name}...\n` });
    try {
      await ensureRepo(project, repoDir);
    } catch (repoErr) {
      console.error(`[${project.name}] Repo error: ${repoErr.message}`);
      await heartbeat({ outputAppend: `❌ Error de repo: ${repoErr.message}\n` });
      throw repoErr;
    }
    console.log(`[${project.name}] Repo ready, running agent...`);
    await heartbeat({ outputAppend: `✓ Repo listo\n\n🤖 Ejecutando tarea con ${config.provider}...\n\n` });

    // ── Run agent with streaming ──
    console.log(`[${project.name}] Calling provider.execute()...`);
    const result = await provider.execute({
      prompt: `${item.title}\n\n${item.notes || ""}`,
      cwd: repoDir,
      context: {
        projectName: project.name,
        language: detail.project?.language,
        repoUrl: detail.project?.htmlUrl,
      },
      onOutput: (chunk) => {
        // Stream to dashboard in real time
        heartbeat({ outputAppend: chunk });
      },
    });

    console.log(`[${project.name}] Provider result: success=${result.success}, output=${result.output?.length || 0} chars, error=${result.error?.slice(0,200) || 'none'}`);

    if (result.success) {
      // ── Commit & Push ──
      const hasChanges = await gitHasChanges(repoDir);
      if (hasChanges) {
        await heartbeat({ outputAppend: "\n\n📦 Commiteando cambios...\n" });
        await gitCommit(repoDir, item.title);

        if (config.githubToken && project.htmlUrl) {
          await heartbeat({ outputAppend: "🚀 Pusheando a GitHub...\n" });
          await gitPush(repoDir, project);
          await heartbeat({ outputAppend: "✓ Push exitoso\n" });
          console.log(`[${project.name}] Pushed to GitHub`);
        }

        const files = result.filesChanged.length > 0
          ? result.filesChanged.join(", ")
          : await getChangedFiles(repoDir);
        await heartbeat({ outputAppend: `\n✓ Archivos: ${files}\n` });
        console.log(`[${project.name}] Committed: ${files}`);
      } else {
        await heartbeat({ outputAppend: "\nℹ️ Sin cambios en archivos\n" });
      }

      // ── Mark done ──
      await dashboard.updateWorkItemStatus(project.id, item.id, "done");

      await dashboard.logActivity({
        projectId: project.id,
        projectName: project.name,
        action: "execute_done",
        title: `Completado: ${item.title}`,
        detail: (result.output || "").slice(0, 500),
        source: "orquestador",
        status: "done",
      });

      await heartbeat({ outputAppend: "\n\n✅ Tarea completada\n" });
      console.log(`[${project.name}] ✓ Done: "${item.title}"`);
    } else {
      // ── Failed ──
      await dashboard.logActivity({
        projectId: project.id,
        projectName: project.name,
        action: "execute_error",
        title: `Error en: ${item.title}`,
        detail: (result.error || "").slice(0, 500),
        source: "orquestador",
        status: "done",
      });

      // Track failure count
      const fails = (failCounts.get(item.id) || 0) + 1;
      failCounts.set(item.id, fails);

      if (fails >= 2) {
        // Too many failures, mark as done with error note
        await dashboard.updateWorkItemStatus(project.id, item.id, "done");
        await heartbeat({ outputAppend: `\n⚠️ Marcado como completado tras ${fails} intentos fallidos\n` });
      }

      await heartbeat({ outputAppend: `\n\n❌ Error: ${(result.error || "").slice(0, 200)}\n` });
      console.error(`[${project.name}] ✗ Failed: "${item.title}" — ${result.error?.slice(0, 300)}`);
    }
  } catch (err) {
    console.error(`[${project.name}] Error: ${err.message}\n${err.stack}`);
    await heartbeat({ outputAppend: `\n\n❌ Error interno: ${err.message}\n` });
  } finally {
    isWorking = false;
    currentWorkItem = null;
    currentProjectId = null;
    await heartbeat({ status: "idle", currentTask: "" });
  }
}

// ── Git helpers ────────────────────────────────────────

async function ensureRepo(project, repoDir) {
  const cloneUrl = getAuthenticatedUrl(project);

  if (existsSync(join(repoDir, ".git"))) {
    // Set remote URL with token for push
    await git(repoDir, ["remote", "set-url", "origin", cloneUrl]);
    await git(repoDir, ["pull", "--rebase", "--autostash"]);
  } else {
    await git(config.workDir, ["clone", cloneUrl, project.name]);
  }

  // Set user config
  await git(repoDir, ["config", "user.name", config.gitName]);
  await git(repoDir, ["config", "user.email", config.gitEmail]);
}

function getAuthenticatedUrl(project) {
  const baseUrl = project.htmlUrl || "";
  if (config.githubToken && baseUrl.includes("github.com")) {
    // https://TOKEN@github.com/owner/repo.git
    return baseUrl.replace(
      "https://github.com",
      `https://${config.githubToken}@github.com`
    ) + ".git";
  }
  return baseUrl.endsWith(".git") ? baseUrl : baseUrl + ".git";
}

async function gitHasChanges(cwd) {
  const result = await git(cwd, ["status", "--porcelain"]);
  return result.trim().length > 0;
}

async function gitCommit(cwd, title) {
  await git(cwd, ["add", "-A"]);

  // Generate a commit message based on actual changes
  let commitMsg = title;
  try {
    const diff = await git(cwd, ["diff", "--cached", "--stat"]);
    const changedFiles = diff.trim().split("\n").filter(l => l.trim()).slice(0, -1); // Remove summary line
    if (changedFiles.length > 0) {
      const fileNames = changedFiles.map(l => l.trim().split(/\s+/)[0]).filter(Boolean);
      commitMsg = `${title}\n\nArchivos modificados:\n${fileNames.map(f => `- ${f}`).join("\n")}`;
    }
  } catch {
    // fallback to just title
  }

  await git(cwd, ["commit", "-m", commitMsg]);
}

async function gitPush(cwd, project) {
  const branch = await git(cwd, ["branch", "--show-current"]);
  await git(cwd, ["push", "origin", branch.trim()]);
}

async function getChangedFiles(cwd) {
  try {
    const result = await git(cwd, ["diff", "--name-only", "HEAD~1"]);
    return result.trim() || "(sin info)";
  } catch {
    return "(sin info)";
  }
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
        reject(new Error(`git ${args[0]}: ${stderr.trim()}`));
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
    console.log("[auth] ✓ Dashboard login OK");
  } catch (err) {
    console.error(`[auth] ✗ Login failed: ${err.message}`);
    process.exit(1);
  }

  const providerOk = await provider.isAvailable();
  console.log(`[provider] ${config.provider}: ${providerOk ? "✓ available" : "✗ NOT available"}`);

  if (!providerOk) {
    console.error(`Provider "${config.provider}" not available.`);
    process.exit(1);
  }

  await heartbeat({ status: "idle", clearOutput: true });

  console.log(`\n[loop] Polling every ${config.pollInterval}s — Ctrl+C to stop\n`);

  // First tick immediately
  await tick();

  // Then poll
  setInterval(tick, config.pollInterval * 1000);
}

async function gracefulShutdown(signal) {
  console.log(`\n[shutdown] ${signal} received`);

  // Return current work item to backlog if interrupted
  if (currentWorkItem && currentProjectId) {
    console.log(`[shutdown] Returning "${currentWorkItem.title}" to backlog`);
    try {
      await dashboard.updateWorkItemStatus(currentProjectId, currentWorkItem.id, "backlog");
      await dashboard.logActivity({
        projectId: currentProjectId,
        projectName: "",
        action: "interrupted",
        title: `Interrumpido: ${currentWorkItem.title}`,
        detail: `El orquestador fue detenido (${signal}). Tarea devuelta al backlog.`,
        source: "orquestador",
        status: "done",
      });
    } catch (err) {
      console.error(`[shutdown] Could not reset item: ${err.message}`);
    }
  }

  await heartbeat({ status: "offline" });
  process.exit(0);
}

process.on("SIGINT", () => gracefulShutdown("SIGINT"));
process.on("SIGTERM", () => gracefulShutdown("SIGTERM"));

main().catch((err) => {
  console.error("Fatal:", err);
  process.exit(1);
});
