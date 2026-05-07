# Claude Code skills — HomeChef Pro

Cada skill es una guia operativa que Claude Code carga automaticamente
cuando se invoca con su nombre. La spec esta en `<skill>/SKILL.md`.

## Skills disponibles

| Skill | Para que sirve |
|---|---|
| `premortem` | Conducir un premortem estructurado: identificar fallos hipoteticos antes del lanzamiento, sus causas raiz, mitigaciones. |
| `security-audit` | Auditoria de seguridad de un servicio web siguiendo OWASP Top 10 + API Security Top 10. Genera un .md con findings categorizados (Critical/High/Medium/Low). |
| `bash-write-large-files` | Trucos para escribir archivos grandes desde bash sin issues de buffering / line endings. |
| `dotnet-ef-postgres-integration-tests` | Setup recomendado de tests de integracion con Testcontainers + EF Core + Postgres. |
| `github-actions-trx-debug` | Debugging de workflows GitHub Actions cuando los tests escupen TRX corruptos. |

## Como invocarlas en una sesion de Claude Code

```
> usa la skill premortem para revisar el deploy del piloto
> arranca la skill security-audit en /api/auth/*
```

Claude Code lee `SKILL.md` y aplica el procedimiento.

## Como compartir entre computadoras

Estas skills viven dentro del repo HomeChef Pro — al hacer `git clone`
en otro computador, ya se cargan automaticamente cuando abris el proyecto.

Para usarlas en CUALQUIER proyecto (no solo HomeChef Pro), hay un repo
separado tipo `od1ns/claude-skills` que contiene los mismos archivos +
podes clonarlo en `C:\Users\<user>\.claude\skills\` para que sean
globales del usuario, no del proyecto.

## Como agregar una skill nueva

Crear una carpeta `.claude/skills/<nombre>/SKILL.md` con frontmatter:

```yaml
---
name: nombre
description: descripcion corta (1 linea)
---
```

Despues el cuerpo en markdown con el procedimiento.
