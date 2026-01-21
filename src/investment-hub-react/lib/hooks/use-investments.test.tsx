import { renderHook, waitFor } from '@testing-library/react'
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { useInvestments, useAddInvestment } from './use-investments'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import * as api from '@/lib/api'
import { QUERY_KEYS } from '@/lib/constants'
import { toast } from 'sonner'

// Mock API module
vi.mock('@/lib/api', () => ({
    getInvestments: vi.fn(),
    addInvestment: vi.fn(),
    getErrorMessage: vi.fn((err) => err.message || 'Error'),
}))

// Mock dependencies
vi.mock('sonner', () => ({
    toast: {
        success: vi.fn(),
        error: vi.fn()
    }
}))

describe('useInvestments Hooks', () => {
    let queryClient: QueryClient

    beforeEach(() => {
        vi.clearAllMocks()
        queryClient = new QueryClient({
            defaultOptions: {
                queries: {
                    retry: false, // Turn off retries for testing
                },
            },
        })
    })

    afterEach(() => {
        queryClient.clear()
    })

    const wrapper = ({ children }: { children: React.ReactNode }) => (
        <QueryClientProvider client={queryClient}>
            {children}
        </QueryClientProvider>
    )

    describe('useInvestments', () => {
        it('fetches investments successfully', async () => {
            const mockData = [{ id: '1', ticker: 'AAPL', quantity: 10 }]
            vi.mocked(api.getInvestments).mockResolvedValue(mockData)

            const { result } = renderHook(() => useInvestments('port-1'), { wrapper })

            // Initially pending
            expect(result.current.isLoading).toBe(true)

            // Wait for success
            await waitFor(() => expect(result.current.isSuccess).toBe(true))

            expect(result.current.data).toEqual(mockData)
            expect(api.getInvestments).toHaveBeenCalledWith('port-1')
        })

        it('does not fetch if portfolioId is missing', () => {
            const { result } = renderHook(() => useInvestments(''), { wrapper })

            expect(result.current.isLoading).toBe(false) // Not enabled, so status is pending but fetchStatus is idle
            expect(result.current.fetchStatus).toBe('idle')
            expect(api.getInvestments).not.toHaveBeenCalled()
        })
    })

    describe('useAddInvestment', () => {
        it('mutates and invalidates queries on success', async () => {
            vi.mocked(api.addInvestment).mockResolvedValue({})
            const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries')

            const { result } = renderHook(() => useAddInvestment(), { wrapper })

            const payload: any = { portfolioId: 'port-1', ticker: 'AAPL' }

            result.current.mutate(payload)

            await waitFor(() => expect(result.current.isSuccess).toBe(true))

            expect(api.addInvestment).toHaveBeenCalledWith(payload)
            expect(toast.success).toHaveBeenCalledWith('Investment added successfully!')

            // Check invalidations
            expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: [QUERY_KEYS.INVESTMENTS, 'port-1'] })
            expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: [QUERY_KEYS.PORTFOLIOS, 'port-1'] })
        })

        it('handles error correctly', async () => {
            const error = new Error('Failed to add')
            vi.mocked(api.addInvestment).mockRejectedValue(error)

            const { result } = renderHook(() => useAddInvestment(), { wrapper })

            result.current.mutate({ portfolioId: 'port-1' } as any)

            await waitFor(() => expect(result.current.isError).toBe(true))

            expect(toast.error).toHaveBeenCalledWith('Failed to add')
        })
    })
})
