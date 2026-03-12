import { spawn } from "node:child_process";
import { BaseProvider } from "./base.js";

const TIMEOUT_MS = 300_000; // 5 min per task

export class ClaudeCodeProvider extends BaseProvider {
  constructor() {
    super("claude-code");
  }

  async isAvailable() {
    try {
      const result = await this.#run(["--version"], process.cwd(), 10_000);
      return result.exitCode === 0;
    } catch {
      return false;
    }
  }

  async execute({ prompt, cwd, context }) {
    const fullPrompt = this.#buildPrompt(prompt, context);

    try {
      const result = await this.#run(
        [
          "-p",
          "--output-format",
          "text",
          "--allowedTools",
          "Read,Edit,Write,Bash,Glob,Grep",
        ],
        cwd,
        TIMEOUT_MS,
        fullPrompt
      );

      if (result.exitCode !== 0) {
        return {
          success: false,
          output: result.stderr || "Claude Code exited with error",
          filesChanged: [],
          error: result.stderr,
        };
      }

      const filesChanged = this.#extractFilesChanged(result.stdout);

      return {
        success: true,
        output: result.stdout,
        filesChanged,
        error: null,
      };
    } catch (err) {
      return {
        success: false,
        output: "",
        filesChanged: [],
        error: err.message,
      };
    }
  }

  #buildPrompt(taskDescription, context) {
    return `Sos un agente de desarrollo que ejecuta tareas concretas en repositorios reales.

PROYECTO: ${context?.projectName || "desconocido"}
LENGUAJE: ${context?.language || "desconocido"}
REPO: ${context?.repoUrl || "local"}

TAREA A EJECUTAR:
${taskDescription}

INSTRUCCIONES:
- Lee el codigo existente antes de modificar
- Hace cambios minimos y precisos para cumplir la tarea
- No rompas funcionalidad existente
- Si necesitas crear archivos nuevos, hacelo
- Si necesitas instalar dependencias, usa el package manager del proyecto
- Al terminar, verifica que el proyecto compila/funciona
- Resume brevemente que hiciste al final`;
  }

  #extractFilesChanged(output) {
    const files = [];
    const patterns = [
      /(?:Created|Modified|Edited|Wrote)\s+(?:file\s+)?[`"']?([^\s`"']+)[`"']?/gi,
      /Writing to\s+([^\s]+)/gi,
    ];
    for (const pattern of patterns) {
      let match;
      while ((match = pattern.exec(output)) !== null) {
        files.push(match[1]);
      }
    }
    return [...new Set(files)];
  }

  /**
   * Spawns claude CLI as a child process.
   * Uses spawn (not exec) to avoid shell injection.
   */
  #run(args, cwd, timeout, stdinData = null) {
    return new Promise((resolve, reject) => {
      const child = spawn("claude", args, {
        cwd,
        env: { ...process.env, CLAUDECODE: undefined },
        stdio: ["pipe", "pipe", "pipe"],
        timeout,
      });

      let stdout = "";
      let stderr = "";

      child.stdout.on("data", (d) => (stdout += d.toString()));
      child.stderr.on("data", (d) => (stderr += d.toString()));

      child.on("close", (exitCode) => {
        resolve({ exitCode, stdout: stdout.trim(), stderr: stderr.trim() });
      });

      child.on("error", reject);

      if (stdinData) {
        child.stdin.write(stdinData);
      }
      child.stdin.end();
    });
  }
}
