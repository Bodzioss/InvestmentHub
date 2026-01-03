import { useQuery } from '@tanstack/react-query'
import { getIncomeSummary, getDividendsSummary, getInterestSummary } from '@/lib/api/income'
import { QUERY_KEYS } from '@/lib/constants'

// ============================================
// useIncomeSummary - Get income summary for portfolio
// ============================================

export function useIncomeSummary(portfolioId: string, year?: number, month?: number) {
    return useQuery({
        queryKey: [QUERY_KEYS.INCOME, portfolioId, 'summary', year, month],
        queryFn: () => getIncomeSummary(portfolioId, year, month),
        enabled: !!portfolioId,
    })
}

// ============================================
// useDividends - Get dividends summary
// ============================================

export function useDividends(portfolioId: string, year?: number) {
    return useQuery({
        queryKey: [QUERY_KEYS.INCOME, portfolioId, 'dividends', year],
        queryFn: () => getDividendsSummary(portfolioId, year),
        enabled: !!portfolioId,
    })
}

// ============================================
// useInterest - Get interest summary
// ============================================

export function useInterest(portfolioId: string, year?: number) {
    return useQuery({
        queryKey: [QUERY_KEYS.INCOME, portfolioId, 'interest', year],
        queryFn: () => getInterestSummary(portfolioId, year),
        enabled: !!portfolioId,
    })
}
