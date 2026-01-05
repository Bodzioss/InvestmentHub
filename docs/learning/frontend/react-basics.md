# React - Podstawy

> [!NOTE]
> Wprowadzenie do React dla .NET developerów. Porównania z Blazor gdzie ma sens.

## Spis Treści
- [Co to jest React?](#co-to-jest-react)
- [Komponenty](#komponenty)
- [JSX](#jsx)
- [Props](#props)
- [State](#state)
- [Hooks](#hooks)
- [Lifecycle](#lifecycle)

---

## Co to jest React?

**React** to **biblioteka JavaScript** (nie framework!) do budowania interfejsów użytkownika. Stworzona przez Facebook/Meta w 2013.

```
Blazor (C#)          →    React (JavaScript/TypeScript)
Component-based      →    Component-based ✅
Razor syntax         →    JSX syntax
@code { }            →    JavaScript/TypeScript
```

**Dlaczego React?**
- ✅ Najpopularniejsza biblioteka UI (więcej ofert pracy)
- ✅ Ogromny ekosystem (biblioteki, narzędzia)
- ✅ Virtual DOM (wydajność)
- ✅ Component-based (jak Blazor)
- ✅ Unidirectional data flow (przewidywalność)

---

## Komponenty

W React **wszystko jest komponentem**. Komponent to funkcja która zwraca UI.

### Functional Components (używamy)

```typescript
// Blazor
@page "/portfolio"
<h1>@portfolio.Name</h1>

@code {
    private Portfolio portfolio = new() { Name = "My Portfolio" };
}
```

```typescript
// React
function PortfolioPage() {
  const portfolio = { name: 'My Portfolio' }
  
  return <h1>{portfolio.name}</h1>
}
```

### Anatomia Komponentu

```typescript
import { useState } from 'react' // 1. Imports

// 2. Props interface (TypeScript)
interface PortfolioCardProps {
  portfolio: Portfolio
  onDelete?: () => void
}

// 3. Component function
function PortfolioCard({ portfolio, onDelete }: PortfolioCardProps) {
  // 4. Hooks (state, effects)
  const [isExpanded, setIsExpanded] = useState(false)
  
  // 5. Event handlers
  function handleToggle() {
    setIsExpanded(!isExpanded)
  }
  
  // 6. JSX return
  return (
    <div onClick={handleToggle}>
      <h2>{portfolio.name}</h2>
      {isExpanded && <p>{portfolio.description}</p>}
      {onDelete && <button onClick={onDelete}>Delete</button>}
    </div>
  )
}

// 7. Export
export default PortfolioCard
```

### Class Components (legacy - nie używamy)

```typescript
// Stary sposób - NIE używamy
class PortfolioCard extends React.Component {
  constructor(props) {
    super(props)
    this.state = { isExpanded: false }
  }
  // ...
}
```

**Dlaczego functional components?**
- ✅ Prostsze
- ✅ Mniej kodu
- ✅ Hooks (useState, useEffect)
- ✅ Łatwiejsze testowanie

---

## JSX

JSX to składnia która wygląda jak HTML, ale to **JavaScript**.

### Podstawy

```typescript
// To wygląda jak HTML
const element = <h1>Hello, World!</h1>

// Ale kompiluje się do:
const element = React.createElement('h1', null, 'Hello, World!')
```

### Różnice JSX vs HTML

| HTML | JSX | Dlaczego? |
|------|-----|-----------|
| `class="btn"` | `className="btn"` | `class` to keyword w JS |
| `for="input"` | `htmlFor="input"` | `for` to keyword w JS |
| `onclick="..."` | `onClick={...}` | camelCase + funkcja |
| `<input>` | `<input />` | Self-closing wymagane |
| `style="color: red"` | `style={{ color: 'red' }}` | Obiekt JS |

### Wyrażenia w JSX

```typescript
const name = 'John'
const count = 5

// {} - JavaScript expression
<h1>Hello, {name}!</h1>
<p>Count: {count * 2}</p>
<div className={count > 10 ? 'high' : 'low'}>
  {count > 10 ? 'High' : 'Low'}
</div>
```

### Conditional Rendering

```typescript
// 1. && operator
{isLoggedIn && <LogoutButton />}

// 2. Ternary
{isLoggedIn ? <LogoutButton /> : <LoginButton />}

// 3. If statement (poza JSX)
function Greeting({ isLoggedIn }: { isLoggedIn: boolean }) {
 if (isLoggedIn) {
    return <h1>Welcome back!</h1>
  }
  return <h1>Please sign in.</h1>
}
```

### Lists

```typescript
const portfolios = [
  { id: '1', name: 'Portfolio A' },
  { id: '2', name: 'Portfolio B' }
]

// map() - pętla w JSX
<ul>
  {portfolios.map((portfolio) => (
    <li key={portfolio.id}>{portfolio.name}</li>
  ))}
</ul>
```

**WAŻNE: `key` prop**
- Unikalny identifier dla każdego elementu listy
- React używa do optymalizacji re-renderów
- ❌ NIE używaj index jako key (chyba że lista się nie zmienia)

---

## Props

Props to **parametry komponentu** - sposób na przekazanie danych do komponentu.

### Podstawowe użycie

```typescript
// Definicja
interface ButtonProps {
  text: string
  onClick: () => void
  variant?: 'primary' | 'secondary'  // Opcjonalne
}

function Button({ text, onClick, variant = 'primary' }: ButtonProps) {
  return (
    <button onClick={onClick} className={variant}>
      {text}
    </button>
  )
}

// Użycie
<Button 
  text="Click me" 
  onClick={() => alert('Clicked!')} 
  variant="primary" 
/>
```

### Props są IMMUTABLE

```typescript
function Button({ text }: { text: string }) {
  // ❌ ŹLE - nie możesz modyfikować props!
  text = 'New text'
  
  // ✅ DOBRZE - użyj state jeśli chcesz zmieniać
  const [buttonText, setButtonText] = useState(text)
}
```

### Children Prop

```typescript
interface CardProps {
  children: React.ReactNode  // Specjalny prop
  title: string
}

function Card({ children, title }: CardProps) {
  return (
    <div className="card">
      <h2>{title}</h2>
      {children}  {/* Zawartość między tagami */}
    </div>
  )
}

// Użycie
<Card title="My Card">
  <p>This is content inside the card</p>
  <button>Click</button>
</Card>
```

### Analogia do C#

```csharp
// C# - parametry metody
void Button(string text, Action onClick, string variant = "primary") { }

// React - props
function Button({ text, onClick, variant = 'primary' }) { }
```

---

## State

**State** to dane które mogą się zmieniać. Gdy state się zmienia, React automatycznie przerenderowuje komponent.

### useState Hook

```typescript
import { useState } from 'react'

function Counter() {
  // [wartość, funkcja do zmiany]
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

### Różne typy state

```typescript
// String
const [name, setName] = useState<string>('')

// Number
const [count, setCount] = useState<number>(0)

// Boolean
const [isLoading, setIsLoading] = useState<boolean>(false)

// Object
const [user, setUser] = useState<User | null>(null)

// Array
const [items, setItems] = useState<string[]>([])
```

### Aktualizacja state

```typescript
// ✅ DOBRZE - nowa wartość
setCount(5)

// ✅ DOBRZE - funkcja (async-safe)
setCount(prevCount => prevCount + 1)

// ❌ ŹLE - mutacja
count = 5  // To NIE zadziała!
```

### State z obiektami

```typescript
const [user, setUser] = useState({ name: 'John', age: 30 })

// ❌ ŹLE - mutacja
user.name = 'Jane'

// ✅ DOBRZE - nowy obiekt (spread operator)
setUser({ ...user, name: 'Jane' })
```

### Porównanie z Blazor

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

---

## Hooks

Hooks to funkcje które pozwalają "zaczepić się" w lifecycle Reacta i używać state w functional components.

### Zasady Hooks

1. ✅ Wywołuj TYLKO na top-level (nie w if/for/funkcjach zagnieżdżonych)
2. ✅ Wywołuj TYLKO w functional components lub custom hooks
3. ✅ Nazywaj z prefiksem `use` (np. `useAuth`)

```typescript
// ❌ ŹLE
if (isLoggedIn) {
  const [name, setName] = useState('') // Hook w if!
}

// ✅ DOBRZE
const [name, setName] = useState('')
if (isLoggedIn) {
  // Użyj name tutaj
}
```

### useState - State lokalny

```typescript
const [state, setState] = useState(initialValue)
```

### useEffect - Side Effects

```typescript
useEffect(() => {
  // Kod wykona się PO renderze
  console.log('Component rendered')
  
  // Cleanup (opcjonalne) - execute przed unmount
  return () => {
    console.log('Component will unmount')
  }
}, [dependency]) // Dependency array
```

**Dependency Array:**

```typescript
// [] - uruchom TYLKO raz (mount)
useEffect(() => {
  console.log('Mounted')
}, [])

// [count] - uruchom gdy count się zmieni
useEffect(() => {
  console.log('Count changed:', count)
}, [count])

// Brak [] - uruchom PO KAŻDYM renderze (rzadko używane)
useEffect(() => {
  console.log('Every render')
})
```

**Przykład - fetch danych:**

```typescript
function PortfolioList({ userId }: { userId: string }) {
  const [portfolios, setPortfolios] = useState<Portfolio[]>([])
  const [isLoading, setIsLoading] = useState(false)
  
  useEffect(() => {
    setIsLoading(true)
    
    getPortfolios(userId)
      .then(data => setPortfolios(data))
      .catch(err => console.error(err))
      .finally(() => setIsLoading(false))
  }, [userId]) // Re-fetch gdy userId się zmieni
  
  if (isLoading) return <div>Loading...</div>
  
  return (
    <ul>
      {portfolios.map(p => <li key={p.id}>{p.name}</li>)}
    </ul>
  )
}
```

### useRef - Referencja do DOM

```typescript
import { useRef } from 'react'

function InputFocus() {
  const inputRef = useRef<HTMLInputElement>(null)
  
  function handleFocus() {
    inputRef.current?.focus() // Bezpośredni dostęp do DOM
  }
  
  return (
    <>
      <input ref={inputRef} type="text" />
      <button onClick={handleFocus}>Focus Input</button>
    </>
  )
}
```

### useMemo - Memoizacja wartości

```typescript
import { useMemo } from 'react'

function ExpensiveComponent({ items }: { items: number[] }) {
  // Oblicz TYLKO gdy items się zmieni
  const total = useMemo(() => {
    console.log('Calculating total...') // Debug
    return items.reduce((sum, item) => sum + item, 0)
  }, [items])
  
  return <div>Total: {total}</div>
}
```

### useCallback - Memoizacja funkcji

```typescript
import { useCallback } from 'react'

function Parent() {
  const [count, setCount] = useState(0)
  
  // Funkcja się NIE zmienia (ta sama referencja)
  const handleClick = useCallback(() => {
    setCount(c => c + 1)
  }, []) // Pusta array - funkcja nigdy się nie zmienia
  
  return <Child onClick={handleClick} />
}
```

---

## Lifecycle

Lifecycle komponentu to etapy jego "życia" w aplikacji.

### Lifecycle Phases

```
Mount → Update → Unmount
  ↓       ↓         ↓
useState useEffect cleanup
         (re-run)
```

### Mount (pierwszy render)

```typescript
useEffect(() => {
  console.log('Component mounted')
  // Fetch data, subscribe, initialize
}, []) // Pusta array!
```

**Analogia do Blazor:**
```csharp
protected override void OnInitialized() { }
```

### Update (re-render)

```typescript
useEffect(() => {
  console.log('Count changed:', count)
}, [count]) // Re-run gdy count się zmieni
```

**Analogia do Blazor:**
```csharp
protected override void OnParametersSet() { }
```

### Unmount (usunięcie z DOM)

```typescript
useEffect(() => {
  const subscription = subscribeToData()
  
  // Cleanup function
  return () => {
    subscription.unsubscribe()
    console.log('Component unmounted')
  }
}, [])
```

**Analogia do Blazor:**
```csharp
public void Dispose() { }
```

### Pełny przykład

```typescript
function Timer() {
  const [seconds, setSeconds] = useState(0)
  
  useEffect(() => {
    console.log('Timer mounted')
    
    // Start timer
    const interval = setInterval(() => {
      setSeconds(s => s + 1)
    }, 1000)
    
    // Cleanup - stop timer on unmount
    return () => {
      console.log('Timer unmounted')
      clearInterval(interval)
    }
  }, []) // Mount only
  
  return <div>Seconds: {seconds}</div>
}
```

---

## Przykłady z InvestmentHub

### Portfolio Card Component

```typescript
// src/investment-hub-react/components/portfolio/portfolio-card.tsx
interface PortfolioCardProps {
  portfolio: Portfolio
  onDelete?: () => void
}

export function PortfolioCard({ portfolio, onDelete }: PortfolioCardProps) {
  const [isExpanded, setIsExpanded] = useState(false)
  
  return (
    <div className="card">
      <h3>{portfolio.name}</h3>
      <p>Value: {portfolio.totalValue?.amount} {portfolio.totalValue?.currency}</p>
      
      <button onClick={() => setIsExpanded(!isExpanded)}>
        {isExpanded ? 'Collapse' : 'Expand'}
      </button>
      
      {isExpanded && (
        <div>
          <p>Investments: {portfolio.activeInvestmentCount}</p>
          <p>Created: {new Date(portfolio.createdDate).toLocaleDateString()}</p>
        </div>
      )}
      
      {onDelete && (
        <button onClick={onDelete}>Delete</button>
      )}
    </div>
  )
}
```

---

## Podsumowanie

### Kluczowe Koncepty

| Koncept | Co robi | Kiedy używać |
|---------|---------|--------------|
| **Props** | Przekazuje dane do komponentu | Zawsze gdy komponent potrzebuje danych z rodzica |
| **State** | Przechowuje dane które się zmieniają | Gdy dane mogą się zmienić (input, toggle, fetch) |
| **useEffect** | Side effects po renderze | Fetch data, subscribe, DOM manipulation |
| **JSX** | JavaScript + XML | Renderowanie UI |

### Best Practices

1. ✅ Jeden komponent = jedna odpowiedzialność
2. ✅ Props są immutable
3. ✅ State tylko gdy potrzebne
4. ✅ useEffect z dependency array
5. ✅ Key prop w listach
6. ✅ TypeScript types dla props

### Pytania rekrutacyjne

| Pytanie | Odpowiedź |
|---------|-----------|
| Co to jest Virtual DOM? | Reprezentacja DOM w pamięci, React porównuje i aktualizuje tylko zmiany |
| Props vs State? | Props - dane z rodzica (immutable), State - lokalne dane (mutable) |
| Kiedy używać useEffect? | Side effects: fetch, subscribe, DOM manipulation |
| Co to jest Hook? | Funkcja pozwalająca używać React features w functional components |
| Dlaczego key w listach? | React identyfikuje elementy, optymalizuje re-renders |

---

**Następny krok:** [Next.js Framework](../nextjs.md) - routing, layouts, Server Components

**Zobacz też:**
- [TypeScript w React](../typescript.md)
- [State Management](../state-management.md)
