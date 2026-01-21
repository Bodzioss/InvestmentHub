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
    const response = await apiClient.get<any>('/api/instruments', {
        params: {
            search: params.query || undefined,
            type: params.assetType || undefined,
            exchange: params.exchange || undefined,
        },
    })

    // Backend returns paginated response { instruments: [...], totalCount: ... }
    return response.data.instruments || response.data.Instruments || []
}
