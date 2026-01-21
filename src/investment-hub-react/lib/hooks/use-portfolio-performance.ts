import { useQuery } from '@tanstack/react-query'
import { getPortfolioPerformance } from '../api/portfolios'
import { QUERY_KEYS } from '../constants'

/**
 * Hook for fetching portfolio performance history
 */
export function usePortfolioPerformance(portfolioId: string) {
    return useQuery({
        queryKey: [QUERY_KEYS.PORTFOLIO_PERFORMANCE, portfolioId],
        queryFn: () => getPortfolioPerformance(portfolioId),
        enabled: !!portfolioId,
        staleTime: 5 * 60 * 1000, // 5 minutes - performance data doesn't change that often
    })
}
