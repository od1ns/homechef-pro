# Skills

Esta carpeta contiene skills (en formato Claude Code / Cowork) reutilizables. Hay dos categorías:

- **Generales** — útiles para cualquier proyecto/conversación, no específicas de HomeChef Pro.
- **Específicas del stack** — destiladas de patrones probados durante el desarrollo de HomeChef Pro, aplicables a proyectos con la misma stack (.NET 10 + EF Core + Postgres + Flutter).

## Cómo instalar globalmente

Para que cualquier conversación pueda invocar estas skills (no solo dentro del repo):

**Windows (PowerShell)**
```powershell
$src = "C:\Users\Toor\source\repos\MasterClaude\HomeChef Pro\docs\skills"
# Carpeta destino: ajustar al path real de skills/user de tu Cowork
$dst = "$env:APPDATA\Claude\local-agent-mode-sessions\skills-plugin\<plugin-id>\<session-id>\skills\user"
New-Item -ItemType Directory -Force -Path $dst | Out-Null
Copy-Item -Recurse -Force "$src\*" $dst
```

**macOS / Linux**
```bash
src="$(pwd)/docs/skills"
dst="$HOME/.claude/skills/user"   # ajustar al path real
mkdir -p "$dst"
cp -r "$src"/* "$dst/"
```

Una vez copiadas, el frontmatter `name:` y `description:` con triggers se carga automáticamente y la skill se invoca cuando el usuario tipea uno de los triggers.

---

## Skills generales

### `premortem`
Imagina que un plan/launch/decisión **ya falló dentro de 6 meses** y trabaja hacia atrás para encontrar todas las razones por las que murió. Basada en la técnica de Gary Klein (Harvard Business Review). Antídoto contra la tendencia de los LLMs a ser agreables y optimistas.

Triggers principales: `premortem this`, `what could kill this`, `find the blind spots`, `stress test this plan`, `am i missing anything`.

Buena para: plan de producto, lanzamiento con dinero o reputación, hire importante, cambio de pricing, partnership, decisión donde el costo de equivocarse es alto.

### `bash-write-large-files`
Escribir archivos largos sin truncamientos silenciosos cuando el path tiene espacios. El `Edit` tool puede reportar OK y haber escrito menos. Usar `bash` heredoc o Python `read_text + write_text`, y verificar con `wc -l`, brace count exacto vía `tr -cd`, y `tail -3`.

Triggers: `escribir archivo grande`, `path con espacios`, `Edit tool truncated file`, `} expected en código que escribí completo`.

### `github-actions-trx-debug`
Cómo leer resultados de tests CI sin acceso admin al repo: confirmar el commit del run vía API pública, descargar artifacts `.trx` desde el browser, parsear con regex Python (no `xml.etree`), iterar sin pedir copy-paste de logs gigantes.

Triggers: `leer logs de CI en GitHub Actions`, `parsear .trx`, `GitHub API rate limit`, `tests integration que pasan local pero fallan en CI`.

---

## Skills específicas del stack (.NET + EF + Postgres)

### `dotnet-ef-postgres-integration-tests`
Patrones probados para tests integration con .NET, EF Core 10 y PostgreSQL evitando 5 pitfalls reales:
- Paralelismo de fixtures con Testcontainers (Collection Fixture compartido).
- Override de connection string que no gana sobre `appsettings.json` (`UseTestDatabase` extension).
- JWT validator que captura issuer/key en closure (`UseTestAuth` con `PostConfigure<JwtBearerOptions>`).
- Trigger `BEFORE UPDATE fn_touch_updated_at` + `Touch()` en C# (`SetAfterSaveBehavior(Ignore)`).
- `Include + Add to collection navigation + SaveChanges` que rompe con concurrency (`AsNoTracking + db.<DbSet>.Add()`).

Triggers: `integration tests with WebApplicationFactory`, `DbUpdateConcurrencyException sin concurrency token`, `tests con Testcontainers Postgres`, `expected 1 row affected, but actually affected 0`.

---

## Cuándo crear nuevas skills

Cuando termines una sesión donde:
- Tomó >5 iteraciones llegar a la fix correcta.
- La causa raíz no era obvia desde los síntomas.
- El patrón es reutilizable en otros proyectos con la misma stack o de forma general.

Documentar **anti-patterns** tanto como **patterns**: el "qué NO hacer" suele ser más valioso que el "qué hacer", porque previene futuras pérdidas de tiempo.

## Formato de cada skill

Cada SKILL.md debe tener:

```yaml
---
name: nombre-en-kebab-case
description: "Descripción larga con MANDATORY TRIGGERS y STRONG TRIGGERS — frases exactas que disparan la skill. Incluir condiciones de NO disparar para evitar falsos positivos."
---

# Título humano
Contenido en Markdown: cuándo usar, cuándo no, paso a paso, ejemplos, edge cases, anti-patterns.
```

El `description` debe ser explícito sobre los triggers porque el frontmatter es lo que el sistema usa para decidir cuándo invocar la skill automáticamente.
