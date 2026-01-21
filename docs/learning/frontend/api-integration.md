# API Integration z Axios

> [!NOTE]
> Jak frontend komunikuje się z backendem - Axios, interceptory, error handling

## Spis Treści
- [Axios - HTTP Client](#axios-http-client)
- [API Client Configuration](#api-client-configuration)
- [API Functions](#api-functions)
- [Error Handling](#error-handling)
- [Integration z React Query](#integration-z-react-query)

---

## Axios - HTTP Client

**Axios** to popularny HTTP client dla JavaScript. Alternatywa dla `fetch()`.

**Dlaczego Axios zamiast fetch?**
- ✅ Automatyczne JSON parsing
- ✅ Interceptory (request/response)
- ✅ Cancel requests
- ✅ Better error handling
- ✅ TypeScript support

### Instalacja

```bash
npm install axios
```

### Podstawowe użycie

```typescript
import axios from 'axios'

// GET
const response = await axios.get('/api/portfolios')
const portfolios = response.data

// POST
await axios.post('/api/portfolios', {
  name: 'My Portfolio',
  currency: 'USD'
})

// PUT
await axios.put(`/api/portfolios/${id}`, {
  name: 'Updated Name'
})

// DELETE
await axios.delete(`/api/portfolios/${id}`)
```

---

## API Client Configuration

### Tworzenie Instance

```typescript
// lib/api/client.ts
import axios, { AxiosInstance } from 'axios'

const apiClient: AxiosInstance = axios.create({
  baseURL: process.env.NEXT_PUBLIC_API_URL,  // http://localhost:5000
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 30000, // 30 sekund
})

export default apiClient
```

**Co to robi?**
- Tworzy "skonfigurowanego" klienta HTTP
- Wszystkie requesty używają tego samego base URL i headersów
- Timeout dla wszystkich requestów

### Request Interceptor - Dodanie JWT

```typescript
// lib/api/client.ts
import { STORAGE_KEYS } from '../constants'

apiClient.interceptors.request.use(
  (config) => {
    // Pobierz token z localStorage
    const token = localStorage.getItem(STORAGE_KEYS.AUTH_TOKEN)
    
    // Dodaj do headersów JEŚLI istnieje
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`
    }
    
    return config
  },
  (error) => {
    return Promise.reject(error)
  }
)
```

**Co to robi?**
- **Request interceptor** uruchamia się **PRZED** każdym requestem
- Automatycznie dodaje `Authorization: Bearer <token>` header
- Nie musisz ręcznie dodawać tokenu w każdym API call!

**Przepływ:**

```
1. apiClient.get('/api/portfolios')
       ↓
2. Request Interceptor
       ↓ (dodaje Authorization header)
3. HTTP Request → Backend
```

### Response Interceptor - Error Handling

```typescript
// lib/api/client.ts
apiClient.interceptors.response.use(
  (response) => {
    // Jeśli sukces (200-299), zwróć response
    return response
  },
  (error: AxiosError<ApiError>) => {
    // Obsługa błędów
    if (error.response) {
      const status = error.response.status
      
      switch (status) {
        case 401: // Unauthorized
          // Token wygasł - usuń i przekieruj do logowania
          localStorage.removeItem(STORAGE_KEYS.AUTH_TOKEN)
          
          if (typeof window !== 'undefined' && !window.location.pathname.includes('/login')) {
            window.location.href = '/login'
          }
          break
          
        case 403: // Forbidden
          console.error('Access forbidden')
          break
          
        case 404: // Not Found
          console.error('Resource not found')
          break
          
        case 500: // Server Error
          console.error('Server error')
          break
      }
    }
    
    return Promise.reject(error)
  }
)
```

**Co to robi?**
- **Response interceptor** uruchamia się **PO KAŻDEJ** odpowiedzi z API
- Globalnie obsługuje błędy (401, 403, 500)
- Automatyczne wylogowanie przy 401
- Nie musisz sprawdzać błędów w każdym komponencie!

---

## API Functions

### Structure

```
lib/api/
├── client.ts          # Axios instance + interceptors
├── auth.ts            # Login, register, changePassword
├── portfolios.ts      # CRUD portfolios
├── investments.ts     # CRUD investments
├── market-data.ts     # Search instruments, prices
├── users.ts           # Admin - get users
└── index.ts           # Barrel export
```

### Auth API

```typescript
// lib/api/auth.ts
import apiClient from './client'
import type { LoginRequest, LoginResponse, User } from '../types'

export async function login(request: LoginRequest): Promise<LoginResponse> {
  const response = await apiClient.post<LoginResponse>('/api/auth/login', request)
  return response.data
}

export async function getCurrentUser(): Promise<User> {
  const response = await apiClient.get<User>('/api/auth/me')
  return response.data
}
```

### Portfolios API

```typescript
// lib/api/portfolios.ts
import apiClient from './client'
import type { Portfolio, CreatePortfolioRequest } from '../types'

export async function getPortfolios(userId: string): Promise<Portfolio[]> {
  const response = await apiClient.get<Portfolio[]>('/api/portfolios', {
    params: { userId }
  })
  return response.data
}

export async function createPortfolio(request: CreatePortfolioRequest): Promise<void> {
  await apiClient.post('/api/portfolios', request)
}

export async function deletePortfolio(portfolioId: string): Promise<void> {
  await apiClient.delete(`/api/portfolios/${portfolioId}`)
}
```

### Barrel Export

```typescript
// lib/api/index.ts
export * from './auth'
export * from './portfolios'
export * from './investments'
export * from './market-data'
export * from './users'
export { default as apiClient } from './client'
```

**Użycie:**

```typescript
// Zamiast
import { getPortfolios } from '@/lib/api/portfolios'
import { login } from '@/lib/api/auth'

// Możesz
import { getPortfolios, login } from '@/lib/api'
```

---

## Error Handling

### Helper Functions

```typescript
// lib/api/client.ts
import axios from 'axios'
import type { ApiError } from '../types'

/**
 * Wyciąga error message z Axios error
 */
export function getErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    const apiError = error.response?.data as ApiError
    return apiError?.error || error.message || 'An error occurred'
  }
  
  if (error instanceof Error) {
    return error.message
  }
  
  return 'An unknown error occurred'
}

