'use client'

import { PieChart, Pie, Cell, ResponsiveContainer, Legend, Tooltip } from 'recharts'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import type { Investment } from '@/lib/types'

interface AssetAllocationChartProps {
    investments: Investment[]
}

const COLORS = ['#0088FE', '#00C49F', '#FFBB28', '#FF8042', '#8884D8', '#82CA9D']

export function AssetAllocationChart({ investments }: AssetAllocationChartProps) {
    if (!investments || investments.length === 0) {
        return (
            <Card>
                <CardHeader>
                    <CardTitle>Asset Allocation</CardTitle>
                    <CardDescription>Distribution by asset type</CardDescription>
                </CardHeader>
                <CardContent className="flex items-center justify-center h-[300px]">
                    <p className="text-sm text-muted-foreground">No data available</p>
                </CardContent>
            </Card>
        )
    }

    // Group investments by asset type and calculate total value
    const assetData = investments.reduce((acc, investment) => {
        const assetType = investment.symbol.assetType
        const value = investment.quantity * (investment.currentPrice?.amount || investment.purchasePrice.amount)

        if (acc[assetType]) {
            acc[assetType] += value
        } else {
            acc[assetType] = value
        }

        return acc
    }, {} as Record<string, number>)

    // Convert to array format for recharts
    const chartData = Object.entries(assetData).map(([name, value]) => ({
        name,
        value: parseFloat(value.toFixed(2))
    }))

    return (
        <Card>
            <CardHeader>
                <CardTitle>Asset Allocation</CardTitle>
                <CardDescription>Distribution by asset type</CardDescription>
            </CardHeader>
            <CardContent>
                <ResponsiveContainer width="100%" height={300}>
                    <PieChart>
                        <Pie
                            data={chartData}
                            cx="50%"
                            cy="50%"
                            labelLine={false}
                            label={({ name, percent }) => `${name}: ${(percent * 100).toFixed(0)}%`}
                            outerRadius={80}
                            fill="#8884d8"
                            dataKey="value"
                        >
                            {chartData.map((entry, index) => (
                                <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                            ))}
                        </Pie>
                        <Tooltip
                            formatter={(value: number) => value.toFixed(2)}
                        />
                        <Legend />
                    </PieChart>
                </ResponsiveContainer>
            </CardContent>
        </Card>
    )
}
