/**
 * Base provider interface.
 * Every agent provider must implement these methods.
 *
 * execute() receives:
 *   - prompt: string — the task description / implementation plan
 *   - cwd: string — the repo directory to work in
 *   - context: object — extra info (project name, language, files, etc.)
 *
 * execute() must return:
 *   - success: boolean
 *   - output: string — summary of what was done
 *   - filesChanged: string[] — list of files modified
 *   - error: string | null
 */
export class BaseProvider {
  constructor(name) {
    this.name = name;
  }

  async isAvailable() {
    throw new Error(`${this.name}: isAvailable() not implemented`);
  }

  async execute({ prompt, cwd, context }) {
    throw new Error(`${this.name}: execute() not implemented`);
  }
}
