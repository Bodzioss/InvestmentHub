# 0005. Frontend Stack - Next.js with App Router

* Status: accepted
* Date: 2026-05-07

## Context and Problem Statement

For the InvestmentHub frontend, we needed to select a robust framework to replace or supersede the legacy Blazor WebAssembly implementation. The application is a highly interactive, data-heavy dashboard (portfolio management, real-time charts, market data). We required a stack that offers excellent developer experience, a strong ecosystem for UI components and data visualization, solid performance, and seamless integration with modern tooling.

## Decision Drivers

* **Ecosystem & UI Libraries**: Access to a vast ecosystem of high-quality, accessible UI components (e.g., shadcn/ui, Radix UI) and charting libraries (e.g., Recharts) which are crucial for a financial dashboard.
* **Performance & Data Fetching**: The need for efficient data fetching, aggressive caching, and robust state management, especially given the complex financial data and potential real-time requirements.
* **Developer Experience (DX)**: Fast refresh, strong TypeScript support, and a well-structured routing system.
* **Future-proofing**: Adopting a modern, actively maintained framework with a large, vibrant community.

## Considered Options

* **Blazor WebAssembly (Legacy)**: Continuing with the existing C#-based Single Page Application. 
  * *Pros:* Shared C# code with the backend. 
  * *Cons:* Slower initial load times, a less mature UI ecosystem compared to the JavaScript/TypeScript world, and a steeper learning curve for purely frontend-focused tasks.
* **React (Vite / CRA)**: A standard Client-Side Rendered (CSR) React application. 
  * *Pros:* Huge ecosystem, familiar to most frontend developers. 
  * *Cons:* Requires setting up custom routing (React Router) and data fetching architectures from scratch.
* **Next.js with App Router**: A full-stack React framework by Vercel. 
  * *Pros:* Built-in routing, React Server Components (RSC), optimized data fetching, fantastic DX, and native support for modern React 19 features.

## Decision Outcome

Chosen option: **Next.js with App Router**, because it provides the most comprehensive and modern toolkit for building complex React applications. It offers an unparalleled ecosystem of UI components (like shadcn/ui) that are critical for quickly building a premium dashboard application.

### Positive Consequences

* **UI/UX Excellence**: We can easily leverage TailwindCSS, Radix UI, and shadcn/ui to build a beautiful, accessible, and highly responsive dashboard without writing basic components from scratch.
* **Optimized Data Fetching**: React Server Components (RSC) and the App Router allow us to fetch data securely on the server and reduce the JavaScript bundle size shipped to the client.
* **Robust Tooling**: First-class support for TypeScript, extremely fast builds, and seamless deployment options (e.g., Vercel, Azure Static Web Apps).
* **State Management Ecosystem**: Easy integration with specialized tools like TanStack Query (React Query) for server state management and Zustand for lightweight client state.

### Negative Consequences

* **Learning Curve**: The App Router paradigm (specifically the mental model of Server Components vs. Client Components) introduces a significant learning curve compared to traditional "Pages Router" or standard Vite-based React apps.
* **Tech Stack Split**: Moving away from Blazor means the developer must maintain context and expertise in both C# (.NET) for the backend and TypeScript (React/Next.js) for the frontend.
* **Complexity**: Next.js introduces server-side complexity (SSR/SSG) that wouldn't exist in a pure Client-Side Rendered application, requiring careful thought about where code executes.
