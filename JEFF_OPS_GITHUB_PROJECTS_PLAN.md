# Jeff Ops — Roadmap de Proyectos activos desde GitHub

Última actualización: 2026-03-11

## Objetivo del producto

Hacer que **GitHub sea la fuente real de repositorios** del usuario y que **Proyectos activos** sea la cartera de repos seleccionados para trabajo continuo dentro de Jeff Ops.

Flujo objetivo completo:

1. Traer repos reales de GitHub del usuario
2. Seleccionar/importar repos a **Proyectos activos**
3. Ver **detalle del proyecto activo**
4. Tener botón **"Analizar proyecto"**
5. Mostrar **recomendaciones**
6. Permitir **checkbox por recomendación**
7. Permitir **notas por recomendación**
8. Pasar recomendaciones seleccionadas al **backlog**
9. Evolucionar a análisis más profundo, historial y actividades

---

## Estado general

### Ya hecho / usable
La **fase base del flujo** ya está implementada y validada funcionalmente.

### Pendiente
Queda la **fase profunda/inteligente** del análisis y la capa de historial/actividades.

---

## Roadmap por fases

---

## Fase 0 — Base limpia y entorno estable

### Objetivo
Tener una base de trabajo limpia, persistente y no atada a `/tmp`.

### Estado
✅ **Hecha**

### Qué se resolvió
- Se dejó de depender de `/tmp/jeffops-rework`
- Se recreó una base persistente en:
  - `/root/worktrees/jeffops-rework`
- Se validó que la rama limpia usable es:
  - `rework/proyectos-activos`
- Se verificó que el build del frontend no estaba estructuralmente roto

### Resultado
Terreno estable para iterar sin perder trabajo por carpetas temporales.

---

## Fase 1 — GitHub como fuente real de repos

### Objetivo
Que Jeff Ops deje de usar proyectos genéricos desconectados y pase a trabajar con **repositorios reales de GitHub**.

### Estado
✅ **Hecha**

### Qué quedó implementado
- Integración real con GitHub vía API autenticada
- Listado de repos reales del usuario
- Vista desde Proyectos activos con opción de:
  - **Importar**
  - **Reimportar**
- Creación/actualización del proyecto activo vinculado al repo

### Resultado visible
GitHub ya funciona como fuente real de repositorios.

---

## Fase 2 — Importación a Proyectos activos

### Objetivo
Convertir repos elegidos de GitHub en **proyectos activos** dentro de Jeff Ops.

### Estado
✅ **Hecha**

### Qué quedó implementado
- Importación desde repos GitHub a proyectos activos
- Reimportación para refrescar datos
- Vinculación del proyecto con metadata del repo

### Resultado visible
Los repos elegidos ya entran al sistema como proyectos activos reales.

---

## Fase 3 — Detalle de proyecto activo

### Objetivo
Mostrar una vista clara del proyecto activo importado.

### Estado
✅ **Hecha**

### Qué quedó implementado
Detalle visible con datos como:
- fuente
- repo
- lenguaje
- branch
- stars
- issues
- link a GitHub

### Resultado visible
Cada proyecto activo ya tiene una ficha entendible y útil.

---

## Fase 4 — Analizar proyecto (versión honesta base)

### Objetivo
Agregar el botón **"Analizar proyecto"** sin vender humo.

### Estado
✅ **Hecha** (base)

### Qué quedó implementado
- Botón **"Analizar proyecto"**
- Estado honesto del análisis
- Análisis base real sobre señales del repo, por ejemplo:
  - README
  - CI
  - tests
  - docs
  - issues
  - metadata/estructura general

### Qué NO hace todavía
- No hace análisis profundo de código completo
- No recorre todavía el repo como lo haría una revisión fuerte de ingeniería

### Resultado visible
Hay análisis usable y sincero, pero todavía no profundo.

---

## Fase 5 — Recomendaciones accionables

### Objetivo
Traducir el análisis en sugerencias utilizables.

### Estado
✅ **Hecha** (base)

### Qué quedó implementado
- Recomendaciones visibles por proyecto
- Lista clara posterior al análisis

### Resultado visible
El proyecto ya devuelve sugerencias accionables iniciales.

---

## Fase 6 — Selección y notas sobre recomendaciones

### Objetivo
Permitir trabajar activamente sobre las recomendaciones.

### Estado
✅ **Hecha**

### Qué quedó implementado
- **Checkbox** por recomendación
- **Nota editable** por recomendación

### Resultado visible
Las recomendaciones ya se pueden revisar y preparar manualmente antes de convertirlas en trabajo.

---

## Fase 7 — Pasar recomendaciones al backlog

### Objetivo
Convertir recomendaciones elegidas en trabajo real dentro del sistema.

### Estado
✅ **Hecha**

### Qué quedó implementado
- Botón para pasar seleccionadas al backlog
- Creación de tareas reales en `KanbanTasks`

