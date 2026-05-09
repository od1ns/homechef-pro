import { Page, expect } from '@playwright/test';

/**
 * Helper para tests UI contra apps Flutter web (CanvasKit).
 *
 * Flutter web 3.41 con CanvasKit pinta todo el contenido (textos, botones,
 * inputs) dentro de un unico <canvas> WebGL. El DOM solo contiene un par
 * de elementos placeholder. Para que Playwright pueda interactuar con
 * widgets, hay que activar el "semantics tree" que Flutter expone como
 * arbol DOM accesible cuando detecta tecnologia asistiva.
 *
 * Forma estandar de activarlo en E2E:
 *   1. Esperar a que el bootstrap de Flutter complete (titulo seteado).
 *   2. Click en `flt-semantics-placeholder` — el handler interno de
 *      Flutter detecta esto como "el usuario uso un screen reader" y
 *      construye el arbol semantico DOM.
 *   3. A partir de aqui, getByLabel/getByRole/getByText funcionan.
 */
export async function bootFlutterApp(page: Page, url: string, titleRegex: RegExp): Promise<void> {
  await page.goto(url);

  // Espera a que Flutter haya seteado el titulo (eso confirma que main.dart
  // termino de bootear).
  await expect(page).toHaveTitle(titleRegex, { timeout: 60000 });

  // Activa el semantics tree. El placeholder esta dentro del flt-glass-pane
  // (shadow DOM). Hacemos el click via JS para evadir shadow DOM.
  await page.evaluate(() => {
    // Busca el placeholder en todo el documento (inclusive shadow roots).
    const findPlaceholder = (root: Document | ShadowRoot): Element | null => {
      const direct = root.querySelector('flt-semantics-placeholder');
      if (direct) return direct;
      // Buscar en shadow roots de elementos
      const all = root.querySelectorAll('*');
      for (const el of Array.from(all)) {
        if ((el as HTMLElement).shadowRoot) {
          const found = findPlaceholder((el as HTMLElement).shadowRoot!);
          if (found) return found;
        }
      }
      return null;
    };
    const ph = findPlaceholder(document);
    if (ph) (ph as HTMLElement).click();
  });

  // Despues del click, el semantics tree se construye asincronicamente.
  // Esperamos a que aparezca al menos un nodo con role="..." o aria-label.
  await page.waitForSelector('flt-semantics, [role], [aria-label]', {
    timeout: 10000,
    state: 'attached',
  }).catch(() => {
    // No-op: si no aparece, los selectores siguientes lanzaran timeouts
    // claros con detalle.
  });
}

/**
 * Helper de logout: limpia localStorage y navega a la URL base.
 * Util para correr varios tests en orden sin contaminar la sesion.
 */
export async function clearSession(page: Page, url: string): Promise<void> {
  await page.goto(url);
  await page.evaluate(() => {
    try {
      localStorage.clear();
      sessionStorage.clear();
    } catch {
      // ignore (algunos browsers tiran si no hay origin)
    }
  });
}
