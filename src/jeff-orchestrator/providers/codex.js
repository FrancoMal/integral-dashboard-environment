import { spawn } from "node:child_process";
import { BaseProvider } from "./base.js";

const TIMEOUT_MS = 300_000;

export class CodexProvider extends BaseProvider {
  constructor() {
    super("codex");
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
    const fullPrompt = `${prompt}\n\nProject: ${context?.projectName || "unknown"}, Language: ${context?.language || "unknown"}`;

    try {
      // codex --quiet --auto-edit to run non-interactively
      const result = await this.#run(
        ["--quiet", "--auto-edit"],
        cwd,
        TIMEOUT_MS,
        fullPrompt
      );

      return {
        success: result.exitCode === 0,
        output: result.stdout || result.stderr,
        filesChanged: [],
        error: result.exitCode !== 0 ? result.stderr : null,
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

  #run(args, cwd, timeout, stdinData = null) {
    return new Promise((resolve, reject) => {
      const child = spawn("codex", args, {
        cwd,
        stdio: ["pipe", "pipe", "pipe"],
        timeout,
      });

      let stdout = "";
      let stderr = "";

      child.stdout.on("data", (d) => (stdout += d.toString()));
      child.stderr.on("data", (d) => (stderr += d.toString()));
      child.on("close", (exitCode) => resolve({ exitCode, stdout: stdout.trim(), stderr: stderr.trim() }));
      child.on("error", reject);

      if (stdinData) child.stdin.write(stdinData);
      child.stdin.end();
    });
  }
}
