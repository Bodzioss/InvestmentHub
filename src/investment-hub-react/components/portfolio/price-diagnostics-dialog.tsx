'use client'

import { useState } from 'react'
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogHeader,
    DialogTitle,
    DialogTrigger,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { RefreshCw, CheckCircle2, XCircle, Info } from 'lucide-react'
import { tracePrice } from '@/lib/api/market-data'
import type { MarketPriceRefreshResult } from '@/lib/types'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Badge } from '@/components/ui/badge'

interface PriceDiagnosticsDialogProps {
    symbol: string
    onRefreshSuccess?: () => void
}

export function PriceDiagnosticsDialog({ symbol, onRefreshSuccess }: PriceDiagnosticsDialogProps) {
    const [isOpen, setIsOpen] = useState(false)
    const [isLoading, setIsLoading] = useState(false)
    const [result, setResult] = useState<MarketPriceRefreshResult | null>(null)

    const handleRefresh = async () => {
        setIsLoading(true)
        try {
            const data = await tracePrice(symbol)
            setResult(data)
            if (data.success && onRefreshSuccess) {
                onRefreshSuccess()
            }
        } catch (error) {
            console.error('Failed to trace price:', error)
        } finally {
            setIsLoading(false)
        }
    }

    return (
        <Dialog open={isOpen} onOpenChange={setIsOpen}>
            <DialogTrigger asChild>
                <Button variant="ghost" size="icon" className="h-6 w-6" title="Price Diagnostics">
                    <Info className="h-4 w-4 text-muted-foreground" />
                </Button>
            </DialogTrigger>
            <DialogContent className="sm:max-w-[600px]">
                <DialogHeader>
                    <DialogTitle className="flex items-center gap-2">
                        Price Diagnostics: {symbol}
                    </DialogTitle>
                    <DialogDescription>
                        Trace the market data fetching process from various providers.
                    </DialogDescription>
                </DialogHeader>

                <div className="space-y-4 py-4">
                    <div className="flex justify-between items-center">
                        <div className="flex gap-2 items-center">
                            {result && (
                                <Badge variant={result.success ? "default" : "destructive"}>
                                    {result.success ? "Success" : "Failed"}
                                </Badge>
                            )}
                            {result?.price && (
                                <span className="font-mono text-sm">
                                    {result.price.toFixed(2)} {result.currency} ({result.source})
                                </span>
                            )}
                        </div>
                        <Button
                            disabled={isLoading}
                            onClick={handleRefresh}
                            size="sm"
                            className="gap-2"
                        >
                            <RefreshCw className={`h-4 w-4 ${isLoading ? 'animate-spin' : ''}`} />
                            Run Diagnostics
                        </Button>
                    </div>

                    <ScrollArea className="h-[300px] w-full rounded-md border bg-muted/50 p-4">
                        {isLoading ? (
                            <div className="flex items-center justify-center h-full text-muted-foreground">
                                <RefreshCw className="h-6 w-6 animate-spin mr-2" />
                                Running trace...
                            </div>
                        ) : result ? (
                            <div className="space-y-1 font-mono text-xs">
                                {result.traceLogs.map((log, i) => (
                                    <div key={i} className="flex gap-2 py-0.5 border-b border-border/50 last:border-0">
                                        <span className="text-muted-foreground shrink-0">{i + 1}.</span>
                                        <span className={
                                            log.includes('ERROR') ? 'text-destructive' :
                                                log.includes('Success') ? 'text-green-600' :
                                                    ''
                                        }>
                                            {log}
                                        </span>
                                    </div>
                                ))}
                            </div>
                        ) : (
                            <div className="flex flex-col items-center justify-center h-full text-muted-foreground text-center px-8">
                                <RefreshCw className="h-8 w-8 mb-4 opacity-20" />
                                <p>Click "Run Diagnostics" to see how the system fetches prices for this instrument.</p>
                            </div>
                        )}
                    </ScrollArea>
                </div>
            </DialogContent>
        </Dialog>
    )
}
