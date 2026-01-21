import apiClient from './client'

// Types for CSV import
export interface ParsedTransactionDto {
    date: string
    operationType: string
    transactionType: string
    account: string
    ticker: string
    currency: string
    quantity: number
    pricePerUnit: number
    totalValue: number
    notes: string | null
    instrumentExists: boolean
    assetType: string | null
    selected: boolean
}

export interface ImportPreviewResponse {
    transactions: ParsedTransactionDto[]
    errors: string[]
    warnings: string[]
    totalRows: number
    skippedRows: number
    parsedTransactions: number
    isSuccess: boolean
}

export interface TransactionToImport {
    date: string
    transactionType: string
    ticker: string
    currency: string
    quantity: number
    pricePerUnit: number
    notes?: string
}

export interface ImportTransactionsRequest {
    portfolioId: string
    exchange: string
    transactions: TransactionToImport[]
}

export interface ImportTransactionsResponse {
    importedCount: number
    failedCount: number
    errors: string[]
    isSuccess: boolean
}

/**
 * Parse MyFund CSV file and return preview of transactions
 */
export async function previewMyFundCsv(file: File): Promise<ImportPreviewResponse> {
    const formData = new FormData()
    formData.append('file', file)

    const response = await apiClient.post<ImportPreviewResponse>('/api/import/myfund/preview', formData, {
        headers: {
            'Content-Type': 'multipart/form-data'
        }
    })
    return response.data
}

/**
 * Import confirmed transactions into a portfolio
 */
export async function importTransactions(request: ImportTransactionsRequest): Promise<ImportTransactionsResponse> {
    const response = await apiClient.post<ImportTransactionsResponse>('/api/import/myfund/import', request)
    return response.data
}