/**
 * Sprawdza czy błąd to 401 (Unauthorized)
 */
export function isUnauthorizedError(error: unknown): boolean {
  return axios.isAxiosError(error) && error.response?.status === 401
}
```

### Użycie w komponencie

```typescript
import { getPortfolios, getErrorMessage } from '@/lib/api'

function PortfolioList() {
  const [error, setError] = useState<string | null>(null)
  
  async function loadPortfolios() {
    try {
      const data = await getPortfolios(userId)
      setPortfolios(data)
    } catch (err) {
      // Helper function wyciąga message
      setError(getErrorMessage(err))
    }
  }
  
  if (error) {
    return <div className="error">{error}</div>
  }
  
  // ...
}
```

---

## Integration z React Query

React Query + Axios = 💪

### Custom Hook

```typescript
// lib/hooks/use-portfolios.ts
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { getPortfolios, createPortfolio, deletePortfolio } from '@/lib/api'
import { QUERY_KEYS } from '@/lib/constants'

export function usePortfolios(userId: string) {
  return useQuery({
    queryKey: [QUERY_KEYS.PORTFOLIOS, userId],
    queryFn: () => getPortfolios(userId),
    enabled: !!userId, // Fetch tylko gdy userId istnieje
  })
}

export function useCreatePortfolio() {
  const queryClient = useQueryClient()
  
  return useMutation({
    mutationFn: createPortfolio,
    onSuccess: () => {
      // Invalidate cache - odśwież listę
      queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.PORTFOLIOS] })
    }
  })
}

export function useDeletePortfolio() {
  const queryClient = useQueryClient()
  
  return useMutation({
    mutationFn: deletePortfolio,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.PORTFOLIOS] })
    }
  })
}
```

### Użycie w komponencie

```typescript
import { usePortfolios, useCreatePortfolio } from '@/lib/hooks/use-portfolios'

