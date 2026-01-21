'use client'

import { TrendingUp, TrendingDown, Minus } from 'lucide-react'
import { usePositions } from '@/lib/hooks/use-positions'
import type { Position } from '@/lib/types'
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow,
} from '@/components/ui/table'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { cn } from '@/lib/utils'

interface PositionsListProps {
    portfolioId: string
}

export function PositionsList({ portfolioId }: PositionsListProps) {
    const { data, isLoading, error } = usePositions(portfolioId)

    if (isLoading) {
        return (
            <Card>
                <CardHeader>
                    <CardTitle>Positions</CardTitle>
                </CardHeader>
                <CardContent>
                    <div className="space-y-3">
                        {[1, 2, 3].map((i) => (
                            <Skeleton key={i} className="h-12 w-full" />
                        ))}
                    </div>
                </CardContent>
            </Card>
        )
    }

    if (error) {
        return (
            <Card>
                <CardHeader>
                    <CardTitle>Positions</CardTitle>
                </CardHeader>
                <CardContent>
                    <p className="text-destructive">Failed to load positions</p>
                </CardContent>
            </Card>
        )
    }

    const positions = data?.positions ?? []
    const summary = data?.summary

    if (positions.length === 0) {
        return (
            <Card>
                <CardHeader>
                    <CardTitle>Positions</CardTitle>
                    <CardDescription>No positions yet</CardDescription>
                </CardHeader>
                <CardContent>
                    <p className="text-muted-foreground text-sm">
                        Record buy transactions to see your positions here.
                    </p>
                </CardContent>
            </Card>
        )
    }

    function formatCurrency(amount: number, currency: string): string {
        return `${amount.toFixed(2)} ${currency}`
    }

    function GainIndicator({ value, percent }: { value: number; percent: number }) {
        if (value === 0) {
            return <Minus className="h-4 w-4 text-muted-foreground" />
        }
        const isPositive = value > 0
        return (
            <div className={cn('flex items-center gap-1', isPositive ? 'text-green-600' : 'text-red-600')}>
                {isPositive ? <TrendingUp className="h-4 w-4" /> : <TrendingDown className="h-4 w-4" />}
                <span className="font-mono text-sm">
                    {isPositive ? '+' : ''}{value.toFixed(2)} ({percent.toFixed(2)}%)
                </span>
            </div>
        )
    }

    return (
        <Card>
            <CardHeader>
                <CardTitle>Positions ({positions.length})</CardTitle>
                <CardDescription>FIFO-calculated positions from your transactions</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
                {/* Summary */}
                {summary && (
                    <div className="grid gap-4 md:grid-cols-4 mb-4">
                        <div className="rounded-lg border p-3">
                            <p className="text-sm text-muted-foreground">Total Value</p>
                            <p className="text-xl font-bold">{formatCurrency(summary.totalValue, summary.currency)}</p>
                        </div>
                        <div className="rounded-lg border p-3">
                            <p className="text-sm text-muted-foreground">Total Cost</p>
                            <p className="text-xl font-bold">{formatCurrency(summary.totalCost, summary.currency)}</p>
                        </div>
                        <div className="rounded-lg border p-3">
                            <p className="text-sm text-muted-foreground">Unrealized P/L</p>
                            <p className={cn(
                                'text-xl font-bold',
                                summary.totalUnrealizedGainLoss > 0 ? 'text-green-600' : summary.totalUnrealizedGainLoss < 0 ? 'text-red-600' : ''
                            )}>
                                {summary.totalUnrealizedGainLoss >= 0 ? '+' : ''}{formatCurrency(summary.totalUnrealizedGainLoss, summary.currency)}
                            </p>
                        </div>
                        <div className="rounded-lg border p-3">
                            <p className="text-sm text-muted-foreground">Total Income</p>
                            <p className="text-xl font-bold text-blue-600">
                                {formatCurrency(summary.totalDividends + summary.totalInterest, summary.currency)}
                            </p>
                        </div>
                    </div>
                )}

                {/* Table */}
                <div className="rounded-md border">
                    <Table>
                        <TableHeader>
                            <TableRow>
                                <TableHead>Symbol</TableHead>
                                <TableHead className="text-right">Quantity</TableHead>
                                <TableHead className="text-right">Avg Cost</TableHead>
                                <TableHead className="text-right">Current</TableHead>
                                <TableHead className="text-right">Value</TableHead>
                                <TableHead className="text-right">Unrealized P/L</TableHead>
                                <TableHead className="text-right">Income</TableHead>
                            </TableRow>
                        </TableHeader>
                        <TableBody>
                            {positions.map((p) => (
                                <TableRow key={`${p.ticker}-${p.exchange}`}>
                                    <TableCell>
                                        <div className="font-medium">{p.ticker}</div>
                                        <div className="text-xs text-muted-foreground flex items-center gap-1">
                                            {p.exchange}
                                            <Badge variant="outline" className="text-xs">{p.assetType}</Badge>
                                        </div>
                                    </TableCell>
                                    <TableCell className="text-right font-mono">
                                        {p.totalQuantity.toFixed(4)}
                                    </TableCell>
                                    <TableCell className="text-right font-mono">
                                        {p.averageCost.toFixed(2)} {p.currency}
                                    </TableCell>
                                    <TableCell className="text-right font-mono">
                                        {p.currentPrice.toFixed(2)} {p.currency}
                                    </TableCell>
                                    <TableCell className="text-right font-mono font-medium">
                                        {p.currentValue.toFixed(2)} {p.currency}
                                    </TableCell>
                                    <TableCell className="text-right">
                                        <GainIndicator value={p.unrealizedGainLoss} percent={p.unrealizedGainLossPercent} />
                                    </TableCell>
                                    <TableCell className="text-right">
                                        {p.totalIncome > 0 ? (
                                            <span className="font-mono text-blue-600">
                                                +{p.totalIncome.toFixed(2)}
                                            </span>
                                        ) : (
                                            <span className="text-muted-foreground">-</span>
                                        )}
                                    </TableCell>
                                </TableRow>
                            ))}
                        </TableBody>
                    </Table>
                </div>
            </CardContent>
        </Card>
    )
}
