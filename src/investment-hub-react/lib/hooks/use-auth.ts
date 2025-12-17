import { useMutation } from '@tanstack/react-query'
import { useRouter } from 'next/navigation'
import { toast } from 'sonner'
import { login, register, getErrorMessage } from '@/lib/api'
import { useAuthStore } from '@/lib/stores'
import { STORAGE_KEYS } from '@/lib/constants'
import { getUserFromToken } from '@/lib/utils/jwt'
import type { LoginRequest, RegisterRequest } from '@/lib/types'

// ============================================
// useLogin - Login mutation
// ============================================

export function useLogin() {
    const router = useRouter()
    const { setAuth } = useAuthStore()

    return useMutation({
        mutationFn: (credentials: LoginRequest) => login(credentials),
        onSuccess: (data) => {
            // Backend returns: { email, token }
            // Decode JWT to get user data
            const user = getUserFromToken(data.token)

            // Zapisz token i user w Zustand store (+ localStorage)
            setAuth(data.token, user)

            // Toast sukcesu
            toast.success(`Welcome back, ${user.name}!`)

            // Przekieruj do dashboard
            router.push('/')
        },
        onError: (error) => {
            toast.error(getErrorMessage(error))
        }
    })
}

// ============================================
// useRegister - Register mutation
// ============================================

export function useRegister() {
    const router = useRouter()

    return useMutation({
        mutationFn: (credentials: RegisterRequest) => register(credentials),
        onSuccess: () => {
            toast.success('Account created! Please log in.')
            router.push('/login')
        },
        onError: (error) => {
            toast.error(getErrorMessage(error))
        }
    })
}

// ============================================
// useLogout - Logout action
// ============================================

export function useLogout() {
    const router = useRouter()
    const { clearAuth } = useAuthStore()

    return () => {
        clearAuth()
        toast.info('Logged out successfully')
        router.push('/login')
    }
}

// ============================================
// useCurrentUser - Get authenticated user
// ============================================

export function useCurrentUser() {
    return useAuthStore((state) => state.user)
}

// ============================================
// useIsAuthenticated - Check auth status
// ============================================

export function useIsAuthenticated() {
    return useAuthStore((state) => state.isAuthenticated)
}
