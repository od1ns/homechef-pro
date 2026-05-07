---
name: bash-write-large-files
description: Cómo escribir archivos largos de forma confiable cuando el path tiene espacios o caracteres especiales. El Edit tool puede truncar archivos silenciosamente con paths como "C:\Users\X\My Project\". Usar bash heredoc o Python en su lugar, y siempre verificar tamaño + braces post-escritura.
triggers:
  - escribir archivo grande
  - path con espacios
  - "Edit tool truncated file"
  - archivo se cortó a la mitad
  - "} expected" en código que escribí completo
---

# Escribir archivos largos sin truncamientos silenciosos

## El problema

El `Edit` tool y a veces el `Write` tool pueden truncar archivos cuando:
- El path contiene espacios (caso típico: "C:\Users\X\My Project\subfolder\file.cs").
- El archivo es grande (>200 líneas).
- Se invoca el tool varias veces consecutivas en la misma conversación.

El truncamiento es silencioso: el tool reporta "OK", pero el archivo en disco queda cortado a la mitad de una línea (a veces en medio de un identificador como `var dir = new DirectoryInfo(AppC`).

El siguiente build falla con errores crípticos como `} expected`, `; expected` o `unexpected end of input`.

## Cuándo usar esta skill

- Vas a escribir un archivo de >100 líneas.
- El path contiene espacios o caracteres no-ASCII.
- Detectaste que un edit anterior truncó un archivo (síntoma: build error en la línea exacta donde termina prematuro).

## Mecanismo correcto

### Para archivos nuevos: bash heredoc

```bash
cat > "/path/with spaces/MyFile.cs" << 'EOF'
using System;
namespace MyNs;
public class MyClass
{
    public void DoStuff() { }
}
EOF
```

Ventajas:
- `'EOF'` (con quotes) evita interpolación de variables.
- Funciona con cualquier longitud.
- Es atómico: o se escribe todo o nada.

### Para modificar un archivo existente: Python read+write

```bash
python3 << 'PYEOF'
from pathlib import Path
p = Path("/path/with spaces/file.cs")
text = p.read_text(encoding='utf-8')

old = '''bloque exacto a reemplazar
multilinea OK'''
new = '''bloque nuevo
multilinea OK'''

assert old in text, "no encontre el bloque"
text = text.replace(old, new)
p.write_text(text, encoding='utf-8')
print(f"OK: lines={len(text.splitlines())}")
PYEOF
```

Ventajas:
- Lee el archivo completo, modifica en memoria, escribe entero.
- `assert old in text` falla rápido si el bloque cambió.
- Funciona con strings de cualquier longitud.

## Verificación post-escritura

Siempre después de escribir un archivo C#/Java/Dart/cualquier brace-language:

```bash
F="/path/with spaces/MyFile.cs"
echo "Lines: $(wc -l < "$F")"
echo "Opens:  $(tr -cd '{' < "$F" | wc -c)"   # cuenta caracteres exactos
echo "Closes: $(tr -cd '}' < "$F" | wc -c)"
echo "Last 3 lines:"
tail -3 "$F"
```

Validaciones:
- **Opens == Closes**: braces balanceados.
- **Last 3 lines termina con `}`** (no en medio de una expresión).
- **Lines** coincide con lo esperado (si escribiste 200 líneas y dice 80, está truncado).

⚠️ **`grep -c '{'` cuenta LÍNEAS con `{`, no caracteres**. Una línea como `if (x) { return new() {...}; }` cuenta como 1 abierta y 1 cerrada por grep, pero son 2 caracteres de cada uno. Usar `tr -cd` para contar caracteres exactos.

## Anti-patterns

- ❌ Usar el Edit tool para reemplazar 100+ líneas de una vez en path con espacios. **Siempre verificar después con `wc -l` y brace count**.
- ❌ Confiar en "File created successfully" como confirmación. El tool puede reportar OK y haber escrito menos.
- ❌ Hacer múltiples Edit consecutivos sobre el mismo archivo grande sin verificar entre ellos.

## Síntomas de truncamiento silencioso

Si después de un edit ves alguno de estos errores en el build:
```
Error: } expected
Error: ; expected
Error: A new expression requires an argument list or ()
Error: unexpected end of file
Error: identifier expected
```

Y el error apunta a la **última línea** del archivo o cerca, **el archivo está truncado**. Verificar con `tail -3` y `wc -l`.

Si el archivo está bajo control de versiones, recuperar la versión correcta:
```bash
git show HEAD:path/to/file.cs > /tmp/file-original.cs
# luego aplicar cambios via Python sobre /tmp/file-original.cs
```

## Edge case: el `Write` tool también puede truncar

Si `Write` truncó un archivo grande, ESCRIBIR DE NUEVO con `Write` puede truncarlo en el mismo punto (cache del path o algo). Pasar a bash heredoc directamente.

## Verificación de un archivo recién escrito (script reutilizable)

```bash
verify_file() {
  local f="$1"
  local expected_lines="${2:-}"
  local lines=$(wc -l < "$f")
  local opens=$(tr -cd '{' < "$f" | wc -c)
  local closes=$(tr -cd '}' < "$f" | wc -c)
  local last_char=$(tail -c 3 "$f" | tr -d ' \n' | head -c 1)

  echo "File: $f"
  echo "  Lines:  $lines${expected_lines:+ (expected $expected_lines)}"
  echo "  Braces: $opens / $closes"
  echo "  Last char: $last_char"

  if [ "$opens" != "$closes" ]; then echo "  ❌ BRACES UNBALANCED"; return 1; fi
  if [ "$last_char" != "}" ] && [ "$last_char" != ")" ] && [ "$last_char" != ";" ]; then
    echo "  ❌ FILE LIKELY TRUNCATED (no proper terminator)"
    return 1
  fi
  echo "  ✓ OK"
}
```
