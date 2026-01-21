import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import {
    getTransactions,
    getTransaction,
    recordBuy,
    recordSell,
    recordDividend,
    recordInterest,
    updateTransaction,
    cancelTransaction
} from '@/lib/api/transactions'
import { getErrorMessage } from '@/lib/api'
import { QUERY_KEYS } from '@/lib/constants'
import type {
    RecordBuyRequest,
    RecordSellRequest,
    RecordDividendRequest,
    RecordInterestRequest,
    UpdateTransactionRequest
} from '@/lib/types'

// ============================================
// useTransactions - Get all transactions for portfolio
// ============================================

export function useTransactions(portfolioId: string) {
    return useQuery({
        queryKey: [QUERY_KEYS.TRANSACTIONS, portfolioId],
        queryFn: () => getTransactions(portfolioId),
        enabled: !!portfolioId,
    })
}

// ============================================
// useTransaction - Get single transaction
// ============================================

export function useTransaction(portfolioId: string, transactionId: string) {
    return useQuery({
        queryKey: [QUERY_KEYS.TRANSACTIONS, portfolioId, transactionId],
        queryFn: () => getTransaction(portfolioId, transactionId),
        enabled: !!portfolioId && !!transactionId,
    })
}

// ============================================
// useRecordBuy - Record BUY transaction
// ============================================

export function useRecordBuy() {
    const queryClient = useQueryClient()

    return useMutation({
        mutationFn: ({ portfolioId, request }: {
            portfolioId: string
            request: RecordBuyRequest
        }) => recordBuy(portfolioId, request),
        onSuccess: (_, variables) => {
            queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.TRANSACTIONS, variables.portfolioId] })
            queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.POSITIONS, variables.portfolioId] })
            queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.PORTFOLIOS, variables.portfolioId] })
            toast.success('BUY transaction recorded successfully!')
        },
        onError: (error) => {
            toast.error(getErrorMessage(error))
        }
    })
}

// ============================================
// useRecordSell - Record SELL transaction
// ============================================

export function useRecordSell() {
    const queryClient = useQueryClient()

    return useMutation({
        mutationFn: ({ portfolioId, request }: {
            portfolioId: string
            request: RecordSellRequest
        }) => recordSell(portfolioId, request),
        onSuccess: (_, variables) => {
            queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.TRANSACTIONS, variables.portfolioId] })
            queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.POSITIONS, variables.portfolioId] })
            queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.PORTFOLIOS, variables.portfolioId] })
            toast.success('SELL transaction recorded successfully!')
        },
        onError: (error) => {
            toast.error(getErrorMessage(error))
        }
    })
}

// ============================================
// useRecordDividend - Record DIVIDEND
// ============================================

export function useRecordDividend() {
    const queryClient = useQueryClient()

    return useMutation({
        mutationFn: ({ portfolioId, request }: {
            portfolioId: string
            request: RecordDividendRequest
        }) => recordDividend(portfolioId, request),
        onSuccess: (data, variables) => {
            queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.TRANSACTIONS, variables.portfolioId] })
            queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.POSITIONS, variables.portfolioId] })
            queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.INCOME, variables.portfolioId] })
            const netAmount = data.netAmount?.toFixed(2) ?? ''
            toast.success(`Dividend recorded! Net amount: ${netAmount}`)
        },
        onError: (error) => {
            toast.error(getErrorMessage(error))
        }
    })
}

// ============================================
// useRecordInterest - Record INTEREST
// ============================================

export function useRecordInterest() {
    const queryClient = useQueryClient()

    return useMutation({
        mutationFn: ({ portfolioId, request }: {
            portfolioId: string
            request: RecordInterestRequest
        }) => recordInterest(portfolioId, request),
        onSuccess: (data, variables) => {
            queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.TRANSACTIONS, variables.portfolioId] })
            queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.POSITIONS, variables.portfolioId] })
            queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.INCOME, variables.portfolioId] })
            const netAmount = data.netAmount?.toFixed(2) ?? ''
            toast.success(`Interest recorded! Net amount: ${netAmount}`)
        },
        onError: (error) => {
            toast.error(getErrorMessage(error))
        }
    })
}

// ============================================
// useUpdateTransaction - Update transaction
// ============================================

export function useUpdateTransaction() {
    const queryClient = useQueryClient()

    return useMutation({
        mutationFn: ({ portfolioId, transactionId, request }: {
            portfolioId: string
            transactionId: string
            request: UpdateTransactionRequest
        }) => updateTransaction(portfolioId, transactionId, request),
        onSuccess: (_, variables) => {
            queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.TRANSACTIONS, variables.portfolioId] })
            queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.POSITIONS, variables.portfolioId] })
            toast.success('Transaction updated successfully!')
        },
        onError: (error) => {
            toast.error(getErrorMessage(error))
        }
    })
}

// ============================================
// useCancelTransaction - Cancel transaction
// ============================================

export function useCancelTransaction() {
    const queryClient = useQueryClient()

    return useMutation({
        mutationFn: ({ portfolioId, transactionId }: {
            portfolioId: string
            transactionId: string
        }) => cancelTransaction(portfolioId, transactionId),
        onSuccess: (_, variables) => {
            queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.TRANSACTIONS, variables.portfolioId] })
            queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.POSITIONS, variables.portfolioId] })
            queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.PORTFOLIOS, variables.portfolioId] })
            toast.success('Transaction cancelled successfully!')
        },
        onError: (error) => {
            toast.error(getErrorMessage(error))
        }
    })
}
