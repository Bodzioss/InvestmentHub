import apiClient from './client'
import type { User } from '../types'

// ============================================
// USERS API FUNCTIONS
// ============================================

/**
 * Get all users (admin only)
 */
export async function getUsers(): Promise<User[]> {
    const response = await apiClient.get<User[]>('/api/users')
    return response.data
}

/**
 * Get user by ID (admin only)
 */
export async function getUser(userId: string): Promise<User> {
    const response = await apiClient.get<User>(`/api/users/${userId}`)
    return response.data
}
