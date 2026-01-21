'use client'

import { useState } from 'react'
import { Wallet, Percent, TrendingUp, Calendar } from 'lucide-react'
import { useIncomeSummary } from '@/lib/hooks/use-income'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow,
} from '@/components/ui/table'

interface IncomeReportProps {
    portfolioId: string
}

export function IncomeReport({ portfolioId }: IncomeReportProps) {
    const currentYear = new Date().getFullYear()
    const [selectedYear, setSelectedYear] = useState<number | undefined>(undefined)

    const { data, isLoading, error } = useIncomeSummary(portfolioId, selectedYear)

    const years = Array.from({ length: 5 }, (_, i) => currentYear - i)

    if (isLoading) {
        return (
            <Card>
                <CardHeader>
                    <CardTitle>Income Report</CardTitle>
                </CardHeader>
                <CardContent>
                    <div className="space-y-3">
                        {[1, 2, 3].map((i) => (
                            <Skeleton key={i} className="h-16 w-full" />
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
                    <CardTitle>Income Report</CardTitle>
                </CardHeader>
                <CardContent>
                    <p className="text-destructive">Failed to load income data</p>
                </CardContent>
            </Card>
        )
    }

    const summary = data

    if (!summary || (summary.totalDividends === 0 && summary.totalInterest === 0)) {
        return (
            <Card>
                <CardHeader>
                    <CardTitle>Income Report</CardTitle>
                    <CardDescription>No income recorded yet</CardDescription>
                </CardHeader>
                <CardContent>
                    <p className="text-muted-foreground text-sm">
                        Record dividend or interest payments to see your income report here.
                    </p>
                </CardContent>
            </Card>
        )
    }

    function formatCurrency(amount: number): string {
        return `${amount.toFixed(2)} ${summary?.currency ?? 'USD'}`
    }

    return (
        <Card>
            <CardHeader>
                <div className="flex items-center justify-between">
                    <div>
                        <CardTitle className="flex items-center gap-2">
                            <TrendingUp className="h-5 w-5" />
                            Income Report
                        </CardTitle>
                        <CardDescription>Dividends and interest payments</CardDescription>
                    </div>
                    <Select
                        value={selectedYear?.toString() ?? 'all'}
                        onValueChange={(v) => setSelectedYear(v === 'all' ? undefined : parseInt(v))}
                    >
                        <SelectTrigger className="w-[120px]">
                            <SelectValue placeholder="Year" />
                        </SelectTrigger>
                        <SelectContent>
                            <SelectItem value="all">All Time</SelectItem>
                            {years.map((year) => (
                                <SelectItem key={year} value={year.toString()}>
                                    {year}
                                </SelectItem>
                            ))}
                        </SelectContent>
                    </Select>
                </div>
            </CardHeader>
            <CardContent className="space-y-6">
                {/* Summary Cards */}
                <div className="grid gap-4 md:grid-cols-3">
                    <div className="rounded-lg border p-4 bg-gradient-to-br from-blue-50 to-blue-100 dark:from-blue-950 dark:to-blue-900">
                        <div className="flex items-center gap-2 text-blue-700 dark:text-blue-300">
                            <Wallet className="h-5 w-5" />
                            <span className="text-sm font-medium">Dividends</span>
                        </div>
                        <p className="text-2xl font-bold mt-2 text-blue-900 dark:text-blue-100">
                            {formatCurrency(summary.totalDividends)}
                        </p>
                    </div>
                    <div className="rounded-lg border p-4 bg-gradient-to-br from-purple-50 to-purple-100 dark:from-purple-950 dark:to-purple-900">
                        <div className="flex items-center gap-2 text-purple-700 dark:text-purple-300">
                            <Percent className="h-5 w-5" />
                            <span className="text-sm font-medium">Interest</span>
                        </div>
                        <p className="text-2xl font-bold mt-2 text-purple-900 dark:text-purple-100">
                            {formatCurrency(summary.totalInterest)}
                        </p>
                    </div>
                    <div className="rounded-lg border p-4 bg-gradient-to-br from-green-50 to-green-100 dark:from-green-950 dark:to-green-900">
                        <div className="flex items-center gap-2 text-green-700 dark:text-green-300">
                            <TrendingUp className="h-5 w-5" />
                            <span className="text-sm font-medium">Total Income</span>
                        </div>
                        <p className="text-2xl font-bold mt-2 text-green-900 dark:text-green-100">
                            {formatCurrency(summary.totalIncome)}
                        </p>
                    </div>
                </div>

                {/* Tabs for breakdowns */}
                <Tabs defaultValue="by-symbol">
                    <TabsList>
                        <TabsTrigger value="by-symbol">By Symbol</TabsTrigger>
                        <TabsTrigger value="by-month">By Month</TabsTrigger>
                    </TabsList>

                    <TabsContent value="by-symbol">
                        {summary.bySymbol.length > 0 ? (
                            <div className="rounded-md border">
                                <Table>
                                    <TableHeader>
                                        <TableRow>
                                            <TableHead>Symbol</TableHead>
                                            <TableHead className="text-right">Dividends</TableHead>
                                            <TableHead className="text-right">Interest</TableHead>
                                            <TableHead className="text-right">Total</TableHead>
                                        </TableRow>
                                    </TableHeader>
                                    <TableBody>
                                        {summary.bySymbol.map((item) => (
                                            <TableRow key={`${item.ticker}-${item.exchange}`}>
                                                <TableCell>
                                                    <div className="font-medium">{item.ticker}</div>
                                                    <div className="text-xs text-muted-foreground">{item.exchange}</div>
                                                </TableCell>
                                                <TableCell className="text-right font-mono">
                                                    {item.dividends > 0 ? (
                                                        <span className="text-blue-600">+{item.dividends.toFixed(2)}</span>
                                                    ) : '-'}
                                                </TableCell>
                                                <TableCell className="text-right font-mono">
                                                    {item.interest > 0 ? (
                                                        <span className="text-purple-600">+{item.interest.toFixed(2)}</span>
                                                    ) : '-'}
                                                </TableCell>
                                                <TableCell className="text-right font-mono font-bold text-green-600">
                                                    +{item.total.toFixed(2)}
                                                </TableCell>
                                            </TableRow>
                                        ))}
                                    </TableBody>
                                </Table>
                            </div>
                        ) : (
                            <p className="text-muted-foreground text-sm py-4">No data by symbol</p>
                        )}
                    </TabsContent>

                    <TabsContent value="by-month">
                        {summary.byMonth.length > 0 ? (
                            <div className="rounded-md border">
                                <Table>
                                    <TableHeader>
                                        <TableRow>
                                            <TableHead>Month</TableHead>
                                            <TableHead className="text-right">Dividends</TableHead>
                                            <TableHead className="text-right">Interest</TableHead>
                                            <TableHead className="text-right">Total</TableHead>
                                        </TableRow>
                                    </TableHeader>
                                    <TableBody>
                                        {summary.byMonth.map((item) => (
                                            <TableRow key={`${item.year}-${item.month}`}>
                                                <TableCell className="font-medium">
                                                    {new Date(item.year, item.month - 1).toLocaleDateString('en-US', {
                                                        year: 'numeric',
                                                        month: 'long'
                                                    })}
                                                </TableCell>
                                                <TableCell className="text-right font-mono">
                                                    {item.dividends > 0 ? (
                                                        <span className="text-blue-600">+{item.dividends.toFixed(2)}</span>
                                                    ) : '-'}
                                                </TableCell>
                                                <TableCell className="text-right font-mono">
                                                    {item.interest > 0 ? (
                                                        <span className="text-purple-600">+{item.interest.toFixed(2)}</span>
                                                    ) : '-'}
                                                </TableCell>
                                                <TableCell className="text-right font-mono font-bold text-green-600">
                                                    +{item.total.toFixed(2)}
                                                </TableCell>
                                            </TableRow>
                                        ))}
                                    </TableBody>
                                </Table>
                            </div>
                        ) : (
                            <p className="text-muted-foreground text-sm py-4">No data by month</p>
                        )}
                    </TabsContent>
                </Tabs>
            </CardContent>
        </Card>
    )
}
