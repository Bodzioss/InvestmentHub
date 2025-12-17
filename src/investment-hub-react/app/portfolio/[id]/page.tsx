'use client'

import { use } from 'react'
import { useRouter } from 'next/navigation'
import { ArrowLeft, RefreshCw } from 'lucide-react'
import { usePortfolio, useInvestments, useRefreshPortfolioPrices } from '@/lib/hooks'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { PortfolioSummary } from '@/components/portfolio/portfolio-summary'
import { AssetAllocationChart } from '@/components/portfolio/asset-allocation-chart'
import { PerformanceChart } from '@/components/portfolio/performance-chart'
import { AddInvestmentDialog } from '@/components/portfolio/add-investment-dialog'
import { InvestmentsList } from '@/components/portfolio/investments-list'

interface PortfolioDetailsPageProps {
    params: Promise<{
        id: string
    }>
}

export default function PortfolioDetailsPage({ params }: PortfolioDetailsPageProps) {
    const { id } = use(params) // Unwrap the promise
    const router = useRouter()
    const { data: portfolio, isLoading: portfolioLoading, error: portfolioError } = usePortfolio(id)
    const { data: investments, isLoading: investmentsLoading } = useInvestments(id)
    const refreshPrices = useRefreshPortfolioPrices()

    if (portfolioLoading) {
        return (
            <div className="min-h-screen bg-background flex items-center justify-center">
                <div className="text-center">
                    <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary mx-auto"></div>
                    <p className="mt-4 text-muted-foreground">Loading portfolio...</p>
                </div>
            </div>
        )
    }

    if (portfolioError || !portfolio) {
        return (
            <div className="min-h-screen bg-background flex items-center justify-center">
                <Card className="w-full max-w-md">
                    <CardHeader>
                        <CardTitle className="text-destructive">Error</CardTitle>
                        <CardDescription>Failed to load portfolio</CardDescription>
                    </CardHeader>
                    <CardContent>
                        <Button onClick={() => router.push('/')}>Back to Dashboard</Button>
                    </CardContent>
                </Card>
            </div>
        )
    }

    return (
        <div className="min-h-screen bg-background">
            {/* Header */}
            <header className="border-b bg-card">
                <div className="container mx-auto flex h-16 items-center justify-between px-4">
                    <div className="flex items-center gap-4">
                        <Button variant="ghost" size="icon" onClick={() => router.push('/')}>
                            <ArrowLeft className="h-5 w-5" />
                        </Button>
                        <div>
                            <h1 className="text-xl font-bold">{portfolio.name}</h1>
                            {portfolio.description && (
                                <p className="text-sm text-muted-foreground">{portfolio.description}</p>
                            )}
                        </div>
                    </div>
                    <div className="flex items-center gap-3">
                        <Button
                            variant="outline"
                            size="sm"
                            onClick={() => refreshPrices.mutate(id)}
                            disabled={refreshPrices.isPending}
                        >
                            <RefreshCw className={`mr-2 h-4 w-4 ${refreshPrices.isPending ? 'animate-spin' : ''}`} />
                            {refreshPrices.isPending ? 'Refreshing...' : 'Refresh Prices'}
                        </Button>
                        <AddInvestmentDialog portfolioId={id}>
                            <Button variant="outline">Add Investment</Button>
                        </AddInvestmentDialog>
                        <Button variant="outline">Edit Portfolio</Button>
                    </div>
                </div>
            </header>

            {/* Main Content */}
            <main className="container mx-auto p-6 space-y-6">
                {/* Summary Cards */}
                <PortfolioSummary portfolio={portfolio} />

                {/* Charts Section */}
                <div className="grid gap-6 lg:grid-cols-2">
                    {/* Asset Allocation Chart */}
                    <AssetAllocationChart investments={investments || []} />

                    {/* Performance Chart */}
                    <PerformanceChart portfolio={portfolio} />
                </div>

                {/* Investments List */}
                <InvestmentsList investments={investments || []} isLoading={investmentsLoading} />
            </main>
        </div>
    )
}
