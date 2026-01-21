import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import {
    getPortfolios,
    getPortfolio,
    createPortfolio,
    updatePortfolio,
    deletePortfolio,
    getErrorMessage
} from '@/lib/api'
import { QUERY_KEYS } from '@/lib/constants'
import type { CreatePortfolioRequest, UpdatePortfolioRequest } from '@/lib/types'

// ============================================
// usePortfolios - Get all portfolios for user
// ============================================

export function usePortfolios(userId: string) {
    return useQuery({
        queryKey: [QUERY_KEYS.PORTFOLIOS, userId],
        queryFn: () => getPortfolios(userId),
        enabled: !!userId, // Only fetch if userId exists
    })
}

// ============================================
// usePortfolio - Get single portfolio
// ============================================

export function usePortfolio(portfolioId: string) {
    return useQuery({
        queryKey: [QUERY_KEYS.PORTFOLIOS, portfolioId],
        queryFn: () => getPortfolio(portfolioId),
        enabled: !!portfolioId,
    })
}

// ============================================
// useCreatePortfolio - Create new portfolio
// ============================================

export function useCreatePortfolio() {
    const queryClient = useQueryClient()

    return useMutation({
        mutationFn: (request: CreatePortfolioRequest) => createPortfolio(request),
        onSuccess: () => {
            // Invalidate portfolios list to refetch
            queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.PORTFOLIOS] })
            toast.success('Portfolio created successfully!')
        },
        onError: (error) => {
            toast.error(getErrorMessage(error))
        }
    })
}

// ============================================
// useUpdatePortfolio - Update portfolio
// ============================================

export function useUpdatePortfolio() {
    const queryClient = useQueryClient()

    return useMutation({
        mutationFn: ({ portfolioId, request }: {
            portfolioId: string
            request: UpdatePortfolioRequest
        }) => updatePortfolio(portfolioId, request),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.PORTFOLIOS] })
            toast.success('Portfolio updated successfully!')
        },
        onError: (error) => {
            toast.error(getErrorMessage(error))
        }
    })
}

// ============================================
// useDeletePortfolio - Delete portfolio
// ============================================

export function useDeletePortfolio() {
    const queryClient = useQueryClient()

    return useMutation({
        mutationFn: (portfolioId: string) => deletePortfolio(portfolioId),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.PORTFOLIOS] })
            toast.success('Portfolio deleted successfully!')
        },
        onError: (error) => {
            toast.error(getErrorMessage(error))
        }
    })
}