### Resultado visible
Las sugerencias ya pueden convertirse en backlog real de ejecución.

---

## Fase 8 — Validación funcional real

### Objetivo
Asegurar que no sea solo UI linda, sino flujo operativo real.

### Estado
✅ **Hecha**

### Qué se validó
- `docker compose build api web`
- runtime real
- login OK con `admin / admin123`
- API de GitHub devolviendo repos reales
- importación de repos OK
- detalle del proyecto OK
- análisis base OK
- creación de backlog OK desde recomendaciones

### Resultado
La fase base no quedó en humo: quedó validada funcionalmente.

---

## Fase 9 — Análisis profundo de proyecto

### Objetivo
Subir de un análisis base a un análisis más serio de ingeniería.

### Estado
🟡 **Pendiente**

### Qué debería incluir
- Leer árbol del repo
- Leer archivos clave
- Detectar stack real
- Detectar framework y tooling
- Detectar convenciones del proyecto
- Mejorar calidad y especificidad de recomendaciones

### Resultado esperado
Recomendaciones mucho más inteligentes y situadas al repo real.

---

## Fase 10 — Historial de análisis

### Objetivo
Persistir el paso del tiempo y no tratar cada análisis como si fuera el primero.

### Estado
🟡 **Pendiente**

### Qué debería incluir
- Historial por proyecto
- Fecha de análisis
- Estado del análisis
- Qué recomendaciones aparecieron
- Qué cambió entre análisis

### Resultado esperado
Poder ver evolución del proyecto y no solo una foto aislada.

---

## Fase 11 — Actividades / trazabilidad de JeffVps

### Objetivo
Mostrar qué estuvo haciendo JeffVps con cada proyecto.

### Estado
🟡 **Pendiente**

### Qué debería incluir
- actividades en curso
- actividades terminadas
- análisis disparados
- imports/reimports
- trabajos ejecutados por herramientas/agentes
- posible tab de **Actividades** en Jeff Ops

### Resultado esperado
Visibilidad operativa de lo que pasa detrás del sistema.

---

## Fase 12 — Integración más fuerte con agentes/harnesses

### Objetivo
Hacer que el sistema no solo analice sino que también conecte mejor con ejecución real.

### Estado
🔴 **Pendiente / bloqueada parcialmente por tooling**

### Qué debería incluir
- disparar análisis más profundos con herramientas externas
- idealmente usar Claude Code / ACP cuando el runtime quede estable
- registrar esa actividad en Jeff Ops

### Bloqueo actual
El entorno ACP quedó más encaminado, pero **Claude CLI tenía auth vencida** y la prueba end-to-end de Claude por ACP no quedó cerrada todavía.

---

## Resumen ejecutivo

## Fases cerradas
- ✅ Fase 0 — Base limpia y estable
- ✅ Fase 1 — GitHub como fuente real de repos
- ✅ Fase 2 — Importación a Proyectos activos
- ✅ Fase 3 — Detalle de proyecto activo
- ✅ Fase 4 — Analizar proyecto (base honesta)
- ✅ Fase 5 — Recomendaciones visibles (base)
- ✅ Fase 6 — Checkbox + notas
- ✅ Fase 7 — Pasar al backlog
- ✅ Fase 8 — Validación funcional real

## Fases pendientes
- 🟡 Fase 9 — Análisis profundo del repo/código
- 🟡 Fase 10 — Historial de análisis
- 🟡 Fase 11 — Actividades / trazabilidad de JeffVps
- 🔴 Fase 12 — Integración más fuerte con Claude Code / ACP / herramientas

---

## Qué ya se puede decir hoy

Hoy Jeff Ops ya permite, de forma real:
- usar GitHub como fuente de repos
- importar proyectos activos
- ver detalle del proyecto
- analizarlo de forma base y honesta
- obtener recomendaciones
- elegirlas, anotarlas y pasarlas al backlog

Eso significa que la **fase base del producto está cumplida**.

---

## Próximo paso recomendado

### Opción A — seguir por producto
Implementar **Fase 9: análisis profundo del repo**

Porque es el salto que más valor le agrega al usuario sobre lo ya construido.

### Opción B — seguir por observabilidad
Implementar **Fase 10 + 11: historial + actividades**

Porque mejora mucho la trazabilidad y hace a Jeff Ops más entendible en el día a día.

### Opción C — seguir por tooling
Cerrar bien **Fase 12: Claude Code / ACP / ejecución integrada**

Porque eso habilita análisis/acciones más potentes, pero depende de terminar de estabilizar el entorno de tooling.

---

## Recomendación final

Orden recomendado para seguir:

1. **Fase 9 — Análisis profundo**
2. **Fase 10 — Historial de análisis**
3. **Fase 11 — Actividades**
4. **Fase 12 — Integración profunda con harnesses/agentes**

Ese orden mantiene foco en valor de producto antes que en complejidad de infraestructura.
