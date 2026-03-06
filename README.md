# AI Coding Environment Dashboard

Un template completo para crear ambientes de desarrollo con Docker. Ideal para ofrecer como servicio o para tener tu propio entorno de coding configurado.

## ✨ Características

- **Dashboard web** con login y gestión de usuarios
- **API REST** con autenticación JWT (.NET 8)
- **Base de datos** SQL Server Express
- **Terminal web** integrada (Ubuntu con Claude Code, Node.js, Python, FFmpeg)
- **Nginx** como reverse proxy
- **100% Docker** - fácil de desplegar

## 🛠️ Stack

| Componente | Tecnología |
|------------|------------|
| Backend | .NET 8 + C# + Entity Framework Core |
| Base de datos | SQL Server 2022 Express |
| Frontend | HTML + CSS + JavaScript (vanilla) |
| Servidor web | Nginx |
| Workspace | Ubuntu 22.04 + Claude Code + Node.js 20 + Python 3 |
| Terminal web | ttyd |
| Contenedores | Docker + Docker Compose |

## 🚀 Cómo usarlo

### 1. Clonar el proyecto

```bash
git clone https://github.com/FrancoMal/ai-coding-environment.git
cd ai-coding-environment
```

### 2. Configurar variables de entorno

```bash
cp .env.example .env
```

### 3. Levantar todo

```bash
docker compose up --build -d
```

### 4. Acceder

- **Dashboard:** http://localhost:8080
- **Usuario:** admin
- **Contraseña:** admin123

## 📁 Estructura

```
ai-coding-environment/
├── docker-compose.yml     # Orquestación de servicios
├── .env.example          # Variables de entorno
├── src/Api/              # Backend .NET 8
│   ├── Controllers/      # Endpoints API
│   ├── Models/           # Modelos de datos
│   ├── Services/         # Lógica de negocio
│   └── Data/             # Entity Framework
├── web/                  # Frontend
│   ├── index.html        # Dashboard
│   ├── login.html        # Login
│   ├── css/              # Estilos
│   └── js/               # JavaScript
├── workspace/            # Container de desarrollo
├── db/                  # Scripts de base de datos
└── nginx/               # Configuración Nginx
```

## 🤖 Para agentes de IA

Este proyecto incluye `AGENTS.md` con instrucciones detalladas para que cualquier agente de IA (Claude Code, OpenCode, etc.) pueda trabajar en él sin necesidad de explicación adicional.

## 📝 Credenciales por defecto

| Servicio | Usuario | Contraseña |
|----------|---------|------------|
| Dashboard | admin | admin123 |
| SQL Server | sa | YourStrong@Passw0rd |

**⚠️ Cambiar contraseñas en producción**

## 🌐 Expansión

El template está diseñado para ser expandido facilmente:

- **Nueva página:** agregar en `web/js/dashboard.js`
- **Nueva tabla:** crear modelo C# + agregar en `db/init.sql`
- **Nuevo servicio Docker:** agregar en `docker-compose.yml`

## 📄 Licencia

MIT
