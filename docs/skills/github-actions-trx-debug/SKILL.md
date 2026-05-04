---
name: github-actions-trx-debug
description: Cómo leer resultados de tests CI en GitHub Actions sin acceso admin al repo. Obtiene metadata vía API pública, parsea archivos .trx (Visual Studio Test Results) descargados como artifacts, y diagnostica fallos sin pedir copy-paste al usuario.
triggers:
  - leer logs de CI en GitHub Actions
  - parsear .trx (Visual Studio Test Results)
  - "GitHub API rate limit"
  - tests integration que pasan local pero fallan en CI
  - el repo es privado o no tengo token de admin
---

# Leer resultados de CI GitHub Actions sin admin auth

## El problema

Para diagnosticar tests rotos en CI normalmente se necesita:
- Ver el log raw del job → requiere admin auth (`/repos/{}/jobs/{}/logs` devuelve 403 sin token).
- Ver el resumen de tests → ídem.

Pero los **artifacts** del workflow (archivos subidos con `actions/upload-artifact@v4`) **sí** se pueden descargar desde el navegador del usuario logueado, sin pasar por API admin.

Si el workflow ya sube los `.trx` (Visual Studio Test Results) como artifact, podemos parsearlos sin tocar el log.

## Setup en el workflow

`.github/workflows/ci.yml`:
```yaml
- name: Run tests
  run: dotnet test --logger "trx;LogFileName=tests.trx"

- name: Upload test results
  if: always()
  uses: actions/upload-artifact@v4
  with:
    name: integration-test-results
    path: '**/TestResults/**/*.trx'
```

## Lo que sí está disponible vía API pública

```
GET https://api.github.com/repos/{owner}/{repo}/actions/runs/{run_id}
GET https://api.github.com/repos/{owner}/{repo}/actions/runs/{run_id}/jobs
GET https://api.github.com/repos/{owner}/{repo}/actions/runs/{run_id}/artifacts
GET https://api.github.com/repos/{owner}/{repo}/check-runs/{check_run_id}/annotations
```

Estos devuelven JSON sin auth para repos públicos. Útiles para:
- Confirmar que el commit del run incluye tu fix (`head_sha`).
- Ver qué jobs pasaron / fallaron y duración.
- Obtener el `archive_download_url` del artifact (este último SÍ requiere auth — el usuario lo descarga desde el browser).

## Workflow del asistente

1. **Confirmar que el run es del commit correcto**:
   ```
   GET /repos/.../actions/runs/{run_id}  → leer head_sha
   ```
   Comparar con `git log -1 --format=%H` local.

2. **Pedir al usuario que descargue el artifact**:
   ```
   URL: https://github.com/{owner}/{repo}/actions/runs/{run_id}/artifacts/{artifact_id}
   "Click en esa URL desde el browser, descargá el zip, descomprimílo en <ruta>".
   ```

3. **Parsear el `.trx` con Python** (los `.trx` son XML):
   ```python
   import re
   content = open(TRX_PATH, encoding='utf-8', errors='replace').read()

   # Resumen
   m = re.search(r'<Counters[^/]*/>', content)
   print(m.group(0))  # total/passed/failed/...

   # Tests fallidos con mensaje + stack
   for ur in re.finditer(r'<UnitTestResult[^>]*outcome="Failed"[^>]*>.*?</UnitTestResult>', content, re.DOTALL):
       block = ur.group(0)
       name = re.search(r'testName="([^"]*)"', block).group(1)
       msg = re.search(r'<Message>(.*?)</Message>', block, re.DOTALL)
       st = re.search(r'<StackTrace>(.*?)</StackTrace>', block, re.DOTALL)
       print(f"FAIL: {name}")
       if msg: print(msg.group(1))
       if st:
           # Decodificar entidades XML
           stack = st.group(1).replace('&#xD;', '\n').replace('&#xA;', '\n').replace('&amp;', '&')
           # Mostrar solo lineas relevantes (las que contienen el namespace del proyecto)
           for line in stack.split('\n'):
               if 'MyCompany' in line and 'cs:line' in line:
                   print('  ' + line.strip())
   ```

   No usar `xml.etree.ElementTree` directo: los `.trx` a veces tienen caracteres mal escapados que rompen el parser estricto. Regex sobre el string es más robusto.

4. **Iterar sin pedir más al usuario**: cada vez que el usuario sube un nuevo run, repetir solo el paso 2 (descarga del artifact nuevo) — el resto se hace en bash automáticamente.

## Anti-patrones

- ❌ Pedir al usuario que copie/pegue el log entero de CI: es ruidoso, se trunca, y obliga al usuario a buscar en una página enorme.
- ❌ Probar `GET .../jobs/{job_id}/logs` sin token: devuelve 403 "Must have admin rights".
- ❌ Hacer `WebFetch` repetido sobre el mismo run: GitHub rate-limita a ~60 req/h sin auth.
- ❌ Confiar en el HTML de `github.com/.../actions/runs/...`: los logs se cargan vía JavaScript después del page render, así que `WebFetch` solo trae el shell HTML.

## Patterns útiles

### Verificar que el run que estás analizando es del commit correcto

```python
import subprocess
local_head = subprocess.check_output(['git', 'rev-parse', 'HEAD']).decode().strip()
# Vs el head_sha del API response
```

### Comparar dos runs consecutivos

Si el fix del commit N+1 supuestamente arregló algo, comparar:
- TRX del run N: X fails con error A
- TRX del run N+1: Y fails con error B

Si X == Y, la fix no aplicó. Si X > Y, la fix aplicó parcialmente. Verificar el `head_sha` de ambos para confirmar que NO estás comparando dos runs del mismo commit.

### Extraer solo la cascada relevante de un stack trace

Los stacks de EF Core son largos. Filtrar a las líneas del proyecto:
```python
for line in stack.split('\n'):
    if any(ns in line for ns in ['HomeChefPro', 'MyCompany']) and 'cs:line' in line:
        print('  ' + line.strip())
```

## Ejemplo completo (script reutilizable)

```python
#!/usr/bin/env python3
"""Parsea un .trx y reporta fallos formateados."""
import re, sys
TRX = sys.argv[1]
content = open(TRX, encoding='utf-8', errors='replace').read()

m = re.search(r'<Counters[^/]*/>', content)
print(f"RESUMEN: {m.group(0) if m else 'no encontrado'}")
print()

fails = list(re.finditer(
    r'<UnitTestResult[^>]*outcome="Failed"[^>]*>.*?</UnitTestResult>',
    content, re.DOTALL))
print(f"Tests fallidos: {len(fails)}\n")

for ur in fails:
    block = ur.group(0)
    name = re.search(r'testName="([^"]*)"', block).group(1).split('.')[-1]
    msg = re.search(r'<Message>(.*?)</Message>', block, re.DOTALL)
    print(f"--- {name} ---")
    if msg:
        print(msg.group(1)[:400])
    print()
```

Llamar: `python3 parse-trx.py /path/to/integration-tests.trx`.
