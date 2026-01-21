import apiClient from './client'
import type {
    LoginRequest,
    LoginResponse,
    RegisterRequest,
    ChangePasswordRequest,
    User
} from '../types'

// ============================================
// AUTH API FUNCTIONS
// ============================================

/**
 * Login user and get JWT token
 */
export async function login(request: LoginRequest): Promise<LoginResponse> {
    const response = await apiClient.post<LoginResponse>('/api/auth/login', request)
    return response.data
}

/**
 * Register new user
 */
export async function register(request: RegisterRequest): Promise<void> {
    await apiClient.post('/api/auth/register', request)
}

/**
 * Change password for current user
 */
export async function changePassword(request: ChangePasswordRequest): Promise<void> {
    await apiClient.post('/api/auth/change-password', request)
}

/**
 * Get current authenticated user info
 */
export async function getCurrentUser(): Promise<User> {
    const response = await apiClient.get<User>('/api/auth/me')
    return response.data
}

/**
 * Logout (client-side only - remove token)
 */
export function logout(): void {
    // Token removal is handled in auth store
    // This is just a helper if needed
}
