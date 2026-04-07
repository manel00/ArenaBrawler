---
trigger: manual
---

# REGLA MANDATORIA

**ANTES DE CUALQUIER ACCIÓN EN ESTE PROYECTO:**

1. **LEER Y CONSULTAR SIEMPRE** el archivo `PROJECT_RULES.md` en la raíz del proyecto
2. **VERIFICAR CONEXIÓN MCP UNITY** ejecutando `telemetry_ping` antes de cualquier operación MCP
3. **BUSCAR LA SKILL NECESARIA** utilizando la herramienta `skill` para identificar el workflow o skill especializado que corresponde a la tarea a realizar, asegurando el uso del proceso correcto
4. **CONSULTAR DOCUMENTACIÓN DE UNITY** utilizando las herramientas MCP disponibles para realizar el trabajo de manera perfecta y conforme a las mejores prácticas oficiales
5. **NUNCA** usar MCP de Playwright para probar el juego - el juego está en Unity, usar únicamente herramientas MCP de Unity
6. **NUNCA** abrir más de 1 página con MCP de Playwright - limitar a una única pestaña para optimizar recursos
7. **NUNCA** usar `CreatePrimitive` en código de Unity - usar exclusivamente assets FBX y Prefabs reales del proyecto
8. **NUNCA** realizar cambios que violen las reglas establecidas en `PROJECT_RULES.md`
9. **JAMÁS** eliminar nada del juego - si algo debe ser eliminado, debe hacerse bajo supervisión humana explícita

**PARA CONECTARSE A UNITY:**

1. **PRIMERO:** Intentar usar Unity MCP tools directamente (`mcp2_manage_scene`, `mcp2_find_gameobjects`, etc.)
2. **SI FALLA** (transport error/connection refused):
   - Usar UnitySkills REST API vía Python
   - Comando: `.\scripts\venv\Scripts\python.exe scripts\unity_skills.py <skill_name>`
   - O importar: `import scripts.unity_skills as us`
   - Listar instancias: `.\scripts\venv\Scripts\python.exe scripts\unity_skills.py --list-instances`

Las reglas del proyecto son **INQUEBRANTABLES** y deben consultarse antes de cualquier modificación.
