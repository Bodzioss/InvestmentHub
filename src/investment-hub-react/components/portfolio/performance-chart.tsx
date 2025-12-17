'use client'

import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Legend } from 'recharts'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { usePortfolioPerformance } from '@/lib/hooks'
import type { Portfolio } from '@/lib/types'
import { Loader2 } from 'lucide-react'

interface PerformanceChartProps {
    portfolio: Portfolio
}

export function PerformanceChart({ portfolio }: PerformanceChartProps) {
    const { data: performanceData, isLoading, error } = usePortfolioPerformance(portfolio.id)

    // Transform API data for chart
    const chartData = performanceData?.dataPoints.map(point => ({
        date: new Date(point.date).toLocaleDateString('en-US', { month: 'short', day: 'numeric' }),
        value: point.value,
        cost: portfolio.totalCost?.amount || 0
    })) || []

    return (
        <Card>
            <CardHeader>
                <CardTitle>Performance</CardTitle>
                <CardDescription>Portfolio value over time from {performanceData?.startDate ? new Date(performanceData.startDate).toLocaleDateString() : 'purchase date'}</CardDescription>
            </CardHeader>
            <CardContent>
                {isLoading ? (
                    <div className="flex items-center justify-center h-[300px]">
                        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
                    </div>
                ) : error ? (
                    <div className="flex items-center justify-center h-[300px] text-muted-foreground">
                        <p>No price history available yet. Refresh prices to see chart.</p>
                    </div>
                ) : chartData.length === 0 ? (
                    <div className="flex items-center justify-center h-[300px] text-muted-foreground">
                        <p>No data available</p>
                    </div>
                ) : (
                    <ResponsiveContainer width="100%" height={300}>
                        <LineChart data={chartData}>
                            <CartesianGrid strokeDasharray="3 3" />
                            <XAxis
                                dataKey="date"
                                tick={{ fontSize: 12 }}
                            />
                            <YAxis
                                tick={{ fontSize: 12 }}
                                tickFormatter={(value) => `${value.toFixed(0)}`}
                            />
                            <Tooltip
                                formatter={(value: number) => [`${value.toFixed(2)} ${portfolio.currency}`, '']}
                            />
                            <Legend />
                            <Line
                                type="monotone"
                                dataKey="value"
                                stroke="#8884d8"
                                strokeWidth={2}
                                name="Portfolio Value"
                                dot={{ r: 4 }}
                            />
                            <Line
                                type="monotone"
                                dataKey="cost"
                                stroke="#82ca9d"
                                strokeWidth={2}
                                strokeDasharray="5 5"
                                name="Total Cost"
                                dot={false}
                            />
                        </LineChart>
                    </ResponsiveContainer>
                )}
            </CardContent>
        </Card>
    )
}
