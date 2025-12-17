import { useMutation, useQueryClient } from '@tanstack/react-query'
import apiClient from '../api/client'
import { QUERY_KEYS } from '../constants'
import { toast } from 'sonner'

// ============================================
// PORTFOLIO ACTIONS HOOKS
// ============================================

interface RefreshPricesResponse {
    message: string
    refreshedCount: number
    totalSymbols: number
}

/**
 * Hook for manually refreshing market prices for a portfolio's investments
 */
export function useRefreshPortfolioPrices() {
    const queryClient = useQueryClient()

    return useMutation({
        mutationFn: async (portfolioId: string): Promise<RefreshPricesResponse> => {
            const response = await apiClient.post<RefreshPricesResponse>(
                `/api/portfolios/${portfolioId}/refresh-prices`
            )
            return response.data
        },
        onSuccess: (data, portfolioId) => {
            // Invalidate investments query to trigger refetch with new prices
            queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.INVESTMENTS, portfolioId] })

            // Show success message
            toast.success(data.message || 'Prices refreshed successfully!', {
                description: `Updated ${data.refreshedCount} of ${data.totalSymbols} symbols`
            })
        },
        onError: (error) => {
            toast.error('Failed to refresh prices', {
                description: error instanceof Error ? error.message : 'Please try again later'
            })
        },
    })
}
