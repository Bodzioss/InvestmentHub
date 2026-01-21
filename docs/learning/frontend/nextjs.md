# Next.js Framework

> [!NOTE]
> Next.js to framework zbudowany na React - routing, SSR, Server Components. To dokument pełniejszy zostanie written po implementacji stron.

## Quick Reference

### App Router Structure

```
app/
├── layout.tsx              # Root layout
├── page.tsx                # Home page (/)
├── (auth)/                 # Route group
│   └── login/
│       └── page.tsx        # /login
└── portfolio/
    └── [id]/               # Dynamic route
        └── page.tsx        # /portfolio/:id
```

### Server vs Client Components

| Server Component | Client Component |
|------------------|------------------|
| Default | `'use client'` na górze |
| Może być `async` | Nie może być `async` |
| Brak hooks | useState, useEffect, etc. |
| Brak event handlers | onClick, onChange, etc. |

### Dynamic Routes

```typescript
// app/portfolio/[id]/page.tsx
export default function PortfolioPage({ params }: { params: { id: string } }) {
  const portfolioId = params.id
  return <div>Portfolio: {portfolioId}</div>
}
```

---

**Pełna dokumentacja będzie dodana po implementacji pierwszych stron.**

**Zobacz też:**
- [React Basics](./react-basics.md)
- [Project Structure](../best-practices/project-structure.md)
