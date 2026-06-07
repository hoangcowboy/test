---
name: bootstrap-frontend
description: |
  Initialize a React 18 + Vite + TypeScript frontend (`ShopWeb/`) from scratch,
  install Tailwind v4 + shadcn/ui + TanStack Query + React Router + react-hook-form
  + zod + axios, and create the baseline (App router, providers, axios client,
  utils, layout). Use this skill when:
  - Monorepo workspace contains `ShopApi/` but NO `ShopWeb/` folder
  - User says "bootstrap frontend", "scaffold UI", "/bootstrap-frontend"
  - As Phase 0b of `deliver-feature` when spec has a Frontend section

  Skip when `ShopWeb/package.json` already exists.
---

# bootstrap-frontend

Initialize `ShopWeb/` from empty to a buildable React + Vite + TS app matching `.github/instructions/frontend.instructions.md`.

## Pre-conditions

- Workspace contains `.github/copilot-instructions.md`
- Workspace contains `ShopApi/` (or is OK with FE-only delivery)
- Workspace does NOT contain `ShopWeb/package.json`
- `node --version` is ≥ 20 LTS
- `npm --version` is ≥ 10

If pre-conditions fail, output the missing requirement and stop.

## Required terminal commands (run in workspace root)

Run sequentially. After each, verify exit code = 0 before continuing.

```bash
# Versions are PINNED (no caret) for demo reproducibility. Update quarterly.

# 1. Scaffold Vite + React + TS  (Vite 5.4 LTS pinned via create-vite)
npm create vite@5.5.6 ShopWeb -- --template react-ts -y
cd ShopWeb

# 2. Install runtime deps — exact versions
npm install \
  react@18.3.1 \
  react-dom@18.3.1 \
  react-router-dom@6.26.2 \
  @tanstack/react-query@5.59.0 \
  @tanstack/react-query-devtools@5.59.0 \
  axios@1.7.7 \
  react-hook-form@7.53.0 \
  zod@3.23.8 \
  @hookform/resolvers@3.9.0 \
  lucide-react@0.445.0 \
  clsx@2.1.1 \
  tailwind-merge@2.5.3 \
  class-variance-authority@0.7.0 \
  sonner@1.5.0 \
  --save-exact

# 3. Install dev deps — exact versions (incl. Vitest + Playwright for mandatory tests)
npm install -D \
  tailwindcss@4.0.0-beta.4 \
  @tailwindcss/vite@4.0.0-beta.4 \
  @types/node@22.7.4 \
  vitest@2.1.1 \
  @vitest/ui@2.1.1 \
  jsdom@25.0.1 \
  @testing-library/react@16.0.1 \
  @testing-library/jest-dom@6.5.0 \
  @testing-library/user-event@14.5.2 \
  @playwright/test@1.47.2 \
  --save-exact

# 3b. Install Playwright browsers (chromium only for speed)
npx --yes playwright@1.47.2 install chromium --with-deps

# 4. Initialize shadcn/ui non-interactively (force-yes + default + base color)
npx --yes shadcn@2.1.0 init -y -d -b slate

# 5. Add starter shadcn primitives (12 commonly used)
npx --yes shadcn@2.1.0 add button input label card dialog dropdown-menu badge \
  select form table separator sheet skeleton sonner -y

cd ..
```

> **Why pinned:** demo reproducibility. Caret `^` allowed Tailwind v4 beta → final to drift between rehearsal and live, breaking shadcn-init. Update these versions quarterly via a scheduled review.

## Files to create / overwrite (after npm install)

### `ShopWeb/vite.config.ts`

```ts
import path from "node:path";
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";

export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: { "@": path.resolve(__dirname, "./src") },
  },
  server: {
    port: 5173,
    proxy: {
      "/api": { target: "http://localhost:5080", changeOrigin: true },
    },
  },
});
```

### `ShopWeb/tsconfig.json` — ensure strict + path alias

Merge into existing:
```json
{
  "compilerOptions": {
    "strict": true,
    "noUnusedLocals": true,
    "noUnusedParameters": true,
    "noImplicitAny": true,
    "baseUrl": ".",
    "paths": { "@/*": ["./src/*"] }
  }
}
```

### `ShopWeb/src/index.css`

```css
@import "tailwindcss";
@import url("https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap");

@theme {
  --font-sans: "Inter", ui-sans-serif, system-ui, sans-serif;

  /* Brand palette — calm slate-blue (works in both light/dark) */
  --color-brand-50:  oklch(0.97 0.02 240);
  --color-brand-100: oklch(0.93 0.04 240);
  --color-brand-200: oklch(0.86 0.07 240);
  --color-brand-300: oklch(0.78 0.10 240);
  --color-brand-400: oklch(0.69 0.14 240);
  --color-brand-500: oklch(0.60 0.17 240);
  --color-brand-600: oklch(0.52 0.18 240);
  --color-brand-700: oklch(0.44 0.17 240);
  --color-brand-800: oklch(0.36 0.14 240);
  --color-brand-900: oklch(0.28 0.10 240);
}

body {
  @apply bg-background text-foreground antialiased font-sans;
  font-feature-settings: "cv02", "cv03", "cv04", "cv11";  /* nicer Inter alternates */
}

/* Smooth scrolling + reduced motion respect */
@media (prefers-reduced-motion: no-preference) {
  html { scroll-behavior: smooth; }
}

/* Better focus ring (visible everywhere) */
*:focus-visible {
  @apply outline-none ring-2 ring-brand-500 ring-offset-2 ring-offset-background rounded-sm;
}
```

