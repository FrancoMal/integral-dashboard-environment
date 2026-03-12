import { ClaudeCodeProvider } from "./claude-code.js";
import { CodexProvider } from "./codex.js";

const providers = {
  "claude-code": () => new ClaudeCodeProvider(),
  codex: () => new CodexProvider(),
  // Add new providers here:
  // "openclaw": () => new OpenClawProvider(),
};

export function createProvider(name) {
  const factory = providers[name];
  if (!factory) {
    throw new Error(
      `Unknown provider: "${name}". Available: ${Object.keys(providers).join(", ")}`
    );
  }
  return factory();
}

export function listProviders() {
  return Object.keys(providers);
}
