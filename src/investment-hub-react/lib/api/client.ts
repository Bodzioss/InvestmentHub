import axios, { AxiosError, AxiosInstance, InternalAxiosRequestConfig } from 'axios'
import { API_BASE_URL, STORAGE_KEYS } from '../constants'
import type { ApiError } from '../types'

// ============================================
// AXIOS INSTANCE
// ============================================

const apiClient: AxiosInstance = axios.create({
    baseURL: API_BASE_URL,
    headers: {
        'Content-Type': 'application/json',
    },
    timeout: 30000, // 30 sekund
})

// ============================================
// REQUEST INTERCEPTOR
// Dodaje JWT token do każdego requesta
// ============================================

apiClient.interceptors.request.use(
    (config: InternalAxiosRequestConfig) => {
        // Pobierz token z localStorage
        const token = localStorage.getItem(STORAGE_KEYS.AUTH_TOKEN)

        if (token && config.headers) {
            config.headers.Authorization = `Bearer ${token}`
        }

        return config
    },
    (error) => {
        return Promise.reject(error)
    }
)

// ============================================
// RESPONSE INTERCEPTOR
// Obsługuje błędy (401, 403, etc.)
// ============================================

apiClient.interceptors.response.use(
    (response) => {
        // Jeśli wszystko OK, zwróć response
        return response
    },
    (error: AxiosError<ApiError>) => {
        // Obsługa różnych kodów błędów
        if (error.response) {
            const status = error.response.status

            switch (status) {
                case 401: // Unauthorized
                    // Usuń token i przekieruj do logowania
                    localStorage.removeItem(STORAGE_KEYS.AUTH_TOKEN)

                    // Jeśli nie jesteśmy już na stronie logowania, przekieruj
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

                case 500: // Internal Server Error
                    console.error('Server error')
                    break
            }
        }

        return Promise.reject(error)
    }
)

// ============================================
// HELPER FUNCTIONS
// ============================================

/**
 * Wyciąga error message z odpowiedzi API
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

export default apiClient
