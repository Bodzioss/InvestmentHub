import apiClient from './client'
import type { IncomeSummaryResponse } from '../types'

// ============================================
// INCOME API FUNCTIONS
// ============================================

/**
 * Get income summary for a portfolio (dividends + interest)
 */
export async function getIncomeSummary(
    portfolioId: string,
    year?: number,
    month?: number
): Promise<IncomeSummaryResponse> {
    const params = new URLSearchParams()
    if (year !== undefined) params.append('year', year.toString())
    if (month !== undefined) params.append('month', month.toString())

    const query = params.toString() ? `?${params.toString()}` : ''
    const response = await apiClient.get<IncomeSummaryResponse>(
        `/api/portfolios/${portfolioId}/income${query}`
    )
    return response.data
}

/**
 * Get dividends summary for a portfolio
 */
export async function getDividendsSummary(
    portfolioId: string,
    year?: number
): Promise<IncomeSummaryResponse> {
    const query = year !== undefined ? `?year=${year}` : ''
    const response = await apiClient.get<IncomeSummaryResponse>(
        `/api/portfolios/${portfolioId}/income/dividends${query}`
    )
    return response.data
}

/**
 * Get interest summary for a portfolio
 */
export async function getInterestSummary(
    portfolioId: string,
    year?: number
): Promise<IncomeSummaryResponse> {
    const query = year !== undefined ? `?year=${year}` : ''
    const response = await apiClient.get<IncomeSummaryResponse>(
        `/api/portfolios/${portfolioId}/income/interest${query}`
    )
    return response.data
}
