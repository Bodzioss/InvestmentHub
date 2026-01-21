'use client'

import { useState } from 'react'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { toast } from 'sonner'
import { sellInvestment } from '@/lib/api/investments'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Loader2 } from 'lucide-react'
import type { Investment } from '@/lib/types'

interface SellInvestmentDialogProps {
    investment: Investment
    open: boolean
    onOpenChange: (open: boolean) => void
}

export function SellInvestmentDialog({ investment, open, onOpenChange }: SellInvestmentDialogProps) {
    const [quantityToSell, setQuantityToSell] = useState(investment.quantity)
    const [salePrice, setSalePrice] = useState(investment.currentPrice?.amount || 0)
    const [saleDate, setSaleDate] = useState(new Date().toISOString().split('T')[0])
    const queryClient = useQueryClient()

    const mutation = useMutation({
        mutationFn: async () => {
            await sellInvestment(investment.id, {
                investmentId: investment.id,
                salePrice: {
                    amount: salePrice,
                    currency: investment.purchasePrice.currency,
                },
                quantityToSell: quantityToSell >= investment.quantity ? undefined : quantityToSell,
                saleDate: new Date(saleDate).toISOString(),
            })
        },
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['investments', investment.portfolioId] })
            queryClient.invalidateQueries({ queryKey: ['portfolio', investment.portfolioId] })

            toast.success('Investment sold successfully')
            onOpenChange(false)
        },
        onError: (error: Error) => {
            toast.error(`Failed to sell investment: ${error.message}`)
        },
    })

    const handleSubmit = (e: React.FormEvent) => {
        e.preventDefault()

        if (quantityToSell <= 0 || quantityToSell > investment.quantity) {
            toast.error('Invalid quantity')
            return
        }

        if (salePrice <= 0) {
            toast.error('Sale price must be greater than 0')
            return
        }

        mutation.mutate()
    }

    const saleProceeds = quantityToSell * salePrice
    const costBasis = quantityToSell * investment.purchasePrice.amount
    const gainLoss = saleProceeds - costBasis
    const gainLossPercent = costBasis > 0 ? (gainLoss / costBasis * 100) : 0
    const remainingQuantity = investment.quantity - quantityToSell

    return (
        <Dialog open={open} onOpenChange={onOpenChange}>
            <DialogContent className="sm:max-w-[525px]">
                <form onSubmit={handleSubmit}>
                    <DialogHeader>
                        <DialogTitle>Sell Investment</DialogTitle>
                        <DialogDescription>
                            Sell shares of {investment.symbol.ticker}
                        </DialogDescription>
                    </DialogHeader>

                    <div className="grid gap-4 py-4">
                        {/* Investment Info */}
                        <div className="rounded-lg border p-4">
                            <div className="flex justify-between items-start">
                                <div>
                                    <h3 className="font-semibold text-lg">{investment.symbol.ticker}</h3>
                                    <p className="text-sm text-muted-foreground">{investment.symbol.assetType}</p>
                                </div>
                                <div className="text-right">
                                    <p className="text-sm text-muted-foreground">Available Quantity</p>
                                    <p className="font-semibold">{investment.quantity.toFixed(2)}</p>
                                </div>
                            </div>
                        </div>

                        {/* Sale Information */}
                        <div className="grid grid-cols-2 gap-4">
                            <div className="grid gap-2">
                                <Label htmlFor="quantity">
                                    Quantity to Sell <span className="text-destructive">*</span>
                                </Label>
                                <Input
                                    id="quantity"
                                    type="number"
                                    step="0.00000001"
                                    min="0.00000001"
                                    max={investment.quantity}
                                    value={quantityToSell}
                                    onChange={(e) => setQuantityToSell(parseFloat(e.target.value) || 0)}
                                    required
                                />
                                <p className="text-xs text-muted-foreground">
                                    Enter full amount to sell all
                                </p>
                            </div>

                            <div className="grid gap-2">
                                <Label htmlFor="salePrice">
                                    Sale Price (per unit) <span className="text-destructive">*</span>
                                </Label>
                                <div className="flex gap-2">
                                    <span className="flex items-center px-3 border rounded-l-md bg-muted text-muted-foreground text-sm">
                                        {investment.purchasePrice.currency}
                                    </span>
                                    <Input
                                        id="salePrice"
                                        type="number"
                                        step="0.01"
                                        min="0.01"
                                        value={salePrice}
                                        onChange={(e) => setSalePrice(parseFloat(e.target.value) || 0)}
                                        className="rounded-l-none"
                                        required
                                    />
                                </div>
                            </div>
                        </div>

                        <div className="grid gap-2">
                            <Label htmlFor="saleDate">
                                Sale Date <span className="text-destructive">*</span>
                            </Label>
                            <Input
                                id="saleDate"
                                type="date"
                                max={new Date().toISOString().split('T')[0]}
                                value={saleDate}
                                onChange={(e) => setSaleDate(e.target.value)}
                                required
                            />
                        </div>

                        {/* Summary */}
                        <div className="rounded-lg border p-4 space-y-2">
                            <div className="flex justify-between">
                                <span className="text-muted-foreground">Sale Proceeds</span>
                                <span className="font-semibold text-primary">
                                    {saleProceeds.toFixed(2)} {investment.purchasePrice.currency}
                                </span>
                            </div>
                            <div className="h-px bg-border" />
                            <div className="flex justify-between text-sm">
                                <span className="text-muted-foreground">Purchase Cost</span>
                                <span>
                                    {costBasis.toFixed(2)} {investment.purchasePrice.currency}
                                </span>
                            </div>
                            <div className="h-px bg-border" />
                            <div className="flex justify-between">
                                <span className="text-muted-foreground">Realized Gain/Loss</span>
                                <span className={gainLoss >= 0 ? 'text-green-600 font-semibold' : 'text-red-600 font-semibold'}>
                                    {gainLoss.toFixed(2)} ({gainLossPercent.toFixed(2)}%)
                                </span>
                            </div>
                            {remainingQuantity > 0 && (
                                <>
                                    <div className="h-px bg-border" />
                                    <div className="flex justify-between text-sm">
                                        <span className="text-muted-foreground">Remaining Quantity</span>
                                        <span>{remainingQuantity.toFixed(2)}</span>
                                    </div>
                                </>
                            )}
                        </div>
                    </div>

                    <DialogFooter>
                        <Button
                            type="button"
                            variant="outline"
                            onClick={() => onOpenChange(false)}
                            disabled={mutation.isPending}
                        >
                            Cancel
                        </Button>
                        <Button
                            type="submit"
                            disabled={mutation.isPending}
                            variant="destructive"
                        >
                            {mutation.isPending && (
                                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                            )}
                            Sell
                        </Button>
                    </DialogFooter>
                </form>
            </DialogContent>
        </Dialog>
    )
}