function PortfolioPage({ userId }: { userId: string }) {
  const { data: portfolios, isLoading, error } = usePortfolios(userId)
  const createMutation = useCreatePortfolio()
  
  async function handleCreate() {
    await createMutation.mutateAsync({
      portfolioId: crypto.randomUUID(),
      ownerId: userId,
      name: 'New Portfolio',
      currency: 'USD'
    })
  }
  
  if (isLoading) return <div>Loading...</div>
  if (error) return <div>Error: {error.message}</div>
  
  return (
    <div>
      <button onClick={handleCreate} disabled={createMutation.isPending}>
        {createMutation.isPending ? 'Creating...' : 'Create Portfolio'}
      </button>
      
      <ul>
        {portfolios?.map(p => <li key={p.id}>{p.name}</li>)}
      </ul>
    </div>
  )
}
```

---

## Przykłady z InvestmentHub

### Pełny przepływ - Login

```typescript
// 1. API Function
// lib/api/auth.ts
export async function login(request: LoginRequest): Promise<LoginResponse> {
  const response = await apiClient.post<LoginResponse>('/api/auth/login', request)
  return response.data
}

// 2. Custom Hook
// lib/hooks/use-auth.ts
export function useLogin() {
  const { setAuth } = useAuthStore()
  const router = useRouter()
  
  return useMutation({
    mutationFn: login,
    onSuccess: (data) => {
      // Zapisz token i user w store
      setAuth(data.token, data.user)
      // Przekieruj do dashboard
      router.push('/')
    }
  })
}

// 3. Komponent
// app/(auth)/login/page.tsx
export default function LoginPage() {
  const loginMutation = useLogin()
  const [formData, setFormData] = useState({ email: '', password: '' })
  
  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    await loginMutation.mutateAsync(formData)
  }
  
  return (
    <form onSubmit={handleSubmit}>
      <input 
        type="email" 
        value={formData.email}
        onChange={(e) => setFormData({ ...formData, email: e.target.value })}
      />
      <input 
        type="password"
        value={formData.password}
        onChange={(e) => setFormData({ ...formData, password: e.target.value })}
      />
      <button type="submit" disabled={loginMutation.isPending}>
        {loginMutation.isPending ? 'Logging in...' : 'Login'}
      </button>
      
      {loginMutation.error && (
        <div className="error">{getErrorMessage(loginMutation.error)}</div>
      )}
    </form>
  )
}
```

**Przepływ:**

```
User submits form
      ↓
handleSubmit()
      ↓
loginMutation.mutateAsync(formData)
      ↓
login(formData)  [lib/api/auth.ts]
      ↓
apiClient.post('/api/auth/login', ...)
      ↓
Request Interceptor (dodaje headers)
      ↓
HTTP POST → Backend API
      ↓
Backend → Response
      ↓
Response Interceptor (sprawdza błędy)
      ↓
useMutation onSuccess
      ↓
setAuth(token, user) → Zustand store
      ↓
router.push('/') → Przekierowanie
```

---

## Podsumowanie

### Struktura API Layer

```
Types (lib/types/index.ts)
      ↓
API Functions (lib/api/*.ts)
      ↓
Custom Hooks (lib/hooks/*.ts)
      ↓
Components
```

### Best Practices

1. ✅ Jeden axios instance dla całej aplikacji
2. ✅ Interceptory dla globalnej logiki (auth, errors)
3. ✅ TypeScript types dla request/response
4. ✅ Barrel exports (`index.ts`) dla czystych importów
5. ✅ Custom hooks dla integration z React Query
6. ✅ Error handling helpers

### Analogia do .NET

| .NET | React |
|------|-------|
| `HttpClient` | Axios |
| `HttpClientFactory` | axios.create() |
| `DelegatingHandler` | Interceptors |
| `IApiClient` (Refit) | API Functions |
| `try/catch` | Try/catch + error helpers |

---

**Zobacz też:**
- [State Management](./state-management.md) - React Query w szczegółach
- [React Basics](./react-basics.md) - useState, useEffect
