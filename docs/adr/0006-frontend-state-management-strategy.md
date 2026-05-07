# 0006. Frontend State Management Strategy

* Status: accepted
* Date: 2026-05-07

## Context and Problem Statement

With the adoption of Next.js and React for the InvestmentHub frontend, we need a clear and scalable strategy for managing state. Modern React applications deal with two fundamentally different types of state:
1. **Server State**: Data fetched from the API (e.g., portfolio details, market quotes). It is asynchronous, shared across multiple users, and quickly becomes stale.
2. **Client State**: UI-specific state (e.g., "is the 'Add Asset' modal open?", "which tab is currently selected?", dark/light mode). It is synchronous and local to the user's current session.

Mixing these two types of state into a single, monolithic global store (like traditional Redux) often leads to excessive boilerplate, complex caching logic, and poor performance. We need a modern, decoupled approach.

## Decision Drivers

* **Separation of Concerns**: Strict separation between asynchronous Server State and synchronous Client State.
* **Caching & Performance**: Efficient caching, background refetching, and deduplication of API requests for server data to ensure a snappy user experience.
* **Developer Experience (DX)**: Minimal boilerplate for managing global UI state.
* **Next.js Compatibility**: The chosen tools must work seamlessly with the Next.js App Router and the React 19 ecosystem.

## Considered Options

* **Redux Toolkit**: A mature, centralized global store. 
  * *Pros:* Industry standard, excellent dev tools. 
  * *Cons:* High boilerplate. Often blurs the lines between client and server state if not meticulously managed (though RTK Query helps, it adds its own complexity).
* **React Context API (Exclusively)**: Using built-in React features for all global state. 
  * *Pros:* No external dependencies. 
  * *Cons:* Severe performance issues (unnecessary re-renders) when used for frequently changing state; lacks any built-in caching or refetching mechanisms for server data.
* **TanStack Query (Server State) + Zustand (Client State)**: A decentralized approach using specialized tools for specific problems.

## Decision Outcome

Chosen option: **TanStack Query (React Query) combined with Zustand**, because this combination provides the most specialized, performant, and low-boilerplate solution for modern React applications.

### Positive Consequences

* **Specialized Data Fetching**: TanStack Query is arguably the best tool for managing Server State. It handles caching, request deduplication, background updates, loading/error states, and pagination out-of-the-box.
* **Minimal Boilerplate for UI State**: Zustand provides a remarkably simple, hook-based API for global Client State without the heavy ceremony of Redux actions, dispatchers, and reducers.
* **Optimized Rendering**: Zustand avoids the re-render issues inherent in standard React Context by allowing components to subscribe only to the specific slices of state they need.
* **Clear Mental Model**: It forces developers to explicitly ask: "Is this data owned by the backend? (If yes -> use TanStack Query)" or "Is this just temporary UI state? (If yes -> use Zustand or local `useState`)", leading to much cleaner component architecture.

### Negative Consequences

* **Two Libraries to Learn**: Developers need to understand the APIs and paradigms of two different libraries instead of a single unified framework like Redux.
* **State Decentralization**: State is no longer in one giant tree. Some state lives in React Query's cache, some in Zustand stores, and some locally in components (via `useState`). This requires discipline and clear naming conventions to know where to look when debugging.
