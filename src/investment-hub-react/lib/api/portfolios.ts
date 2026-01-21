import apiClient from './client'
import type { Portfolio, CreatePortfolioRequest, UpdatePortfolioRequest } from '../types'

// ============================================
// PORTFOLIO CRUD OPERATIONS  
// ============================================

export async function getPortfolios(userId: string): Promise<Portfolio[]> {
    const response = await apiClient.get<Portfolio[]>(`/api/portfolios/user/${userId}`)
    return response.data
}

export async function getPortfolio(portfolioId: string): Promise<Portfolio> {
    const response = await apiClient.get<Portfolio>(`/api/portfolios/${portfolioId}`)
    return response.data
}

export async function createPortfolio(request: CreatePortfolioRequest): Promise<Portfolio> {
    const response = await apiClient.post<Portfolio>('/api/portfolios', request)
    return response.data
}

export async function updatePortfolio(portfolioId: string, request: UpdatePortfolioRequest): Promise<Portfolio> {
    const response = await apiClient.put<Portfolio>(`/api/portfolios/${portfolioId}`, request)
    return response.data
}

export async function deletePortfolio(portfolioId: string): Promise<void> {
    await apiClient.delete(`/api/portfolios/${portfolioId}`)
}

// ============================================
// PORTFOLIO PERFORMANCE
// ============================================

export interface PerformanceDataPoint {
    date: string
    value: number
    totalCost: number
}

export interface PortfolioPerformanceResponse {
    dataPoints: PerformanceDataPoint[]
    investmentValues: Record<string, PerformanceDataPoint[]>
    startDate: string
    endDate: string
    currency: string
}

export async function getPortfolioPerformance(portfolioId: string): Promise<PortfolioPerformanceResponse> {
    const response = await apiClient.get<PortfolioPerformanceResponse>(`/api/portfolios/${portfolioId}/performance`)
    return response.data
}
