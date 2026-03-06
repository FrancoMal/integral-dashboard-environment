# AI Coding Environment - Proyecto Template

## Descripción

**Producto:** Template/Docker pre-configurado para usuarios que quieren usar Claude Code/OpenCode sin saber programación.

El usuario compra/alquila el ambiente ya configurado con:
- Base de datos
- Auth
- Claude Code pre-configurado
- Todo listo para usar

## Stack Tecnológico

### Core
- **.NET** - Backend/API
- **SQL Server Express** - Base de datos
- **Docker + Docker Compose** - Contenedores

### Frontend
- **Refine** o **Retool** - Dashboard admin
- O simple HTML/Vue con Nginx

### Herramientas Preinstaladas
- Claude Code / OpenCode
- Git
- Node.js
- Python
- FFmpeg

### Extras
- Web Terminal: Gotty o Wetty
- Auth: Authelia o similar

## Arquitectura

```
┌─────────────────────────────────────┐
│         VPS / VM                    │
│  ┌─────────────────────────────┐    │
│  │   Docker Compose           │    │
│  │  ┌─────────┐ ┌─────────┐  │    │
│  │  │ .NET    │ │ SQL     │  │    │
│  │  │ API     │ │ Server  │  │    │
│  │  └─────────┘ └─────────┘  │    │
│  │  ┌─────────┐ ┌─────────┐  │    │
│  │  │ Claude  │ │ Nginx   │  │    │
│  │  │ Code   │ │ Proxy   │  │    │
│  │  └─────────┘ └─────────┘  │    │
│  └─────────────────────────────┘    │
└─────────────────────────────────────┘
```

## Uso

1. Comprar VPS
2. Instalar Docker
3. Ejecutar docker-compose up
4. Listo - ambiente funcionando

## Estado Actual

- [x] VM Ubuntu configurada (100.104.207.12)
- [x] Docker instalado
- [x] Estructura básica creada
  - docker-compose.yml
  - web/index.html
  - db/init.sql
- [ ] API .NET (en progreso)
- [ ] Probar docker-compose up

## Cómo Ejecutar

```bash
cd ai-coding-environment
docker-compose up --build
```

Acceder a:
- Web: http://localhost:8080
- API: http://localhost:5000
- SQL Server: localhost:1433

## Ubicación Proyecto

`C:\Users\Usuario\clawd\projects\ai-coding-environment`

## Notas

- El AGENTS.md debe ser amigable para usuarios no técnicos
- El usuario final no necesita saber CLI
- Todo accesible via web dashboard
