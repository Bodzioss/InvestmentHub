import apiClient from './client'
import type {
    MarketPrice,
    PriceHistory
} from '../types'

// ============================================
// MARKET DATA API FUNCTIONS
// ============================================


/**
 * Get current market price for symbol
 */
export async function getMarketPrice(symbol: string): Promise<MarketPrice> {
    const response = await apiClient.get<MarketPrice>(
        `/api/market/price/${symbol}`
    )
    return response.data
}

/**
 * Get price history for symbol
 */
export async function getPriceHistory(
    symbol: string,
    days: number = 30
): Promise<PriceHistory> {
    const response = await apiClient.get<PriceHistory>(
        `/api/market-data/history/${symbol}`,
        {
            params: { days }
        }
    )
    return response.data
}
