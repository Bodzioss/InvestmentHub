# InvestmentHub - Frontend Learning Doc - React & Next.js

> [!NOTE]
> Ten dokument jest żywą dokumentacją - będzie aktualizowany przy każdej nowej funkcji.

---

## Spis treści

### React Ecosystem
1. [React - Podstawy](#1-react-podstawy)
2. [Next.js - Framework](#2-nextjs-framework)
3. [TypeScript w React](#3-typescript-w-react)
4. [TanStack Query (React Query)](#4-tanstack-query)
5. [Zustand - State Management](#5-zustand)
6. [React Hook Form + Zod](#6-react-hook-form-zod)
7. [Axios - HTTP Client](#7-axios)

### Styling & UI
8. [Tailwind CSS](#8-tailwind-css)
9. [shadcn/ui](#9-shadcnui)
10. [Dark Mode (next-themes)](#10-dark-mode)

### Zaawansowane
11. [Custom Hooks](#11-custom-hooks)
12. [Error Handling](#12-error-handling)
13. [Environment Variables](#13-environment-variables)

---

## 1. React - Podstawy

### Co to jest React?

**React** to **biblioteka JavaScript** (nie framework!) do budowania interfejsów użytkownika. Stworzona przez Facebook/Meta w 2013.

```
Blazor (C#)          →    React (JavaScript/TypeScript)
Component-based      →    Component-based ✅
Razor syntax         →    JSX syntax
@code { }            →    JavaScript/TypeScript
```

### Komponenty - Podstawowa Jednostka

W React **wszystko jest komponentem**. Komponent to funkcja która zwraca UI.

```typescript
// Blazor
@page "/portfolio"
<h1>@portfolio.Name</h1>

@code {
    private Portfolio portfolio;
}
```

```typescript
// React
function PortfolioPage() {
  const portfolio = { name: 'My Portfolio' }
  
  return <h1>{portfolio.name}</h1>
}
```

### JSX - JavaScript + XML

JSX to składnia która wygląda jak HTML, ale to JavaScript.

```typescript
// To wygląda jak HTML
const element = <h1>Hello, World!</h1>

// Ale kompiluje się do:
const element = React.createElement('h1', null, 'Hello, World!')
```

**Różnice JSX vs HTML:**

| HTML | JSX |
|------|-----|
| `class="..."` | `className="..."` |
| `for="..."` | `htmlFor="..."` |
| `onclick="..."` | `onClick={...}` |
| `<input>` | `<input />` (self-closing) |

### Props - Przekazywanie Danych

Props to jak parametry funkcji - sposób na przekazanie danych do komponentu.

```typescript
// Definicja komponentu z props
interface ButtonProps {
  text: string
  onClick: () => void
  variant?: 'primary' | 'secondary'
}

function Button({ text, onClick, variant = 'primary' }: ButtonProps) {
  return (
    <button onClick={onClick} className={variant}>
      {text}
    </button>
  )
}

// Użycie
<Button text="Click me" onClick={() => alert('Clicked!')} variant="primary" />
```

**Analogia do C#:**

```csharp
// C# - parametry metody
void Button(string text, Action onClick, string variant = "primary") { }

// React - props
function Button({ text, onClick, variant = 'primary' }) { }
```

### State - Reaktywny Stan

**State** to dane które mogą się zmieniać. Gdy state się zmienia, React automatycznie przerenderowuje komponent.

```typescript
import { useState } from 'react'

function Counter() {
  // useState zwraca [wartość, funkcję do zmiany]
  const [count, setCount] = useState(0)
  
  return (
    <div>
      <p>Count: {count}</p>
      <button onClick={() => setCount(count + 1)}>
        Increment
      </button>
    </div>
  )
}
```

**Porównanie z Blazor:**

```csharp
// Blazor
@code {
    private int count = 0;
    
    private void Increment() {
        count++;
        StateHasChanged(); // Ręczne odświeżenie
    }
}
```

```typescript
// React
const [count, setCount] = useState(0)

function increment() {
  setCount(count + 1) // Automatyczne odświeżenie!
}
```

### Hooks - "Zaczepienie się" w React

Hooks to funkcje które pozwalają "zaczepić się" w lifecycle Reacta.

#### **useState** - Lokalny State

```typescript
const [name, setName] = useState('John')
const [isLoading, setIsLoading] = useState(false)
const [items, setItems] = useState<string[]>([])
```

#### **useEffect** - Side Effects

```typescript
useEffect(() => {
  // Kod wykona się PO renderze
  console.log('Component mounted or updated')
  
  // Cleanup (opcjonalne)
  return () => {
    console.log('Component will unmount')
  }
}, [dependency]) // Dependency array
```

**Dependency Array:**

```typescript
// [] - uruchom TYLKO raz (mount)
useEffect(() => { }, [])

// [count] - uruchom gdy count się zmieni
useEffect(() => { }, [count])

// Brak [] - uruchom PO KAŻDYM renderze
useEffect(() => { })
```

**Analogia do Blazor:**

```csharp
// Blazor
protected override void OnInitialized() { }
protected override void OnParametersSet() { }

// React
useEffect(() => { }, [])      // OnInitialized
useEffect(() => { }, [param]) // OnParametersSet
```

---

## 2. Next.js - Framework

### Co to jest Next.js?

**Next.js** to framework zbudowany **na bazie React**. Dodaje:
- ✅ Routing (bez instalacji React Router)
- ✅ Server-Side Rendering (SSR)
- ✅ Static Site Generation (SSG)
- ✅ API Routes
- ✅ Image optimization
- ✅ Built-in CSS/Tailwind support

```
React (biblioteka UI)
    ↓
Next.js (framework - React + routing + SSR + więcej)
    ↓
Nasza aplikacja
```

### App Router (Next.js 13+)

Next.js ma **file-based routing** - struktura folderów = struktura URL.

```
app/
├── page.tsx                    → /
├── about/
│   └── page.tsx                → /about
├── portfolio/
│   └── [id]/
│       └── page.tsx            → /portfolio/123
└── (auth)/                     → (route group - nie w URL)
    └── login/
        └── page.tsx            → /login
```

**Special Files:**

| Plik | Przeznaczenie |
|------|---------------|
| `page.tsx` | Strona (URL endpoint) |
| `layout.tsx` | Layout (wrapper dla dzieci) |
| `loading.tsx` | Loading state (Suspense) |
| `error.tsx` | Error boundary |
| `not-found.tsx` | 404 page |

### Server vs Client Components

Next.js ma **dwa typy komponentów**:

#### **Server Components (default)**

```typescript
// app/portfolio/[id]/page.tsx
// To jest SERVER component (brak 'use client')

async function PortfolioPage({ params }: { params: { id: string } }) {
  // Može być async!
  const portfolio = await getPortfolio(params.id)
  
  return <div>{portfolio.name}</div>
}
```

**Cechy:**
- ✅ Renderowane na serwerze
- ✅ Mogą być `async`
- ✅ Mogą bezpośrednio fetchować dane
- ❌ NIE mogą używać hooks (`useState`, `useEffect`)
- ❌ NIE mogą obsługiwać event handlers (`onClick`)

#### **Client Components**

```typescript
'use client' // WAŻNE - ta dyrektywa na górze!

import { useState } from 'react'

function Counter() {
  const [count, setCount] = useState(0) // ✅ OK
  
  return (
    <button onClick={() => setCount(count + 1)}> {/* ✅ OK */}
      Count: {count}
    </button>
  )
}
```

**Cechy:**
- ✅ Renderowane w przeglądarce
- ✅ Mogą używać hooks
- ✅ Mogą mieć interaktywność (onClick, onChange)
- ❌ NIE mogą być `async`

**Kiedy używać czego?**

| Użyj Server Component | Użyj Client Component |
|-----------------------|-----------------------|
| Fetch danych | State (`useState`) |
| Backend logic | Event handlers (onClick) |
| Dostęp do DB/API bezpośrednio | Interaktywność |
| SEO | Hooks (useEffect) |
| Brak interakcji | Przegląd-specific API |

### Layouts - Współdzielone UI

Layout to komponent "wrapper" dla stron.

```typescript
// app/layout.tsx - ROOT LAYOUT (dla całej aplikacji)
export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
      <body>
        <Providers>
          {children} {/* Tu renderują się strony */}
        </Providers>
      </body>
    </html>
  )
}

// app/(dashboard)/layout.tsx - NESTED LAYOUT
export default function DashboardLayout({ children }: { children: React.ReactNode }) {
  return (
    <div>
      <NavMenu />  {/* Nawigacja tylko dla dashboard */}
      <main>{children}</main>
    </div>
  )
}
```

**Hierarchia renderowania:**

```
app/layout.tsx (Root)
    ↓
app/(dashboard)/layout.tsx (Dashboard)
    ↓
app/(dashboard)/page.tsx (Home)
```

Wynik:
```tsx
<html>
  <body>
    <Providers>
      <div>
        <NavMenu />
        <main>
          <HomePage />
        </main>
      </div>
    </Providers>
  </body>
</html>
```

### Dynamic Routes - [param]

```
app/
└── portfolio/
    └── [id]/           ← Dynamic segment
        └── page.tsx
```

```typescript
// app/portfolio/[id]/page.tsx
interface PageProps {
  params: { id: string }
  searchParams: { [key: string]: string | string[] | undefined }
}

export default function PortfolioPage({ params }: PageProps) {
  const portfolioId = params.id // Dostęp do parametru URL
  
  return <div>Portfolio ID: {portfolioId}</div>
}

// URL: /portfolio/abc-123
// params.id = "abc-123"
```

### Route Groups - (nazwa)

Route groups organizują kod **bez wpływu na URL**.

```
app/
├── (auth)/             ← NIE pojawi się w URL
│   ├── login/
│   │   └── page.tsx    → /login (bez /auth/)
│   └── register/
│       └── page.tsx    → /register
└── (dashboard)/        ← NIE pojawi się w URL
    └── page.tsx        → / (bez /dashboard/)
```

**Po co?**
- Różne layouty dla różnych grup
- Organizacja kodu (wszystkie strony auth razem)

---

## 3. TypeScript w React

### Typing Props

```typescript
// Interface dla props
interface PortfolioCardProps {
  portfolio: Portfolio
  onDelete?: () => void  // Opcjonalne
  className?: string
}

function PortfolioCard({ portfolio, onDelete, className }: PortfolioCardProps) {
  return <div className={className}>{portfolio.name}</div>
}
```

### Typing State

```typescript
// Prosty typ
const [name, setName] = useState<string>('')

// Obiekt
const [user, setUser] = useState<User | null>(null)

// Array
const [portfolios, setPortfolios] = useState<Portfolio[]>([])

// Enum
const [status, setStatus] = useState<InvestmentStatus>(InvestmentStatus.Active)
```

### Typing Event Handlers

```typescript
function handleClick(event: React.MouseEvent<HTMLButtonElement>) {
  console.log(event.currentTarget)
}

function handleChange(event: React.ChangeEvent<HTMLInputElement>) {
  console.log(event.target.value)
}

function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
  event.preventDefault()
}
```

### Generic Components

```typescript
interface ListProps<T> {
  items: T[]
  renderItem: (item: T) => React.ReactNode
}

function List<T>({ items, renderItem }: ListProps<T>) {
  return (
    <ul>
      {items.map((item, index) => (
        <li key={index}>{renderItem(item)}</li>
      ))}
    </ul>
  )
}

// Użycie
<List<Portfolio>
  items={portfolios}
  renderItem={(p) => <span>{p.name}</span>}
/>
```

---

## 4. TanStack Query (React Query)

### Co to jest?

**React Query** to biblioteka do **zarządzania stanem asynchronicznym** (API calls, cache, synchronizacja).

**Bez React Query:**

```typescript
function PortfolioList() {
  const [portfolios, setPortfolios] = useState<Portfolio[]>([])
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<Error | null>(null)
  
  useEffect(() => {
    setIsLoading(true)
    getPortfolios(userId)
      .then(data => setPortfolios(data))
      .catch(err => setError(err))
      .finally(() => setIsLoading(false))
  }, [userId])
  
  // Ręczne cachowanie?
  // Ręczne refetching?
  // Ręczne error handling?
}
```

**Z React Query:**

```typescript
function PortfolioList() {
  const { data: portfolios, isLoading, error } = useQuery({
    queryKey: ['portfolios', userId],
    queryFn: () => getPortfolios(userId)
  })
  
  // Cache, refetching, error handling - wszystko automatyczne!
}
```

### useQuery - Fetch Data

```typescript
import { useQuery } from '@tanstack/react-query'

const { 
  data,           // Dane z API
  isLoading,      // Czy ładuje się PO RAZ PIERWSZY
  isFetching,     // Czy ładuje się (też w tle)
  error,          // Błąd (jeśli wystąpił)
  refetch         // Funkcja do ręcznego odświeżenia
} = useQuery({
  queryKey: ['portfolios', userId],  // Unikalny klucz (cache key)
  queryFn: () => getPortfolios(userId),  // Funkcja fetch
  staleTime: 5 * 60 * 1000,  // 5 min - dane "fresh"
  retry: 1,  // Ile razy próbować po błędzie
})
```

### Query Keys - Cache Strategy

Query Key identyfikuje dane w cache.

```typescript
// Klucz hierarchiczny
['portfolios']                    // Wszystkie portfele
['portfolios', userId]            // Portfele użytkownika
['portfolios', userId, 'active']  // Aktywne portfele użytkownika

['portfolio', portfolioId]        // Pojedynczy portfel
['portfolio', portfolioId, 'investments']  // Inwestycje portfela
```

**Invalidation:**

```typescript
// Invaliduj wszystkie queries zaczynające się od 'portfolios'
queryClient.invalidateQueries({ queryKey: ['portfolios'] })

// Invaliduj tylko konkretny portfel
queryClient.invalidateQueries({ queryKey: ['portfolio', portfolioId] })
```

### useMutation - Modify Data

```typescript
import { useMutation, useQueryClient } from '@tanstack/react-query'

function CreatePortfolioButton() {
  const queryClient = useQueryClient()
  
  const mutation = useMutation({
    mutationFn: createPortfolio,
    onSuccess: () => {
      // Po sukcesie - invalidate cache (odśwież listę)
      queryClient.invalidateQueries({ queryKey: ['portfolios'] })
    },
    onError: (error) => {
      alert(error.message)
    }
  })
  
  function handleCreate() {
    mutation.mutate({
      portfolioId: crypto.randomUUID(),
      name: 'New Portfolio',
      currency: 'USD'
    })
  }
  
  return (
    <button onClick={handleCreate} disabled={mutation.isPending}>
      {mutation.isPending ? 'Creating...' : 'Create Portfolio'}
    </button>
  )
}
```

### Optimistic Updates

Optymistyczne aktualizacje - UI zmienia się PRZED odpowiedzią z serwera.

```typescript
const mutation = useMutation({
  mutationFn: updatePortfolio,
  onMutate: async (newData) => {
    // 1. Anuluj wszystkie outgoing refetches
    await queryClient.cancelQueries({ queryKey: ['portfolios'] })
    
    // 2. Snapshot obecnego stanu (do rollback)
    const previous = queryClient.getQueryData(['portfolios'])
    
    // 3. Optymistycznie zaktualizuj UI
    queryClient.setQueryData(['portfolios'], (old: Portfolio[]) => {
      return old.map(p => p.id === newData.id ? { ...p, ...newData } : p)
    })
    
    return { previous }
  },
  onError: (err, newData, context) => {
    // Rollback przy błędzie
    queryClient.setQueryData(['portfolios'], context?.previous)
  },
  onSettled: () => {
    // Zawsze odśwież po zakończeniu
    queryClient.invalidateQueries({ queryKey: ['portfolios'] })
  }
})
```

### Query DevTools

React Query ma panel deweloperski:

```typescript
import { ReactQueryDevtools } from '@tanstack/react-query-devtools'

<QueryClientProvider client={queryClient}>
  <App />
  <ReactQueryDevtools initialIsOpen={false} />
</QueryClientProvider>
```

**Co pokazuje:**
- Wszystkie aktywne queries
- Status (fresh, stale, fetching, inactive)
- Cache content
- Network requests
- Możliwość ręcznego refetch/invalidate

---

## 5. Zustand - State Management

### Co to jest?

**Zustand** to lekka biblioteka do **globalnego state management**. Alternatywa dla Redux/Context API.

**Porównanie:**

```
Blazor: Fluxor (state management)
React: Zustand / Redux / Context API
```

### Tworzenie Store

```typescript
import { create } from 'zustand'

interface AuthState {
  token: string | null
  user: User | null
  isAuthenticated: boolean
  
  setAuth: (token: string, user: User) => void
  clearAuth: () => void
}

export const useAuthStore = create<AuthState>((set) => ({
  // Initial state
  token: null,
  user: null,
  isAuthenticated: false,
  
  // Actions
  setAuth: (token, user) => set({ token, user, isAuthenticated: true }),
  clearAuth: () => set({ token: null, user: null, isAuthenticated: false }),
}))
```

### Używanie Store

```typescript
function NavBar() {
  // Pobierz tylko to co potrzebujesz
  const user = useAuthStore((state) => state.user)
  const clearAuth = useAuthStore((state) => state.clearAuth)
  
  if (!user) return <LoginButton />
  
  return (
    <div>
      <span>{user.name}</span>
      <button onClick={clearAuth}>Logout</button>
    </div>
  )
}
```

**Optimization - wybieraj tylko potrzebne dane:**

```typescript
// ❌ ŹLE - re-render przy KAŻDEJ zmianie w store
const state = useAuthStore()

// ✅ DOBRZE - re-render tylko gdy user się zmieni
const user = useAuthStore(state => state.user)
```

### Persist Middleware - localStorage

```typescript
import { create } from 'zustand'
import { persist } from 'zustand/middleware'

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      token: null,
      user: null,
      setAuth: (token, user) => set({ token, user }),
      clearAuth: () => set({ token: null, user: null }),
    }),
    {
      name: 'auth-storage',  // Klucz w localStorage
      partialize: (state) => ({ 
        // Zapisz tylko token i user (nie całe state)
        token: state.token,
        user: state.user 
      }),
    }
  )
)
```

**Co to robi:**
- Zapisuje state do `localStorage`
- Przy odświeżeniu strony - przywraca state
- Można wybrać co zapisywać (`partialize`)

### Zustand vs React Query

| Zustand | React Query |
|---------|-------------|
| Globalny state UI | State asynchroniczny (API) |
| Przycisk toggle, theme, selected user | Portfolios, investments, user data |
| Persystencja (localStorage) | Cache + auto-refetch |
| Synchroniczny | Async by design |

**Przykład użycia:**

```typescript
// Zustand - UI state
const theme = useThemeStore(state => state.theme)
const selectedUserId = useUserStore(state => state.selectedUserId)

// React Query - Server state
const { data: portfolios } = useQuery({
  queryKey: ['portfolios', selectedUserId],
  queryFn: () => getPortfolios(selectedUserId)
})
```

---

**(Kontynuacja w następnej sekcji...)**

**Czy chcesz żebym kontynuował z pozostałymi sekcjami (React Hook Form, Axios, Tailwind, etc.)?** 🚀
