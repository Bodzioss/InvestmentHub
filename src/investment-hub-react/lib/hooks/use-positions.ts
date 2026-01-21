import { useQuery } from '@tanstack/react-query'
import { getPositions, getPositionBySymbol } from '@/lib/api/positions'
import { QUERY_KEYS } from '@/lib/constants'

// ============================================
// usePositions - Get all positions for portfolio
// ============================================

export function usePositions(portfolioId: string) {
    return useQuery({
        queryKey: [QUERY_KEYS.POSITIONS, portfolioId],
        queryFn: () => getPositions(portfolioId),
        enabled: !!portfolioId,
    })
}

// ============================================
// usePosition - Get position by symbol
// ============================================

export function usePosition(portfolioId: string, ticker: string) {
    return useQuery({
        queryKey: [QUERY_KEYS.POSITIONS, portfolioId, ticker],
        queryFn: () => getPositionBySymbol(portfolioId, ticker),
        enabled: !!portfolioId && !!ticker,
    })
}
