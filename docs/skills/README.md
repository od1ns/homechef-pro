# Skills extraídas de este proyecto

Esta carpeta contiene skills (en formato Claude Code / Cowork) destiladas de patrones probados durante el desarrollo de HomeChef Pro. Son reutilizables en cualquier proyecto con la misma stack.

## Cómo usar estas skills

### Opción 1 — Como referencia interna del proyecto
Los `SKILL.md` ya están commiteados en este repo. Cualquier asistente que entre puede leerlos para entender los patrones aplicados acá.

### Opción 2 — Instalar globalmente
Para que cualquier conversación (incluso fuera de este proyecto) las pueda invocar como skill:

```powershell
# Windows
$dst = "$env:APPDATA\Claude\local-agent-mode-sessions\skills-plugin\<plugin-id>\<session-id>\skills\user"
New-Item -ItemType Directory -Force -Path $dst
Copy-Item -Recurse "docs\skills\*" $dst
```

```bash
# macOS/Linux
dst="$HOME/.claude/skills/user"  # ajustar al path real
mkdir -p "$dst"
cp -r docs/skills/* "$dst/"
```

## Skills disponibles

### `dotnet-ef-postgres-integration-tests`
Patrones probados para tests integration con .NET, EF Core 10 y PostgreSQL evitando 5 pitfalls reales:
- Paralelismo de fixtures con Testcontainers (Collection Fixture compartido).
- Override de connection string que no gana sobre `appsettings.json` (`UseTestDatabase` extension).
- JWT validator que captura issuer/key en closure (`UseTestAuth` con `PostConfigure<JwtBearerOptions>`).
- Trigger `BEFORE UPDATE fn_touch_updated_at` + `Touch()` en C# (`SetAfterSaveBehavior(Ignore)`).
- `Include + Add to collection navigation + SaveChanges` que rompe con concurrency (`AsNoTracking + db.<DbSet>.Add()`).

### `github-actions-trx-debug`
Cómo leer resultados de tests CI sin acceso admin al repo:
- Confirmar el commit del run vía API pública.
- Descargar artifacts `.trx` desde el browser.
- Parsear los `.trx` con regex robusto en Python (no `xml.etree` que falla con XML mal escapado).
- Iterar sin pedir copy-paste de logs gigantes al usuario.

### `bash-write-large-files`
Cómo escribir archivos largos sin truncamientos silenciosos cuando el path tiene espacios. El `Edit` tool puede reportar OK y haber escrito menos:
- Usar `bash` heredoc o Python `read_text + write_text` en lugar del Edit tool.
- Verificar con `wc -l`, brace count exacto (`tr -cd '{' | wc -c`), y `tail` después de cada escritura.
- Detectar y diagnosticar truncamientos (síntomas: errores `} expected` en la última línea).

## Cuándo crear nuevas skills

Cuando termines una sesión donde:
- Tomó >5 iteraciones llegar a la fix correcta.
- La causa raíz no era obvia desde los síntomas.
- El patrón es reutilizable en otros proyectos con la misma stack.

Documentar **anti-patterns** tanto como **patterns**: el "qué NO hacer" suele ser más valioso que el "qué hacer", porque previene futuras pérdidas de tiempo.
