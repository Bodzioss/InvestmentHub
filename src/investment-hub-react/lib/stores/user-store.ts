import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import { STORAGE_KEYS } from '../constants'

// ============================================
// USER STORE STATE (dla admina)
// ============================================

interface UserState {
    // State
    selectedUserId: string | null

    // Actions
    setSelectedUser: (userId: string | null) => void
    clearSelectedUser: () => void
}

// ============================================
// ZUSTAND STORE
// ============================================

/**
 * Store dla admina do wybierania użytkownika
 * W Blazorze był to UserState (Fluxor)
 */
export const useUserStore = create<UserState>()(
    persist(
        (set) => ({
            // Initial state
            selectedUserId: null,

            // Set selected user (admin wybiera użytkownika)
            setSelectedUser: (userId) => {
                set({ selectedUserId: userId })
            },

            // Clear selection (powrót do widoku wszystkich)
            clearSelectedUser: () => {
                set({ selectedUserId: null })
            },
        }),
        {
            name: STORAGE_KEYS.SELECTED_USER, // klucz w localStorage
        }
    )
)
