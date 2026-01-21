'use client'

import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { ReactQueryDevtools } from '@tanstack/react-query-devtools'
import { ThemeProvider } from 'next-themes'
import { Toaster } from '@/components/ui/sonner'
import { useState } from 'react'
import { QUERY_CONFIG } from '@/lib/constants'

// ============================================
// REACT QUERY CLIENT
// ============================================

function makeQueryClient() {
    return new QueryClient({
        defaultOptions: {
            queries: {
                staleTime: QUERY_CONFIG.STALE_TIME,
                retry: QUERY_CONFIG.RETRY,
                refetchOnWindowFocus: QUERY_CONFIG.REFETCH_ON_WINDOW_FOCUS,
            },
        },
    })
}

let browserQueryClient: QueryClient | undefined = undefined

function getQueryClient() {
    if (typeof window === 'undefined') {
        // Server: always make a new query client
        return makeQueryClient()
    } else {
        // Browser: make a new query client if we don't already have one
        if (!browserQueryClient) browserQueryClient = makeQueryClient()
        return browserQueryClient
    }
}

// ============================================
// PROVIDERS COMPONENT
// ============================================

export function Providers({ children }: { children: React.ReactNode }) {
    const [queryClient] = useState(getQueryClient)

    return (
        <QueryClientProvider client={queryClient}>
            <ThemeProvider
                attribute="class"
                defaultTheme="system"
                enableSystem
                disableTransitionOnChange
            >
                {children}
                <Toaster />
            </ThemeProvider>

            {/* React Query DevTools - tylko w development */}
            <ReactQueryDevtools initialIsOpen={false} />
        </QueryClientProvider>
    )
}
