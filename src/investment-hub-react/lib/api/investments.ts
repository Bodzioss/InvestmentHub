import apiClient from './client'
import type {
    Investment,
    AddInvestmentRequest,
    UpdateInvestmentRequest,
    SellInvestmentRequest
} from '../types'

// ============================================
// INVESTMENTS API FUNCTIONS
// ============================================

/**
 * Get all investments for a portfolio
 */
export async function getInvestments(portfolioId: string): Promise<Investment[]> {
    const response = await apiClient.get<Investment[]>(
        `/api/investments/portfolio/${portfolioId}`
    )
    return response.data
}

/**
 * Get single investment by ID
 */
export async function getInvestment(
    portfolioId: string,
    investmentId: string
): Promise<Investment> {
    const response = await apiClient.get<Investment>(
        `/api/portfolios/${portfolioId}/investments/${investmentId}`
    )
    return response.data
}

/**
 * Add new investment to portfolio
 */
export async function addInvestment(request: AddInvestmentRequest): Promise<void> {
    await apiClient.post('/api/investments', request)
}

/**
 * Update investment details
 */
export async function updateInvestment(
    portfolioId: string,
    investmentId: string,
    request: UpdateInvestmentRequest
): Promise<void> {
    await apiClient.put(
        `/api/portfolios/${portfolioId}/investments/${investmentId}`,
        request
    )
}

/**
 * Sell investment (partial or full)
 */
export async function sellInvestment(
    portfolioId: string,
    investmentId: string,
    request: SellInvestmentRequest
): Promise<void> {
    await apiClient.post(
        `/api/portfolios/${portfolioId}/investments/${investmentId}/sell`,
        request
    )
}

/**
 * Delete investment
 */
export async function deleteInvestment(
    portfolioId: string,
    investmentId: string
): Promise<void> {
    await apiClient.delete(
        `/api/portfolios/${portfolioId}/investments/${investmentId}`
    )
}
