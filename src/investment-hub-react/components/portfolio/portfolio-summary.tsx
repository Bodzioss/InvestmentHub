import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import type { Portfolio } from '@/lib/types'
import { ArrowUpIcon, ArrowDownIcon } from 'lucide-react'

interface PortfolioSummaryProps {
    portfolio: Portfolio
}

export function PortfolioSummary({ portfolio }: PortfolioSummaryProps) {
    // Calculate return percentage
    const totalValue = portfolio.totalValue?.amount || 0
    const totalCost = portfolio.totalCost?.amount || 0
    const gainLoss = portfolio.unrealizedGainLoss?.amount || 0
    const returnPercentage = totalCost > 0 ? ((totalValue - totalCost) / totalCost) * 100 : 0
    const isPositive = gainLoss >= 0

    return (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
            {/* Total Value */}
            <Card>
                <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                    <CardTitle className="text-sm font-medium">Total Value</CardTitle>
                    <svg
                        xmlns="http://www.w3.org/2000/svg"
                        viewBox="0 0 24 24"
                        fill="none"
                        stroke="currentColor"
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth="2"
                        className="h-4 w-4 text-muted-foreground"
                    >
                        <path d="M12 2v20M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6" />
                    </svg>
                </CardHeader>
                <CardContent>
                    <div className="text-2xl font-bold">
                        {totalValue.toFixed(2)} {portfolio.totalValue?.currency}
                    </div>
                    <p className="text-xs text-muted-foreground">
                        Current market value
                    </p>
                </CardContent>
            </Card>

            {/* Total Cost */}
            <Card>
                <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                    <CardTitle className="text-sm font-medium">Total Cost</CardTitle>
                    <svg
                        xmlns="http://www.w3.org/2000/svg"
                        viewBox="0 0 24 24"
                        fill="none"
                        stroke="currentColor"
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth="2"
                        className="h-4 w-4 text-muted-foreground"
                    >
                        <rect width="20" height="14" x="2" y="5" rx="2" />
                        <path d="M2 10h20" />
                    </svg>
                </CardHeader>
                <CardContent>
                    <div className="text-2xl font-bold">
                        {totalCost.toFixed(2)} {portfolio.totalCost?.currency}
                    </div>
                    <p className="text-xs text-muted-foreground">
                        Total purchase cost
                    </p>
                </CardContent>
            </Card>

            {/* Gain/Loss */}
            <Card>
                <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                    <CardTitle className="text-sm font-medium">Gain/Loss</CardTitle>
                    {isPositive ? (
                        <ArrowUpIcon className="h-4 w-4 text-green-600" />
                    ) : (
                        <ArrowDownIcon className="h-4 w-4 text-red-600" />
                    )}
                </CardHeader>
                <CardContent>
                    <div className={`text-2xl font-bold ${isPositive ? 'text-green-600' : 'text-red-600'}`}>
                        {gainLoss >= 0 ? '+' : ''}{gainLoss.toFixed(2)} {portfolio.unrealizedGainLoss?.currency}
                    </div>
                    <p className="text-xs text-muted-foreground">
                        Unrealized {isPositive ? 'gain' : 'loss'}
                    </p>
                </CardContent>
            </Card>

            {/* Return % */}
            <Card>
                <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                    <CardTitle className="text-sm font-medium">Return</CardTitle>
                    <svg
                        xmlns="http://www.w3.org/2000/svg"
                        viewBox="0 0 24 24"
                        fill="none"
                        stroke="currentColor"
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth="2"
                        className="h-4 w-4 text-muted-foreground"
                    >
                        <polyline points="22 12 18 12 15 21 9 3 6 12 2 12" />
                    </svg>
                </CardHeader>
                <CardContent>
                    <div className={`text-2xl font-bold ${isPositive ? 'text-green-600' : 'text-red-600'}`}>
                        {returnPercentage >= 0 ? '+' : ''}{returnPercentage.toFixed(2)}%
                    </div>
                    <p className="text-xs text-muted-foreground">
                        Total return
                    </p>
                </CardContent>
            </Card>
        </div>
    )
}
