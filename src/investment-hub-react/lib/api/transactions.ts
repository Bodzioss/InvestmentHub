import apiClient from './client'
import type {
    Transaction,
    TransactionCreatedResponse,
    TransactionsListResponse,
    RecordBuyRequest,
    RecordSellRequest,
    RecordDividendRequest,
    RecordInterestRequest,
    UpdateTransactionRequest
} from '../types'

// ============================================
// TRANSACTIONS API FUNCTIONS
// ============================================

/**
 * Get all transactions for a portfolio
 */
export async function getTransactions(portfolioId: string): Promise<TransactionsListResponse> {
    const response = await apiClient.get<TransactionsListResponse>(
        `/api/portfolios/${portfolioId}/transactions`
    )
    return response.data
}

/**
 * Get a single transaction by ID
 */
export async function getTransaction(
    portfolioId: string,
    transactionId: string
): Promise<Transaction> {
    const response = await apiClient.get<Transaction>(
        `/api/portfolios/${portfolioId}/transactions/${transactionId}`
    )
    return response.data
}

/**
 * Record a BUY transaction
 */
export async function recordBuy(
    portfolioId: string,
    request: RecordBuyRequest
): Promise<TransactionCreatedResponse> {
    const response = await apiClient.post<TransactionCreatedResponse>(
        `/api/portfolios/${portfolioId}/transactions/buy`,
        request
    )
    return response.data
}

/**
 * Record a SELL transaction
 */
export async function recordSell(
    portfolioId: string,
    request: RecordSellRequest
): Promise<TransactionCreatedResponse> {
    const response = await apiClient.post<TransactionCreatedResponse>(
        `/api/portfolios/${portfolioId}/transactions/sell`,
        request
    )
    return response.data
}

/**
 * Record a DIVIDEND payment
 */
export async function recordDividend(
    portfolioId: string,
    request: RecordDividendRequest
): Promise<TransactionCreatedResponse> {
    const response = await apiClient.post<TransactionCreatedResponse>(
        `/api/portfolios/${portfolioId}/transactions/dividend`,
        request
    )
    return response.data
}

/**
 * Record an INTEREST payment
 */
export async function recordInterest(
    portfolioId: string,
    request: RecordInterestRequest
): Promise<TransactionCreatedResponse> {
    const response = await apiClient.post<TransactionCreatedResponse>(
        `/api/portfolios/${portfolioId}/transactions/interest`,
        request
    )
    return response.data
}

/**
 * Update a transaction
 */
export async function updateTransaction(
    portfolioId: string,
    transactionId: string,
    request: UpdateTransactionRequest
): Promise<void> {
    await apiClient.put(
        `/api/portfolios/${portfolioId}/transactions/${transactionId}`,
        request
    )
}

/**
 * Cancel a transaction
 */
export async function cancelTransaction(
    portfolioId: string,
    transactionId: string
): Promise<void> {
    await apiClient.delete(
        `/api/portfolios/${portfolioId}/transactions/${transactionId}`
    )
}
