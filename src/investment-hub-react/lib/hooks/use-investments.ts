import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import {
    getInvestments,
    getInvestment,
    addInvestment,
    updateInvestment,
    sellInvestment,
    deleteInvestment,
    getErrorMessage
} from '@/lib/api'
import { QUERY_KEYS } from '@/lib/constants'
import type { AddInvestmentRequest, UpdateInvestmentRequest, SellInvestmentRequest } from '@/lib/types'

// ============================================
// useInvestments - Get all investments for portfolio
// ============================================

export function useInvestments(portfolioId: string) {
    return useQuery({
        queryKey: [QUERY_KEYS.INVESTMENTS, portfolioId],
        queryFn: () => getInvestments(portfolioId),
        enabled: !!portfolioId,
    })
}

// ============================================
// useInvestment - Get single investment
// ============================================

export function useInvestment(portfolioId: string, investmentId: string) {
    return useQuery({
        queryKey: [QUERY_KEYS.INVESTMENTS, investmentId],
        queryFn: () => getInvestment(portfolioId, investmentId),
        enabled: !!portfolioId && !!investmentId,
    })
}

// ============================================
// useAddInvestment - Add new investment
// ============================================

export function useAddInvestment() {
    const queryClient = useQueryClient()

    return useMutation({
        mutationFn: (request: AddInvestmentRequest) => addInvestment(request),
        onSuccess: (_, variables) => {
            // Invalidate both investments and portfolio queries
            queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.INVESTMENTS, variables.portfolioId] })
            queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.PORTFOLIOS, variables.portfolioId] })
            toast.success('Investment added successfully!')
        },
        onError: (error) => {
            toast.error(getErrorMessage(error))
        }
    })
}

// ============================================
// useUpdateInvestment - Update investment
// ============================================

export function useUpdateInvestment() {
    const queryClient = useQueryClient()

    return useMutation({
        mutationFn: ({ portfolioId, investmentId, request }: {
            portfolioId: string
            investmentId: string
            request: UpdateInvestmentRequest
        }) => updateInvestment(portfolioId, investmentId, request),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.INVESTMENTS] })
            queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.PORTFOLIOS] })
            toast.success('Investment updated successfully!')
        },
        onError: (error) => {
            toast.error(getErrorMessage(error))
        }
    })
}

// ============================================
// useSellInvestment - Sell investment
// ============================================

export function useSellInvestment() {
    const queryClient = useQueryClient()

    return useMutation({
        mutationFn: ({ investmentId, request }: {
            investmentId: string
            request: SellInvestmentRequest
        }) => sellInvestment(investmentId, request),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.INVESTMENTS] })
            queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.PORTFOLIOS] })
            toast.success('Investment sold successfully!')
        },
        onError: (error) => {
            toast.error(getErrorMessage(error))
        }
    })
}

// ============================================
// useDeleteInvestment - Delete investment
// ============================================

export function useDeleteInvestment() {
    const queryClient = useQueryClient()

    return useMutation({
        mutationFn: ({ portfolioId, investmentId }: { portfolioId: string, investmentId: string }) =>
            deleteInvestment(portfolioId, investmentId),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.INVESTMENTS] })
            queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.PORTFOLIOS] })
            toast.success('Investment deleted successfully!')
        },
        onError: (error) => {
            toast.error(getErrorMessage(error))
        }
    })
}
