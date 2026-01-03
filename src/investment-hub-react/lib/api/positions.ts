import apiClient from './client'
import type {
    Position,
    PositionsListResponse
} from '../types'

// ============================================
// POSITIONS API FUNCTIONS
// ============================================

/**
 * Get all positions for a portfolio (FIFO-calculated)
 */
export async function getPositions(portfolioId: string): Promise<PositionsListResponse> {
    const response = await apiClient.get<PositionsListResponse>(
        `/api/portfolios/${portfolioId}/positions`
    )
    return response.data
}

/**
 * Get a specific position by symbol ticker
 */
export async function getPositionBySymbol(
    portfolioId: string,
    ticker: string
): Promise<Position> {
    const response = await apiClient.get<Position>(
        `/api/portfolios/${portfolioId}/positions/${ticker}`
    )
    return response.data
}
