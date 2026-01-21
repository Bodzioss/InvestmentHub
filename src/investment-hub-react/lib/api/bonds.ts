import apiClient from './client'

// ============================================
// BONDS API TYPES
// ============================================

export interface BondTypeInfo {
    code: string
    name: string
    durationYears: number
    margin: number
    earlyRedemptionFee: number
    isInflationIndexed: boolean
}

export interface BondSeries {
    instrumentId: string
    symbol: string
    name: string
    type: string
    issueDate: string
    maturityDate: string
    firstYearRate: number
    margin: number
    earlyRedemptionFee: number
}

export interface BondDetails extends BondSeries {
    nominalValue: number
    isInflationIndexed: boolean
    yearsToMaturity: number
    interestPeriods: InterestPeriod[]
}

export interface InterestPeriod {
    periodNumber: number
    startDate: string
    endDate: string
    interestRate: number
    accruedInterest: number
}

export interface BondValueResult {
    quantity: number
    nominalValuePerBond: number
    totalNominalValue: number
    accruedInterestPerBond: number
    totalAccruedInterest: number
    grossValuePerBond: number
    totalGrossValue: number
    taxPerBond: number
    totalTax: number
    netValuePerBond: number
    totalNetValue: number
    currentPeriodNumber?: number
    currentInterestRate?: number
    daysInCurrentPeriod: number
    maturityDate: string
    daysToMaturity: number
    isEarlyRedemption: boolean
    earlyRedemptionFeePerBond: number
    totalEarlyRedemptionFee: number
    netValueAfterEarlyRedemption: number
    isProjection: boolean
    assumedInflationRate: number
}

export interface CalculateBondValueRequest {
    symbol: string
    quantity: number
    asOfDate?: string
    calculationType: 'current' | 'early_redemption' | 'maturity'
    assumedInflation?: number
}

export interface SyncBondsResult {
    fetchedCount: number
    addedCount: number
    message: string
}

// ============================================
// BONDS API FUNCTIONS
// ============================================

/**
 * Get all available bond types
 */
export async function getBondTypes(): Promise<BondTypeInfo[]> {
    const response = await apiClient.get<BondTypeInfo[]>('/api/bonds/types')
    return response.data
}

/**
 * Get all available bond series
 */
export async function getBondSeries(): Promise<BondSeries[]> {
    const response = await apiClient.get<BondSeries[]>('/api/bonds/series')
    return response.data
}

/**
 * Get details for a specific bond series
 */
export async function getBondDetails(symbol: string): Promise<BondDetails> {
    const response = await apiClient.get<BondDetails>(`/api/bonds/${symbol}`)
    return response.data
}

/**
 * Calculate bond value
 */
export async function calculateBondValue(
    request: CalculateBondValueRequest
): Promise<BondValueResult> {
    const response = await apiClient.post<BondValueResult>(
        '/api/bonds/calculate',
        request
    )
    return response.data
}

/**
 * Sync bonds from obligacjeskarbowe.pl
 */
export async function syncBonds(): Promise<SyncBondsResult> {
    const response = await apiClient.post<SyncBondsResult>('/api/bonds/sync')
    return response.data
}
