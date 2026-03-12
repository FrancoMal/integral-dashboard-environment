import { createServer } from "node:http";
import { spawn } from "node:child_process";

const PORT = 4500;
const MAX_FILE_CHARS = 8000;
const MAX_TOTAL_CHARS = 60000;
const TIMEOUT_MS = 120_000;

function buildPrompt(repoName, language, files) {
  return `Sos un analista de codigo senior. Analiza los siguientes archivos del repositorio "${repoName}" (lenguaje principal: ${language || "desconocido"}).

Tu trabajo es dar recomendaciones concretas y accionables sobre el codigo real. No recomendaciones genericas. Cada recomendacion debe mencionar el archivo y la linea o seccion especifica.

Categorias posibles: seguridad, calidad, performance, arquitectura, mantenibilidad, bugs
Prioridades: 3 (critica), 2 (importante), 1 (mejora)

RESPONDE UNICAMENTE con un JSON array valido, sin markdown, sin explicacion extra. Cada elemento:
{"title": "string corto", "notes": "explicacion con archivo y linea", "category": "string", "priority": number}

Maximo 10 recomendaciones. Si el codigo esta bien, devuelve un array vacio [].

ARCHIVOS:
${files.map((f) => `--- ${f.path} ---\n${f.content}`).join("\n\n")}

JSON:`;
}

function buildFeaturesPrompt(repoName, language, files, readme, description) {
  const fileList = files.map((f) => `--- ${f.path} ---\n${f.content}`).join("\n\n");

  return `Sos un product manager e ingeniero senior. Analiza el repositorio "${repoName}" y propone features nuevas que tengan sentido para este proyecto.

Descripcion del proyecto: ${description || "No disponible"}
Lenguaje principal: ${language || "desconocido"}
${readme ? `README:\n${readme.slice(0, 3000)}` : "Sin README"}

ARCHIVOS DEL PROYECTO:
${fileList}

INSTRUCCIONES:
- Propone entre 3 y 7 features nuevas que se acoplen naturalmente al proyecto actual
- Cada feature debe ser concreta, implementable y util para el usuario
- La implementacion debe ser lo suficientemente detallada para que un agente de IA (Claude Code) pueda ejecutarla directamente
- Incluir que archivos crear o modificar
- Pensar en el stack actual del proyecto y proponer features coherentes con el
- No proponer cosas que ya estan implementadas
- Complejidad: "baja" (1-2 archivos, < 1 hora), "media" (3-5 archivos, 1-3 horas), "alta" (6+ archivos, > 3 horas)

RESPONDE UNICAMENTE con un JSON array valido, sin markdown, sin explicacion extra. Cada elemento:
{"title": "nombre corto de la feature", "description": "que hace y por que es util para el usuario", "implementation": "pasos detallados de implementacion, mencionando archivos especificos a crear/modificar, funciones a agregar, endpoints, componentes de UI, etc. Escrito como si fuera un prompt para Claude Code.", "filesToModify": "lista de archivos separados por coma", "complexity": "baja|media|alta"}

JSON:`;
}

function runClaude(prompt) {
  return new Promise((resolve, reject) => {
    const child = spawn("claude", ["-p", "--output-format", "text"], {
      env: { ...process.env, CLAUDECODE: undefined },
      stdio: ["pipe", "pipe", "pipe"],
      timeout: TIMEOUT_MS,
    });

    let stdout = "";
    let stderr = "";

    child.stdout.on("data", (d) => (stdout += d.toString()));
    child.stderr.on("data", (d) => (stderr += d.toString()));

    child.on("close", (code) => {
      if (code !== 0) {
        reject(new Error(`claude exit ${code}: ${stderr}`));
      } else {
        resolve(stdout.trim());
      }
    });

    child.on("error", reject);

    child.stdin.write(prompt);
    child.stdin.end();
  });
}

function parseJSON(text) {
  // Try to extract JSON array from response
  const arrayMatch = text.match(/\[[\s\S]*\]/);
  if (arrayMatch) {
    try {
      const parsed = JSON.parse(arrayMatch[0]);
      if (Array.isArray(parsed)) return parsed;
    } catch {}
  }
  return [];
}

function readBody(req) {
  return new Promise((resolve, reject) => {
    let body = "";
    req.on("data", (chunk) => (body += chunk));
    req.on("end", () => {
      try {
        resolve(JSON.parse(body));
      } catch (e) {
        reject(new Error("Invalid JSON body"));
      }
    });
    req.on("error", reject);
  });
}

const server = createServer(async (req, res) => {
  // Health check
  if (req.method === "GET" && req.url === "/health") {
    res.writeHead(200, { "Content-Type": "application/json" });
    res.end(JSON.stringify({ status: "ok" }));
    return;
  }

  // Analyze endpoint
  if (req.method === "POST" && req.url === "/analyze") {
    try {
      const { repoName, language, files } = await readBody(req);

      if (!files || !Array.isArray(files) || files.length === 0) {
        res.writeHead(400, { "Content-Type": "application/json" });
        res.end(JSON.stringify({ error: "No files provided" }));
        return;
      }

      // Truncate files to fit within limits
      let totalChars = 0;
      const trimmedFiles = [];
      for (const f of files) {
        if (totalChars >= MAX_TOTAL_CHARS) break;
        const content = (f.content || "").slice(0, MAX_FILE_CHARS);
        totalChars += content.length;
        trimmedFiles.push({ path: f.path, content });
      }

      const prompt = buildPrompt(repoName, language, trimmedFiles);

      console.log(
        `[analyze] repo=${repoName} files=${trimmedFiles.length} prompt_len=${prompt.length}`
      );

      const raw = await runClaude(prompt);
      const recommendations = parseJSON(raw);

      console.log(
        `[analyze] repo=${repoName} recommendations=${recommendations.length}`
      );

      res.writeHead(200, { "Content-Type": "application/json" });
      res.end(JSON.stringify({ recommendations, raw: raw.slice(0, 500) }));
    } catch (err) {
      console.error(`[analyze] error: ${err.message}`);
      res.writeHead(500, { "Content-Type": "application/json" });
      res.end(JSON.stringify({ error: err.message, recommendations: [] }));
    }
    return;
  }

  // Features endpoint
  if (req.method === "POST" && req.url === "/features") {
    try {
      const { repoName, language, files, readme, description } =
        await readBody(req);

      let totalChars = 0;
      const trimmedFiles = [];
      for (const f of files || []) {
        if (totalChars >= MAX_TOTAL_CHARS) break;
        const content = (f.content || "").slice(0, MAX_FILE_CHARS);
        totalChars += content.length;
        trimmedFiles.push({ path: f.path, content });
      }

      const prompt = buildFeaturesPrompt(
        repoName,
        language,
        trimmedFiles,
        readme,
        description
      );

      console.log(
        `[features] repo=${repoName} files=${trimmedFiles.length} prompt_len=${prompt.length}`
      );

      const raw = await runClaude(prompt);
      const features = parseJSON(raw);

      console.log(`[features] repo=${repoName} features=${features.length}`);

      res.writeHead(200, { "Content-Type": "application/json" });
      res.end(JSON.stringify({ features, raw: raw.slice(0, 500) }));
    } catch (err) {
      console.error(`[features] error: ${err.message}`);
      res.writeHead(500, { "Content-Type": "application/json" });
      res.end(JSON.stringify({ error: err.message, features: [] }));
    }
    return;
  }

  res.writeHead(404);
  res.end("Not found");
});

server.listen(PORT, "0.0.0.0", () => {
  console.log(`claude-analyzer listening on :${PORT}`);
});
