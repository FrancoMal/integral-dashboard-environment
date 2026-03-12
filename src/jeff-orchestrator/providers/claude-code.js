import { spawn } from "node:child_process";
import { BaseProvider } from "./base.js";

const TIMEOUT_MS = 300_000; // 5 min per task

export class ClaudeCodeProvider extends BaseProvider {
  constructor() {
    super("claude-code");
  }

  async isAvailable() {
    try {
      const result = await this.#runSimple(["--version"], process.cwd(), 10_000);
      return result.exitCode === 0;
    } catch {
      return false;
    }
  }

  /**
   * Execute a task using Claude Code SDK (stream-json mode).
   * @param {object} options
   * @param {string} options.prompt
   * @param {string} options.cwd
   * @param {object} options.context
   * @param {function} [options.onOutput] - Called with formatted text for live dashboard
   */
  async execute({ prompt, cwd, context, onOutput }) {
    const fullPrompt = this.#buildPrompt(prompt, context);

    try {
      const result = await this.#runStreaming(
        [
          "-p",
          "--output-format", "text",
          "--allowedTools", "Read,Edit,Write,Bash,Glob,Grep",
        ],
        cwd,
        TIMEOUT_MS,
        fullPrompt,
        onOutput
      );

      console.log(`[claude-code] Exit: ${result.exitCode}, stdout: ${result.fullText.length} chars, stderr: ${result.stderr.length} chars, files: ${result.filesChanged.length}`);
      if (result.fullText.length < 200) console.log(`[claude-code] Full output: ${result.fullText}`);
      if (result.stderr) console.log(`[claude-code] Stderr: ${result.stderr.slice(0, 500)}`);

      if (result.exitCode !== 0) {
        return {
          success: false,
          output: result.fullText || result.stderr || "Claude Code exited with error",
          filesChanged: result.filesChanged,
          error: result.stderr || `Exit code ${result.exitCode}`,
        };
      }

      // Check if output contains error indicators
      if (result.fullText.includes("Invalid API key") || result.fullText.includes("Error:")) {
        const errorLine = result.fullText.split("\n").find(l => l.includes("Error") || l.includes("Invalid")) || "Unknown error in output";
        return {
          success: false,
          output: result.fullText,
          filesChanged: result.filesChanged,
          error: errorLine,
        };
      }

      return {
        success: true,
        output: result.fullText,
        filesChanged: result.filesChanged,
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

  /**
   * Run claude with text output and stream stdout chunks in real time.
   */
  #runStreaming(args, cwd, timeout, stdinData, onOutput) {
    return new Promise((resolve, reject) => {
      const child = spawn("claude", args, {
        cwd,
        env: { ...process.env, CLAUDECODE: undefined, ANTHROPIC_API_KEY: undefined },
        stdio: ["pipe", "pipe", "pipe"],
        timeout,
      });

      let stderr = "";
      let fullText = "";
      const filesChanged = new Set();

      child.stdout.on("data", (d) => {
        const chunk = d.toString();
        fullText += chunk;
        if (onOutput) onOutput(chunk);

        // Try to detect file changes from output
        const editMatches = chunk.matchAll(/(?:Created|Modified|Edited|Wrote|Writing)\s+(?:file\s+)?[`"']?([^\s`"'\n]+)/gi);
        for (const m of editMatches) filesChanged.add(m[1]);
      });

      child.stderr.on("data", (d) => {
        stderr += d.toString();
      });

      child.on("close", (exitCode) => {
        resolve({
          exitCode,
          fullText: fullText.trim(),
          stderr: stderr.trim(),
          filesChanged: [...filesChanged],
        });
      });

      child.on("error", reject);

      if (stdinData) child.stdin.write(stdinData);
      child.stdin.end();
    });
  }

  /**
   * Simple run without streaming (for version check etc.)
   */
  #runSimple(args, cwd, timeout) {
    return new Promise((resolve, reject) => {
      const child = spawn("claude", args, {
        cwd,
        env: { ...process.env, CLAUDECODE: undefined, ANTHROPIC_API_KEY: undefined },
        stdio: ["pipe", "pipe", "pipe"],
        timeout,
      });
      let stdout = "";
      let stderr = "";
      child.stdout.on("data", (d) => (stdout += d.toString()));
      child.stderr.on("data", (d) => (stderr += d.toString()));
      child.on("close", (exitCode) => resolve({ exitCode, stdout: stdout.trim(), stderr: stderr.trim() }));
      child.on("error", reject);
      child.stdin.end();
    });
  }
}
