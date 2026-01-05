# InvestmentHub - Learning Documentation

> [!NOTE]
> Kompletna dokumentacja techniczna projektu InvestmentHub - Backend (.NET), Frontend (React), Infrastructure i Best Practices

---

## 📚 Dokumentacja

### Backend (.NET)
- **[Backend Core](./backend/backend-core.md)** - .NET, CQRS, Event Sourcing, MediatR
- **[Domain-Driven Design](./backend/ddd.md)** - Aggregates, Value Objects, Domain Events
- **[Infrastructure](./backend/infrastructure.md)** - PostgreSQL, Marten, Redis, RabbitMQ
- **[API Design](./backend/api-design.md)** - REST, Controllers, Middleware

### Frontend (React)
- **[React Basics](./frontend/react-basics.md)** - Components, Hooks, JSX, Props, State
- **[Next.js Framework](./frontend/nextjs.md)** - App Router, Server/Client Components, Routing
- **[TypeScript](./frontend/typescript.md)** - Types, Interfaces, Generics w React
- **[State Management](./frontend/state-management.md)** - React Query (TanStack Query), Zustand
- **[Forms & Validation](./frontend/forms.md)** - React Hook Form, Zod
- **[Styling](./frontend/styling.md)** - Tailwind CSS, shadcn/ui, Dark Mode
- **[API Integration](./frontend/api-integration.md)** - Axios, Interceptors, Error Handling

### Infrastructure & DevOps
- **[Database](./infrastructure/database.md)** - PostgreSQL, Marten Event Store, Migrations
- **[Caching](./infrastructure/caching.md)** - Redis, Distributed Cache
- **[Messaging](./infrastructure/messaging.md)** - RabbitMQ, MassTransit
- **[Deployment](./infrastructure/deployment.md)** - Docker, Azure, CI/CD

### Best Practices
- **[Project Structure](./best-practices/project-structure.md)** - Folder organization, File naming
- **[Code Patterns](./best-practices/patterns.md)** - Design Patterns używane w projekcie
- **[Testing](./best-practices/testing.md)** - Unit, Integration, E2E tests
- **[Performance](./best-practices/performance.md)** - Optymalizacje, Caching strategies

---

## 🎯 Dla kogo jest ta dokumentacja?

### Jesteś .NET Developerem uczącym się React?
Zacznij od:
1. [React Basics](./frontend/react-basics.md) - zrozum komponenty i hooks
2. [Next.js Framework](./frontend/nextjs.md) - poznaj App Router
3. [State Management](./frontend/state-management.md) - React Query (jak Fluxor w Blazor)

### Jesteś React Developerem uczącym się .NET?
Zacznij od:
1. [Backend Core](./backend/backend-core.md) - CQRS, Event Sourcing
2. [DDD](./backend/ddd.md) - Domain-Driven Design patterns
3. [Infrastructure](./backend/infrastructure.md) - PostgreSQL, Event Store

### Chcesz zrozumieć całą architekturę?
Przeczytaj w kolejności:
1. [Backend Core](./backend/backend-core.md) - jak działa backend
2. [Project Structure](./best-practices/project-structure.md) - organizacja kodu
3. [React Basics](./frontend/react-basics.md) - jak działa frontend
4. [API Integration](./frontend/api-integration.md) - jak frontend łączy się z backendem

---

## 🔄 Port z Blazor na React - Mapa Konceptów

| Blazor (.NET) | React (JavaScript/TypeScript) |
|---------------|-------------------------------|
| `@code { }` | `useState`, `useEffect` |
| `@inject` | `useContext`, Zustand, Props |
| Fluxor (State) | Zustand, React Query |
| Razor Components | React Components (functional) |
| `@page "/path"` | Next.js file-based routing |
| `OnInitialized()` | `useEffect(() => {}, [])` |
| `StateHasChanged()` | Automatyczne (React) |
| CSS Isolation | Tailwind CSS, CSS Modules |
| MudBlazor | shadcn/ui, Radix UI |

---

## 📖 Jak używać tej dokumentacji?

### Podczas rozwoju
Każdy plik dokumentacji opisuje **konkretny temat** z:
- ✅ Wyjaśnieniem konceptu
- ✅ Przykładami z projektu InvestmentHub
- ✅ Porównaniem C# ↔ TypeScript (gdzie ma sens)
- ✅ Best practices
- ✅ Pułapki (co unikać)

### Podczas nauki
- Czytaj po kolei (od basics do advanced)
- Exp experimentuj z kodem w projekcie
- Odnoszące się do plików w projekcie

### Jako referencja
- Ctrl+F w danym pliku
- Sprawdź Table of Contents na górze każdego pliku
- Linki między plikami prowadzą do powiązanych tematów

---

## 🚀 Quick Start

### Backend (Pierwsze uruchomienie)
```bash
# 1. Uruchom .NET Aspire
cd host/InvestmentHub.AppHost
dotnet run

# 2. Sprawdź dashboard
# http://localhost:15888
```

### Frontend (Pierwsze uruchomienie)
```bash
# 1. Zainstaluj zależności
cd src/investment-hub-react
npm install

# 2. Uruchom dev server
npm run dev

# 3. Otwórz w przeglądarce
# http://localhost:3000
```

---

## 📝 Status Dokumentacji

| Sekcja | Status | Ostatnia aktualizacja |
|--------|--------|----------------------|
| Backend Core | ✅ Kompletne | 2025-12 |
| React Basics | 🟡 W trakcie | 2025-12-15 |
| Next.js | 🟡 W trakcie | 2025-12-15 |
| State Management | 🟡 W trakcie | 2025-12-15 |
| Forms | ⏳ Planowane | - |
| Styling | ⏳ Planowane | - |
| API Integration | ⏳ Planowane | - |

---

## 🤝 Contributing

Ta dokumentacja jest żywa i rozwija się wraz z projektem. Jeśli:
- Znalazłeś błąd
- Masz sugestię
- Chcesz dodać wyjaśnienie

Po prostu zaktualizuj odpowiedni plik .md!