(shadcn `init` will inject its own CSS variables for `--background`, `--foreground`, `--border`, etc. — KEEP those, append the `@theme` block + body styles above/alongside.)

### `ShopWeb/src/lib/axios.ts`

```ts
import axios from "axios";

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE ?? "/api",
  timeout: 10_000,
});

api.interceptors.response.use(
  (r) => r,
  (err) => {
    const msg = err.response?.data?.title ?? err.message;
    return Promise.reject(new Error(msg));
  },
);
```

### `ShopWeb/src/lib/queryClient.ts`

```ts
import { QueryClient } from "@tanstack/react-query";

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      refetchOnWindowFocus: false,
      retry: 1,
    },
  },
});
```

### `ShopWeb/src/App.tsx`

```tsx
import { BrowserRouter, Route, Routes } from "react-router-dom";
import { QueryClientProvider } from "@tanstack/react-query";
import { ReactQueryDevtools } from "@tanstack/react-query-devtools";
import { Toaster } from "sonner";
import { queryClient } from "@/lib/queryClient";
import { RootLayout } from "@/components/RootLayout";
import { HomePage } from "@/pages/HomePage";

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <Routes>
          <Route element={<RootLayout />}>
            <Route index element={<HomePage />} />
          </Route>
        </Routes>
        <Toaster richColors position="top-right" />
      </BrowserRouter>
      <ReactQueryDevtools initialIsOpen={false} />
    </QueryClientProvider>
  );
}
```

### `ShopWeb/src/components/RootLayout.tsx`

```tsx
import { Link, Outlet } from "react-router-dom";
import { ShoppingCart, Package } from "lucide-react";

export function RootLayout() {
  return (
    <div className="min-h-screen flex flex-col">
      <header className="border-b sticky top-0 z-40 bg-background/95 backdrop-blur">
        <div className="container mx-auto flex items-center justify-between h-14 px-4">
          <Link to="/" className="flex items-center gap-2 font-semibold">
            <Package className="size-5" />
            <span>ShopWeb</span>
          </Link>
          <nav className="flex items-center gap-4 text-sm">
            <Link to="/products" className="hover:text-brand-700">Products</Link>
            <Link to="/cart" className="flex items-center gap-1 hover:text-brand-700">
              <ShoppingCart className="size-4" />Cart
            </Link>
          </nav>
        </div>
      </header>
      <main className="flex-1 container mx-auto px-4 py-6">
        <Outlet />
      </main>
      <footer className="border-t py-4 text-center text-xs text-muted-foreground">
        © 2026 ShopWeb · Built with Copilot Agent Mode
      </footer>
    </div>
  );
}
```

### `ShopWeb/src/pages/HomePage.tsx`

```tsx
export function HomePage() {
  return (
    <section className="text-center py-16">
      <h1 className="text-4xl font-bold tracking-tight">Welcome to ShopWeb</h1>
      <p className="mt-4 text-muted-foreground">
        Pages will be implemented by @frontend-dev from the feature spec.
      </p>
    </section>
  );
}
```

### `ShopWeb/.env.development`

```
VITE_API_BASE=http://localhost:5080/api
```

### `ShopWeb/vitest.config.ts`

```ts
import { defineConfig, mergeConfig } from "vitest/config";
import viteConfig from "./vite.config";

export default mergeConfig(viteConfig, defineConfig({
  test: {
    environment: "jsdom",
    globals: true,
    setupFiles: ["./src/test/setup.ts"],
    css: false,
    coverage: { provider: "v8", reporter: ["text", "html"], lines: 80, branches: 70 },
  },
}));
```

### `ShopWeb/src/test/setup.ts`

```ts
import "@testing-library/jest-dom/vitest";
import { afterEach } from "vitest";
import { cleanup } from "@testing-library/react";
afterEach(() => cleanup());
```

### `ShopWeb/playwright.config.ts`

```ts
import { defineConfig, devices } from "@playwright/test";

export default defineConfig({
  testDir: "./tests-e2e",
  fullyParallel: false,           // serial — share BE state (DB seed)
  forbidOnly: true,
  reporter: [["list"], ["html", { open: "never" }]],
  use: {
    baseURL: "http://localhost:5173",
    trace: "retain-on-failure",
    screenshot: "only-on-failure",
    video: "retain-on-failure",
  },
  projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
  webServer: [
    {
      command: "cd ../ShopApi && dotnet run --no-build --urls http://localhost:5080",
      url: "http://localhost:5080/health",
      reuseExistingServer: true,
      timeout: 60_000,
    },
    {
      command: "npm run dev -- --port 5173",
      url: "http://localhost:5173",
      reuseExistingServer: true,
      timeout: 60_000,
    },
  ],
});
```

