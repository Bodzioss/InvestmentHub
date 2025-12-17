import apiClient from './client'
import type { Instrument } from '../types'

// ============================================
// INSTRUMENTS API FUNCTIONS
// ============================================

export interface SearchInstrumentsParams {
    query?: string
    assetType?: string
    exchange?: string
}

/**
 * Search instruments by ticker, name, or ISIN
 */
export async function searchInstruments(params: SearchInstrumentsParams): Promise<Instrument[]> {
    const response = await apiClient.get<Instrument[]>('/api/instruments', {
        params: {
            query: params.query || undefined,
            assetType: params.assetType || undefined,
            exchange: params.exchange || undefined,
        },
    })
    return response.data
}
