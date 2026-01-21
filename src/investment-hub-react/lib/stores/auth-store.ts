import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import type { User } from '../types'
import { STORAGE_KEYS } from '../constants'

// ============================================
// AUTH STORE STATE
// ============================================

interface AuthState {
    // State
    token: string | null
    user: User | null

    // Computed
    isAuthenticated: boolean

    // Actions
    setAuth: (token: string, user: User) => void
    clearAuth: () => void
    updateUser: (user: User) => void
}

// ============================================
// ZUSTAND STORE
// ============================================

export const useAuthStore = create<AuthState>()(
    persist(
        (set) => ({
            // Initial state
            token: null,
            user: null,
            isAuthenticated: false,

            // Set authentication (po logowaniu)
            setAuth: (token, user) => {
                localStorage.setItem(STORAGE_KEYS.AUTH_TOKEN, token)
                set({ token, user, isAuthenticated: true })
            },

            // Clear authentication (po wylogowaniu)
            clearAuth: () => {
                localStorage.removeItem(STORAGE_KEYS.AUTH_TOKEN)
                set({ token: null, user: null, isAuthenticated: false })
            },

            // Update user info (bez zmiany tokenu)
            updateUser: (user) => {
                set({ user })
            },
        }),
        {
            name: STORAGE_KEYS.AUTH_TOKEN, // klucz w localStorage
            // Persist EVERYTHING - token, user, isAuthenticated
        }
    )
)
