# Struktura Projektu React

> [!NOTE]
> Organizacja kodu w projekcie `investment-hub-react` - folder structure, naming conventions, patterns

## Spis Treści
- [Przegląd Struktury](#przegląd-struktury)
- [Folder `app/`](#folder-app)
- [Folder `lib/`](#folder-lib)
- [Folder `components/`](#folder-components)
- [Naming Conventions](#naming-conventions)

---

## Przegląd Struktury

```
src/investment-hub-react/
├── app/                    # Next.js App Router
│   ├── (auth)/             # Route group - strony auth
│   ├── (dashboard)/        # Route group - strony po zalogowaniu
│   ├── layout.tsx          # Root layout
│   ├── page.tsx            # Home page
│   ├── providers.tsx       # React Query + Theme Providers
│   └── globals.css         # Global styles
├── components/
│   ├── ui/                 # shadcn/ui base components
│   ├── layout/             # Layout components (NavMenu, etc.)
│   ├── portfolio/          # Portfolio-specific
│   ├── investment/         # Investment-specific
│   └── shared/             # Reusable across features
├── lib/
│   ├── api/                # API client + functions
│   ├── hooks/              # Custom React hooks
│   ├── stores/             # Zustand stores
│   ├── types/              # TypeScript types
│   ├── constants.ts        # App constants
│   └── utils.ts            # Utility functions
├── public/                 # Static assets
├── .env.local              # Environment variables (git-ignored)
├── package.json            # Dependencies
└── tsconfig.json           # TypeScript config
```

---

## Folder `app/`

Next.js App Router - **file-based routing**.

### Route Groups `(name)`

```
app/
├── (auth)/
│   ├── layout.tsx          # Layout TYLKO dla auth
│   ├── login/
│   │   └── page.tsx        → /login
│   └── register/
│       └── page.tsx        → /register
└── (dashboard)/
    ├── layout.tsx          # Layout TYLKO dla dashboard (NavMenu)
    ├── page.tsx            → /
    └── portfolio/
        └── [id]/
            └── page.tsx    → /portfolio/:id
```

**Dlaczego route groups?**
- Auth pages: wyśrodkowany formularz, BEZ nawigacji
- Dashboard pages: NavMenu, sidebar, footer

### Special Files

| File | Purpose |
|------|---------|
| `page.tsx` | Strona (URL endpoint) |
| `layout.tsx` | Layout wrapper |
| `loading.tsx` | Loading state (Suspense) |
| `error.tsx` | Error boundary |
| `not-found.tsx` | 404 page |

### `providers.tsx`

```typescript
'use client'

import { QueryClientProvider } from '@tanstack/react-query'
import { ThemeProvider } from 'next-themes'

export function Providers({ children }: { children: React.ReactNode }) {
  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider attribute="class" defaultTheme="system">
        {children}
      </ThemeProvider>
    </QueryClientProvider>
  )
}
```

**Dlaczego osobny plik?**
- `'use client'` - providers muszą być Client Components
- Root layout może pozostać Server Component
- Separacja odpowiedzialności

---

## Folder `lib/`

Business logic, utilities, wszystko **poza UI**.

### `lib/api/`

```
api/
├── client.ts               # Axios instance + interceptors
├── auth.ts                 # Login, register, etc.
├── portfolios.ts           # CRUD portfolios
├── investments.ts          # CRUD investments
├── market-data.ts          # Instruments, prices
├── users.ts                # Admin - users
└── index.ts                # Barrel export
```

**Pattern:**

```typescript
// client.ts - configuration
export const apiClient = axios.create({ ... })

// portfolios.ts - functions
export async function getPortfolios(userId: string): Promise<Portfolio[]> { ... }
export async function createPortfolio(request: CreatePortfolioRequest): Promise<void> { ... }

// index.ts - barrel export
export * from './auth'
export * from './portfolios'
```

### `lib/hooks/`

```
hooks/
├── use-auth.ts             # useLogin, useRegister, etc.
├── use-portfolios.ts       # usePortfolios, useCreatePortfolio
├── use-investments.ts      # useInvestments, useAddInvestment
└── use-market-data.ts      # useSearchInstruments, useMarketPrice
```

**Pattern - React Query hooks:**

```typescript
// use-portfolios.ts
import { useQuery, useMutation } from '@tanstack/react-query'
import { getPortfolios, createPortfolio } from '@/lib/api'

export function usePortfolios(userId: string) {
  return useQuery({
    queryKey: ['portfolios', userId],
    queryFn: () => getPortfolios(userId),
  })
}

export function useCreatePortfolio() {
  const queryClient = useQueryClient()
  
  return useMutation({
    mutationFn: createPortfolio,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['portfolios'] })
    }
  })
}
```

### `lib/stores/`

```
stores/
├── auth-store.ts           # Zustand - token, user, login/logout
├── user-store.ts           # Zustand - selectedUserId (admin)
└── index.ts                # Barrel export
```

**Pattern - Zustand with persist:**

```typescript
// auth-store.ts
import { create } from 'zustand'
import { persist } from 'zustand/middleware'

interface AuthState {
  token: string | null
  user: User | null
  setAuth: (token: string, user: User) => void
  clearAuth: () => void
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      token: null,
      user: null,
      setAuth: (token, user) => set({ token, user }),
      clearAuth: () => set({ token: null, user: null }),
    }),
    { name: 'auth-storage' }
  )
)
```

### `lib/types/index.ts`

```typescript
// WSZYSTKIE types w jednym pliku (łatwiej zarządzać)
export interface Portfolio { ... }
export interface Investment { ... }
export interface User { ... }
export interface Money { ... }
export interface Symbol { ... }

export enum AssetType { ... }
export enum InvestmentStatus { ... }
```

### `lib/constants.ts`

```typescript
export const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000'

export const STORAGE_KEYS = {
  AUTH_TOKEN: 'investmenthub_token',
  SELECTED_USER: 'investmenthub_selected_user',
} as const

export const QUERY_KEYS = {
  PORTFOLIOS: 'portfolios',
  INVESTMENTS: 'investments',
} as const
```

---

## Folder `components/`

Wszystkie React komponenty UI.

### `components/ui/`

**shadcn/ui base components** - NIE MODYFIKUJ (generowane przez CLI).

```
ui/
├── button.tsx
├── card.tsx
├── dialog.tsx
├── input.tsx
├── select.tsx
└── ...
```

Dodawanie nowych:
```bash
npx shadcn@latest add card
npx shadcn@latest add dialog
```

### `components/layout/`

Komponenty layout (nawigacja, sidebar, footer).

```
layout/
├── nav-menu.tsx            # Górna nawigacja
├── user-menu.tsx           # Dropdown z avatarem
├── theme-toggle.tsx        # Dark/light mode przełącznik
└── footer.tsx              # Stopka
```

**Pattern:**

```typescript
// nav-menu.tsx
'use client'  // Client Component (onClick, useState)

export function NavMenu() {
  const { user } = useAuthStore()
  
  return (
    <nav>
      <Link href="/">Dashboard</Link>
      {user && <UserMenu user={user} />}
    </nav>
  )
}
```

### `components/portfolio/`

Komponenty specyficzne dla portfeli.

```
portfolio/
├── portfolio-card.tsx      # Karta portfela
├── portfolio-list.tsx      # Lista portfeli
├── portfolio-form.tsx      # Formularz create/edit
└── portfolio-stats.tsx     # Statystyki portfela
```

**Pattern - feature-specific:**

```typescript
// portfolio-card.tsx
interface PortfolioCardProps {
  portfolio: Portfolio
  onDelete?: () => void
}

export function PortfolioCard({ portfolio, onDelete }: PortfolioCardProps) {
  return (
    <Card>
      <CardHeader>
        <CardTitle>{portfolio.name}</CardTitle>
      </CardHeader>
      <CardContent>
        <p>{portfolio.totalValue?.amount} {portfolio.totalValue?.currency}</p>
      </CardContent>
      {onDelete && (
        <CardFooter>
          <Button variant="destructive" onClick={onDelete}>Delete</Button>
        </CardFooter>
      )}
    </Card>
  )
}
```

### `components/shared/`

Komponenty reużywalne across features.

```
shared/
├── loading-skeleton.tsx    # Loading placeholders
├── error-boundary.tsx      # Error handling
├── empty-state.tsx         # Empty lists
└── confirm-dialog.tsx      # Confirmation dialog
```

---

## Naming Conventions

### Files

| Type | Naming | Example |
|------|--------|---------|
| **React Component** | `kebab-case.tsx` | `portfolio-card.tsx` |
| **Page** | `page.tsx` | `app/portfolio/[id]/page.tsx` |
| **Layout** | `layout.tsx` | `app/(dashboard)/layout.tsx` |
| **API Function** | `kebab-case.ts` | `api/portfolios.ts` |
| **Hook** | `use-*.ts` | `use-portfolios.ts` |
| **Type** | `kebab-case.ts` OR `index.ts` | `types/index.ts` |
| **Store** | `*-store.ts` | `auth-store.ts` |

### Components

```typescript
// ✅ DOBRZE - PascalCase dla komponentu
export function PortfolioCard({ portfolio }: PortfolioCardProps) { }

// ✅ DOBRZE - Props interface z suffiksem Props
interface PortfolioCardProps {
  portfolio: Portfolio
}

// ❌ ŹLE
export function portfolio_card() { }
interface Props { }
```

### Functions

```typescript
// ✅ DOBRZE - camelCase
export async function getPortfolios(userId: string) { }
export function usePortfolios(userId: string) { }

// ❌ ŹLE
export async function GetPortfolios() { }
```

### Constants

```typescript
// ✅ DOBRZE - UPPER_SNAKE_CASE
export const API_BASE_URL = '...'
export const STORAGE_KEYS = { ... }

// ✅ DOBRZE - camelCase dla config objects
export const queryConfig = { ... }
```

---

## Import Paths

### Absolute Imports (`@/`)

```typescript
// tsconfig.json
{
  "compilerOptions": {
    "paths": {
      "@/*": ["./*"]
    }
  }
}
```

**Użycie:**

```typescript
// ✅ DOBRZE - absolute
import { Button } from '@/components/ui/button'
import { usePortfolios } from '@/lib/hooks/use-portfolios'
import { Portfolio } from '@/lib/types'

// ❌ ŹLE - relative (za dużo ../)
import { Button } from '../../../components/ui/button'
```

### Barrel Exports

```typescript
// lib/api/index.ts
export * from './auth'
export * from './portfolios'
export * from './investments'

// Użycie
import { login, getPortfolios, addInvestment } from '@/lib/api'
```

---

## Podsumowanie

### Hierarchia Zależności

```
Components (UI)
      ↓
Custom Hooks
      ↓
API Functions + Stores
      ↓
Types + Constants
```

**Zasada:** Wyższe warstwy mogą importować z niższych, ALE NIE odwrotnie!

### Best Practices

1. ✅ **Separation of Concerns** - UI  (components) vs Logic (lib)
2. ✅ **Feature Folders** - `components/portfolio/`, `components/investment/`
3. ✅ **Barrel Exports** - `index.ts` dla czystych importów
4. ✅ **Absolute Imports** - `@/` zamiast `../../`
5. ✅ **Consistent Naming** - kebab-case (files), PascalCase (components), camelCase (functions)
6. ✅ **TypeScript Types** - osobny folder `lib/types/`

---

**Zobacz też:**
- [React Basics](./react-basics.md) - Component patterns
- [Next.js](./nextjs.md) - App Router structure
- [API Integration](./api-integration.md) - API layer design