### `ShopWeb/package.json` scripts addition

```json
"scripts": {
  "dev": "vite",
  "build": "tsc -b && vite build",
  "preview": "vite preview",
  "test": "vitest run",
  "test:watch": "vitest",
  "test:e2e": "playwright test",
  "test:e2e:ui": "playwright test --ui",
  "test:smoke": "playwright test --grep @smoke"
}
```

### `ShopWeb/src/main.tsx` — overwrite

```tsx
import React from "react";
import ReactDOM from "react-dom/client";
import App from "./App";
import "./index.css";

ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode><App /></React.StrictMode>,
);
```

### `ShopWeb/index.html` — update `<title>` + lang

```html
<!doctype html>
<html lang="en">
  <head>
    <meta charset="UTF-8" />
    <link rel="icon" type="image/svg+xml" href="/vite.svg" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>ShopWeb · Demo</title>
  </head>
  <body><div id="root"></div><script type="module" src="/src/main.tsx"></script></body>
</html>
```

## Gitignore hygiene (CRITICAL — do not skip)

Frontend scaffolding generates ~13,000+ files in `ShopWeb/node_modules/` and `dist/` / `playwright-report/` / `test-results/` after running tests. NONE of these should ever be committed.

**Step A — Verify the root `.gitignore` already covers FE patterns.** It is created by `setup.ps1` / `setup.sh` and must contain at least:

```
node_modules/
dist/
dist-ssr/
build/
.vite/
.cache/
*.local
*.tsbuildinfo
playwright-report/
test-results/
playwright/.cache/
.env
.env.local
.env.development.local
.env.production.local
*.log
```

If any of these are missing, APPEND them. Do not rewrite the file from scratch (preserve the .NET section).

**Step B — Create `ShopWeb/.gitignore`** (scoped to FE, defensive layer in case the workspace is later split out):

```bash
cat > ShopWeb/.gitignore <<'EOF'
node_modules/
dist/
dist-ssr/
.vite/
*.local
*.tsbuildinfo
playwright-report/
test-results/
playwright/.cache/
.env
.env.local
.env.development.local
.env.production.local
*.log
EOF
```

`npm create vite` USUALLY generates one, but version drift (5.5.6 vs newer) sometimes skips it. This step is idempotent — overwriting Vite's default with the same content is fine.

**Step C — Verify nothing junk is in the git index** before committing:

```bash
git status --short | head -20
git ls-files | grep -E "(node_modules|/dist/|playwright-report|test-results)" | head -5
```

If anything tracked appears: `git rm -r --cached <path>` to untrack, then commit.

## Verification

```bash
cd ShopWeb
npm run build
```

Must exit 0. If TS errors, fix and re-run.

Optionally:
```bash
npm run dev
```
→ Vite serves on `http://localhost:5173`, page renders "Welcome to ShopWeb".

## Output to user

```markdown
## ✅ Frontend bootstrapped

**Created:**
- `ShopWeb/` (Vite + React 18 + TS strict)
- Tailwind v4 + shadcn/ui (12 primitives vendored in `components/ui/`)
- TanStack Query + React Router + Toaster + DevTools wired

**Status:**
- ✅ `npm run build` OK
- ✅ Dev server starts on http://localhost:5173
- ✅ Proxy `/api` → http://localhost:5080 configured

**Ready for:** `@frontend-dev` to implement feature pages from TODO.md (FE tasks).

**Commit:** `chore: bootstrap ShopWeb baseline (React+Vite+TS+Tailwind+shadcn)`
```

## Triggers

- "/bootstrap-frontend"
- "bootstrap UI"
- "scaffold React app"
- As Phase 0b of `deliver-feature` when spec has a Frontend section

## Don't

- Don't run `npm create vite` if `ShopWeb/package.json` already exists
- Don't add UI libs not in this list (no MUI, no Chakra, no Mantine — clash with shadcn)
- Don't implement feature pages here (that's `@frontend-dev`)
- Don't add state-management libs (Zustand/Redux) unless explicitly required — TanStack Query covers server state, useState/Context covers UI state
- Don't pin Node/npm versions in package.json — let user's env decide

## Self-heal (autonomous mode)

`npm install` fail → `npm cache clean --force` → retry once. Peer-dep conflict → retry with `--legacy-peer-deps`. `npx shadcn init` hanging interactive → kill, re-run with `-y -d -b slate -s`. Vite port 5173 busy → set `server.port` to next free (5174…). `npm run build` TS error → patch types, rebuild (max 2 retries). See `deliver-feature/SKILL.md` Autonomous Mode for budget rules.
