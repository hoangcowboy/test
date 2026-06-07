---
applyTo: "ShopWeb/**/*.{ts,tsx}"
---

# Frontend convention (ShopWeb/)

These rules apply only to files under `ShopWeb/`.

## TypeScript

- `strict: true` always — no `any`, no `unknown` without narrowing
- Prefer `type` for unions, `interface` for object shapes (extendable)
- No `// @ts-ignore` / `// @ts-expect-error` without a one-line "why" comment
- DTO types in `features/<name>/types.ts` MUST mirror BE record names

## React

- Function components only — no class components
- Hooks rules: only call at top level, never inside loops/conditions
- One component per file, file name matches component (`ProductList.tsx` exports `ProductList`)
- Default export = main component; named exports for sub-components

## State

- **Server state** → TanStack Query (`useQuery`, `useMutation`)
- **Form state** → `useForm` from `react-hook-form` + `zodResolver`
- **UI state local to component** → `useState`
- **Cross-tree shared UI state** → `React.Context` (lift up first, context only if 3+ levels)
- ❌ Never use `useState` to hold API data
- ❌ Never `useEffect(() => { fetch(...) }, [])` — use TanStack Query

## Data fetching

- All HTTP via `@/lib/axios` (`api` instance) — never raw `fetch` or new axios instance
- Query keys MUST come from `features/<name>/api.ts` `xxxKeys` object (no inline strings)
- Mutations call `queryClient.invalidateQueries({ queryKey: xxxKeys.xxx })` on success
- `staleTime` configured globally (30s) — override only when justified

## Styling

- Tailwind utilities only — no `<style>` blocks, no `.css` files (except `index.css`)
- `cn()` from `@/lib/utils` for conditional classes
- Mobile-first: `sm: md: lg:` (small first, not large first)
- Spacing scale: stick to Tailwind defaults (`p-4`, `gap-2`) — avoid `[13px]`
- Color: use design tokens from `index.css` (`bg-background`, `text-foreground`, `text-muted-foreground`, `border`)
- Dark mode via `dark:` variants — shadcn primitives already handle this

## shadcn/ui

- ❌ NEVER edit files under `src/components/ui/` — they are vendored
- ✅ Re-add via CLI if a primitive is buggy: `npx shadcn@latest add <name> -y`
- Compose primitives in feature folders / `src/components/` instead of forking
- Toast: use `sonner` (`toast.success(...)`) not custom

## Forms

- `useForm<Z>({ resolver: zodResolver(schema), mode: "onBlur" })`
- Schema in `features/<name>/schemas.ts`
- Use shadcn `<Form>` family (`FormField`, `FormItem`, `FormLabel`, `FormControl`, `FormMessage`)
- Submit handler: `onSubmit={form.handleSubmit(handle)}` — never bare onSubmit
- Disable submit button while `isSubmitting`

## Accessibility

- `<button type="button">` by default (only `type="submit"` inside forms)
- Icon-only buttons → `aria-label="..."`
- Modal traps focus (shadcn `<Dialog>` does this)
- Visible focus ring — don't override `focus-visible:` from shadcn
- `<h1>` once per page, then `<h2>`, `<h3>` hierarchical

## Performance

- `React.memo` only when profiled — not by default
- Heavy lists → `key` MUST be stable id, not array index
- Code-split routes with `React.lazy` if a page bundle > 100kb
- Images: `<img loading="lazy" />` for off-screen

## Naming

- Component: `PascalCase.tsx`
- Hook: `useCamelCase.ts`
- Type: `PascalCase`
- Function / variable: `camelCase`
- Constant: `UPPER_SNAKE` for true compile-time constants only
- Folder: lowercase (`products`, `cart`, not `Products`)

## Imports

- Absolute paths via `@/` alias (`import { api } from "@/lib/axios"`)
- Order: 1) external 2) `@/...` 3) relative — auto-sort with biome/prettier if configured
- No `import * as X` unless namespacing 5+ symbols

## Forbidden

- ❌ `dangerouslySetInnerHTML` (unless reviewer-approved with sanitization)
- ❌ `any`, `Object`, `Function` types
- ❌ `console.log` in committed code (use `console.warn` for dev-only and remove before commit)
- ❌ Inline `style={{ ... }}` unless value is dynamic (computed at runtime)
- ❌ Direct `localStorage` access — wrap in a hook with try/catch
- ❌ `eval`, `new Function(...)` ever
- ❌ Mutating props or state directly
- ❌ `new Date()` for "now" in tests — inject a clock

## Don't (process)

- ❌ Skip `npm run build` before committing — TS must pass
- ❌ Merge multiple TODOs into one commit
- ❌ Add a new dep without architect sign-off (raise via comment in TODO.md)
