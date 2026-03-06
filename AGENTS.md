# AGENTS.md - AI Coding Environment Template

## Rol

Eres un desarrollador backend/infraestructor. Tu trabajo es crear un **template/Docker pre-configurado** para usuarios que quieren usar Claude Code sin saber programación.

## Objetivo

Crear una imagen/Dockerfile que incluya:
1. **.NET** - API backend
2. **SQL Server Express** - Base de datos
3. **Auth** - Sistema de autenticación
4. **Claude Code** - Pre-configurado
5. **Dashboard** - Interfaz web simple

## Stack Tecnológico

- **Backend:** .NET 8 + C#
- **DB:** SQL Server Express (Docker)
- **Auth:** Authelia o JWT simple
- **Frontend:** HTML simple o Vue.js
- **Docker:** Docker Compose para orquestar todo

## Estructura del Proyecto

```
ai-coding-environment/
├── docker-compose.yml
├── Dockerfile
├── src/
│   └── Api/
│       ├── Program.cs
│       ├── Controllers/
│       └── Models/
├── db/
│   └── init.sql
└── claude/
    └── AGENTS.md (este archivo)
```

## Requisitos Funcionales

### 1. API Backend (.NET)
- Endpoints para:
  - Login/Registro de usuarios
  - Gestión de ambientes
  - Stats del sistema
- Conexión a SQL Server
- JWT Auth

### 2. Base de Datos (SQL Server)
- Schema para usuarios
- Tabla de configuraciones por usuario
- Scripts de inicialización

### 3. Dashboard Web
- Página de login
- Dashboard simple con:
  - Estado del sistema
  - Acceso a Claude Code (botón/link)
  - Configuración básica
- served by Nginx

### 4. Claude Code Pre-configurado
- Instalado en el container
- Configuración básica
- Listo para usar

### 5. Docker Compose
- Orquestar todos los servicios
- Volúmenes persistentes
- Redes definidas

## Instrucciones de Desarrollo

1. **No escribir código pesado vos solo** - Usá las herramientas disponibles para generar código
2. **Priorizá funcionalidad sobre Bells & Whistles**
3. **Mantener simple** - El usuario final no sabe de tecnología
4. **Todo debe funcionar out-of-the-box**

## Cómo Ejecutar

1. `docker-compose up --build`
2. Acceder a `http://localhost:8080`
3. Login con admin/admin
4. Listo para usar

## Notas Importantes

- El usuario no sabe de CLI - todo via web
- Debe ser copy-pasteable a cualquier VPS
- Debe funcionar sin configuración adicional
- AGENTS.md mejorado debe estar incluido para que el usuario pueda mejorar el sistema

---

**Creado:** 2026-03-05
**Para:** Proyecto Template AI Coding Environment
