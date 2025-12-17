import { useQuery } from '@tanstack/react-query'
import { searchInstruments, type SearchInstrumentsParams } from '../api/instruments'
import { getMarketPrice } from '../api/market-data'
import { QUERY_KEYS } from '../constants'

// ============================================
// INSTRUMENTS HOOKS
// ============================================

/**
 * Hook for searching instruments with debouncing
 * Automatically disabled for queries less than 2 characters
 */
export function useInstrumentSearch(
    query: string,
    options?: Omit<SearchInstrumentsParams, 'query'>
) {
    return useQuery({
        queryKey: [QUERY_KEYS.INSTRUMENTS, 'search', query, options],
        queryFn: () =>
            searchInstruments({
                query,
                ...options,
            }),
        enabled: query.length >= 2, // Only search if query is at least 2 chars
        staleTime: 5 * 60 * 1000, // Cache for 5 minutes
    })
}

/**
 * Hook for getting market price for a symbol
 */
export function useMarketPrice(symbol: string | undefined, enabled: boolean = true) {
    return useQuery({
        queryKey: [QUERY_KEYS.MARKET_PRICE, symbol],
        queryFn: () => getMarketPrice(symbol!),
        enabled: enabled && !!symbol, // Only fetch if symbol exists and enabled is true
        staleTime: 60 * 1000, // Cache for 1 minute
        retry: 1, // Only retry once on failure
    })
}
