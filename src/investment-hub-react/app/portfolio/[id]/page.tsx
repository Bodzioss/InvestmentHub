'use client'

import { use, useState } from 'react'
import { useRouter } from 'next/navigation'
import { ArrowLeft, RefreshCw, Plus, Receipt, BarChart3, Wallet } from 'lucide-react'
import { usePortfolio, useRefreshPortfolioPrices } from '@/lib/hooks'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { PortfolioSummary } from '@/components/portfolio/portfolio-summary'
import { PerformanceChart } from '@/components/portfolio/performance-chart'
import { RecordTransactionDialog, TransactionsList, PositionsList, IncomeReport } from '@/components/transaction'

interface PortfolioDetailsPageProps {
    params: Promise<{
        id: string
    }>
}

export default function PortfolioDetailsPage({ params }: PortfolioDetailsPageProps) {
    const { id } = use(params)
    const router = useRouter()
    const [activeTab, setActiveTab] = useState('positions')
    const { data: portfolio, isLoading: portfolioLoading, error: portfolioError } = usePortfolio(id)
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
                        <RecordTransactionDialog portfolioId={id}>
                            <Button>
                                <Plus className="mr-2 h-4 w-4" />
                                Record Transaction
                            </Button>
                        </RecordTransactionDialog>
                    </div>
                </div>
            </header>

            {/* Main Content */}
            <main className="container mx-auto p-6 space-y-6">
                {/* Summary Cards */}
                <PortfolioSummary portfolio={portfolio} />

                {/* Charts Section */}
                <PerformanceChart portfolio={portfolio} />

                {/* Tabbed Content: Positions, Transactions, Income */}
                <Tabs value={activeTab} onValueChange={setActiveTab} className="space-y-4">
                    <TabsList className="grid w-full grid-cols-3 lg:w-[450px]">
                        <TabsTrigger value="positions" className="flex items-center gap-2">
                            <BarChart3 className="h-4 w-4" />
                            Positions
                        </TabsTrigger>
                        <TabsTrigger value="transactions" className="flex items-center gap-2">
                            <Receipt className="h-4 w-4" />
                            Transactions
                        </TabsTrigger>
                        <TabsTrigger value="income" className="flex items-center gap-2">
                            <Wallet className="h-4 w-4" />
                            Income
                        </TabsTrigger>
                    </TabsList>

                    <TabsContent value="positions">
                        <PositionsList portfolioId={id} />
                    </TabsContent>

                    <TabsContent value="transactions">
                        <TransactionsList portfolioId={id} />
                    </TabsContent>

                    <TabsContent value="income">
                        <IncomeReport portfolioId={id} />
                    </TabsContent>
                </Tabs>
            </main>
        </div>
    )
}

