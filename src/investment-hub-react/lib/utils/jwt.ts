// ============================================
// JWT DECODE UTILITY
// ============================================

interface JwtPayload {
    sub: string // User ID
    email: string
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name': string
    exp: number
    iss: string
    aud: string
}

/**
 * Decode JWT token to extract user information
 * @param token JWT token string
 * @returns Decoded payload
 */
export function decodeJwt(token: string): JwtPayload {
    try {
        const base64Url = token.split('.')[1]
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/')
        const jsonPayload = decodeURIComponent(
            atob(base64)
                .split('')
                .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
                .join('')
        )
        return JSON.parse(jsonPayload)
    } catch (error) {
        console.error('Failed to decode JWT:', error)
        throw new Error('Invalid JWT token')
    }
}

/**
 * Extract user info from JWT token
 */
export function getUserFromToken(token: string) {
    const payload = decodeJwt(token)

    return {
        id: payload.sub,
        email: payload.email,
        name: payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] || payload.email,
        role: 'User', // Default role - backend doesn't provide this in token
    }
}
