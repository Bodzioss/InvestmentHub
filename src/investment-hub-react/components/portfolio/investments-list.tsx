'use client'

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { PriceCacheDialog } from './price-cache-dialog'
import type { Investment } from '@/lib/types'

interface InvestmentsListProps {
    investments: Investment[]
    isLoading: boolean
}

export function InvestmentsList({ investments, isLoading }: InvestmentsListProps) {
    if (isLoading) {
        return (
            <Card>
                <CardHeader>
                    <CardTitle>Investments</CardTitle>
                    <CardDescription>Loading your investments...</CardDescription>
                </CardHeader>
                <CardContent>
                    <div className="space-y-4">
                        {[...Array(3)].map((_, i) => (
                            <div key={i} className="flex items-center justify-between p-4 border rounded-lg animate-pulse">
                                <div className="space-y-2">
                                    <div className="h-4 w-24 bg-muted rounded" />
                                    <div className="h-3 w-32 bg-muted rounded" />
                                </div>
                                <div className="h-6 w-20 bg-muted rounded" />
                            </div>
                        ))}
                    </div>
                </CardContent>
            </Card>
        )
    }

    if (!investments || investments.length === 0) {
        return (
            <Card>
                <CardHeader>
                    <CardTitle>Investments</CardTitle>
                    <CardDescription>No investments yet</CardDescription>
                </CardHeader>
                <CardContent>
                    <div className="text-center py-8 text-muted-foreground">
                        <p>Click "Add Investment" to get started</p>
                    </div>
                </CardContent>
            </Card>
        )
    }

    return (
        <Card>
            <CardHeader>
                <CardTitle>Investments</CardTitle>
                <CardDescription>{investments.length} active investments</CardDescription>
            </CardHeader>
            <CardContent>
                <div className="space-y-3">
                    {investments.map((investment) => {
                        const currentPricePerUnit = investment.currentPrice?.amount || 0
                        const totalValue = investment.currentValue?.amount || 0
                        const gainLoss = investment.unrealizedGainLoss?.amount || 0
                        const isProfit = gainLoss >= 0

                        return (
                            <div
                                key={investment.id}
                                className="flex items-center justify-between p-4 border rounded-lg hover:bg-accent transition-colors"
                            >
                                <div className="flex-1">
                                    <div className="flex items-center gap-2">
                                        <h4 className="font-semibold">{investment.symbol.ticker}</h4>
                                        <Badge variant="secondary" className="text-xs">
                                            {investment.symbol.assetType}
                                        </Badge>
                                        <PriceCacheDialog symbol={investment.symbol.ticker} />
                                    </div>
                                    <div className="text-sm text-muted-foreground mt-1">
                                        {investment.quantity} shares @ {investment.purchasePrice.amount.toFixed(2)} {investment.purchasePrice.currency}
                                    </div>
                                </div>

                                <div className="text-right">
                                    {/* Current price per unit */}
                                    <div className="text-sm text-muted-foreground">
                                        {currentPricePerUnit > 0
                                            ? `${currentPricePerUnit.toFixed(2)} ${investment.currentPrice?.currency || ''} / share`
                                            : 'Price unavailable'}
                                    </div>
                                    {/* Total value */}
                                    <div className="font-semibold">
                                        {totalValue > 0
                                            ? `${totalValue.toFixed(2)} ${investment.currentValue?.currency || ''}`
                                            : '-'}
                                    </div>
                                    {/* Gain/Loss */}
                                    {investment.unrealizedGainLoss && (
                                        <div
                                            className={`text-sm font-medium ${isProfit ? 'text-green-600' : 'text-red-600'
                                                }`}
                                        >
                                            {isProfit ? '+' : ''}
                                            {gainLoss.toFixed(2)} {investment.unrealizedGainLoss.currency}
                                        </div>
                                    )}
                                </div>
                            </div>
                        )
                    })}
                </div>
            </CardContent>
        </Card>
    )
}
