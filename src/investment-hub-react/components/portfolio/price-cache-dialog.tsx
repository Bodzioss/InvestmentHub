'use client'

import { useState } from 'react'
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogHeader,
    DialogTitle,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { History, Loader2 } from 'lucide-react'
import { useQuery } from '@tanstack/react-query'
import apiClient from '@/lib/api/client'

interface CachedPrice {
    symbol: string
    price: number
    currency: string
    fetchedAt: string
    source: string
}

interface PriceCacheDialogProps {
    symbol: string
    children?: React.ReactNode
}

async function getCachedPrices(symbol: string): Promise<CachedPrice[]> {
    const response = await apiClient.get<CachedPrice[]>(`/api/portfolios/cached-prices/${symbol}`)
    return response.data
}

export function PriceCacheDialog({ symbol, children }: PriceCacheDialogProps) {
    const [open, setOpen] = useState(false)

    const { data: prices, isLoading, error } = useQuery({
        queryKey: ['cached-prices', symbol],
        queryFn: () => getCachedPrices(symbol),
        enabled: open, // Only fetch when dialog is open
    })

    return (
        <Dialog open={open} onOpenChange={setOpen}>
            {children || (
                <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => setOpen(true)}
                    className="h-8 w-8 p-0"
                >
                    <History className="h-4 w-4" />
                    <span className="sr-only">View price history</span>
                </Button>
            )}

            <DialogContent className="max-w-3xl max-h-[80vh] overflow-y-auto">
                <DialogHeader>
                    <DialogTitle>Cached Prices for {symbol}</DialogTitle>
                    <DialogDescription>
                        Price history from cache (last 30 days)
                    </DialogDescription>
                </DialogHeader>

                {isLoading ? (
                    <div className="flex items-center justify-center py-8">
                        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
                    </div>
                ) : error ? (
                    <div className="text-center text-muted-foreground py-8">
                        <p>Failed to load cached prices</p>
                    </div>
                ) : !prices || prices.length === 0 ? (
                    <div className="text-center text-muted-foreground py-8">
                        <p>No cached prices found for {symbol}</p>
                        <p className="text-sm mt-2">Try refreshing prices first</p>
                    </div>
                ) : (
                    <div className="border rounded-lg overflow-hidden">
                        <table className="w-full">
                            <thead className="bg-muted">
                                <tr>
                                    <th className="px-4 py-2 text-left text-sm font-medium">Date & Time</th>
                                    <th className="px-4 py-2 text-right text-sm font-medium">Price</th>
                                    <th className="px-4 py-2 text-left text-sm font-medium">Source</th>
                                </tr>
                            </thead>
                            <tbody className="divide-y">
                                {prices.map((price, index) => (
                                    <tr key={index} className="hover:bg-muted/50">
                                        <td className="px-4 py-2 text-sm">
                                            {new Date(price.fetchedAt).toLocaleString()}
                                        </td>
                                        <td className="px-4 py-2 text-sm text-right font-mono">
                                            {price.price.toFixed(2)} {price.currency}
                                        </td>
                                        <td className="px-4 py-2 text-sm text-muted-foreground">
                                            {price.source}
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                        <div className="px-4 py-2 bg-muted/50 text-xs text-muted-foreground">
                            Total: {prices.length} cached price{prices.length !== 1 ? 's' : ''}
                        </div>
                    </div>
                )}
            </DialogContent>
        </Dialog>
    )
}
