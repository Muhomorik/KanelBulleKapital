@AGENTS.md

# Frontend — Stack & Hosting

## Hosting: Azure Static Web Apps (not a Node server)

This frontend ships as a **pure static export** — no Node runtime in production.

- `next.config.ts` → `output: "export"` → `next build` writes static HTML/JS/CSS to `out/`
- Deployed to **Azure Static Web Apps** (CDN + static file hosting only)
- **There is no per-request SSR, no route handlers at runtime, no middleware, no server components at runtime.** Anything that looks like SSR is really build-time pre-rendering on the CI machine.

### Implications — read before "fixing" things

- **No hydration-mismatch guard gymnastics needed for client-fetched data.**
  The dashboard boots with demo data as client state ([app/page.tsx](app/page.tsx))
  and only fetches real data after mount. The pre-rendered HTML only ever
  contains demo values, so `useSyncExternalStore` / mount gates /
  `suppressHydrationWarning` are usually unnecessary. Just format dates in
  local time directly.
- **Build-time locale/timezone ≠ user's.** Any `toLocaleDateString()` /
  `toLocaleTimeString()` called during render bakes the build machine's locale
  into the static HTML. It's fine for demo fallback values, but don't rely on
  it for real data display — real data renders client-side anyway.
- **Do not add** `route.ts` handlers, `"use server"` actions, `revalidate`,
  ISR, `generateMetadata` with dynamic fetches, or anything else that assumes
  a server. It won't run.
- **API calls go to the backend directly** via `NEXT_PUBLIC_API_URL` — an
  Azure Functions app, not a Next.js route.

## Tech stack

| Tech                       | Purpose                    |
| -------------------------- | -------------------------- |
| Next.js 16 (static export) | Build tool + routing       |
| React 19                   | UI                         |
| TypeScript 5               | Types                      |
| Tailwind CSS 4             | Styling                    |
| shadcn/ui (Base UI)        | Component primitives       |
| lucide-react               | Icons                      |
| Jest 30 + Testing Library  | Unit tests (`__tests__/`)  |

## Backend contract

- Backend: .NET 9 Azure Functions ([backend/KanelBrief.Functions](../backend/KanelBrief.Functions))
- Dates over the wire are **always `DateTimeOffset`** → ISO 8601 with offset (e.g. `"2026-04-06T00:00:00+00:00"`). Parse with `new Date(iso)` directly — never append `"T00:00:00"`.
- Types mirror backend C# models in [lib/types.ts](lib/types.ts).

## Running

```bash
npm run dev      # local dev server
npm run build    # static export to out/
npm test         # jest
```
