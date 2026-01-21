'use client'

import { useState } from 'react'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { toast } from 'sonner'
import { updateInvestmentValue } from '@/lib/api/investments'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Loader2 } from 'lucide-react'
import type { Investment } from '@/lib/types'

interface UpdateInvestmentDialogProps {
    investment: Investment
    open: boolean
    onOpenChange: (open: boolean) => void
}

export function UpdateInvestmentDialog({ investment, open, onOpenChange }: UpdateInvestmentDialogProps) {
    const [newPrice, setNewPrice] = useState(investment.currentPrice?.amount || 0)
    const queryClient = useQueryClient()

    const mutation = useMutation({
        mutationFn: async () => {
            await updateInvestmentValue(investment.id, {
                investmentId: investment.id,
                currentPrice: {
                    amount: newPrice,
                    currency: investment.purchasePrice.currency,
                },
            })
        },
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['investments', investment.portfolioId] })
            queryClient.invalidateQueries({ queryKey: ['portfolio', investment.portfolioId] })

            toast.success('Investment value updated successfully')
            onOpenChange(false)
        },
        onError: (error: Error) => {
            toast.error(`Failed to update investment: ${error.message}`)
        },
    })

    const handleSubmit = (e: React.FormEvent) => {
        e.preventDefault()

        if (newPrice <= 0) {
            toast.error('Price must be greater than 0')
            return
        }

        mutation.mutate()
    }

    const change = (newPrice - (investment.currentPrice?.amount || 0)) * investment.quantity
    const changePercent = investment.currentPrice?.amount
        ? ((newPrice - investment.currentPrice.amount) / investment.currentPrice.amount * 100)
        : 0

    return (
        <Dialog open={open} onOpenChange={onOpenChange}>
            <DialogContent className="sm:max-w-[525px]">
                <form onSubmit={handleSubmit}>
                    <DialogHeader>
                        <DialogTitle>Update Investment Value</DialogTitle>
                        <DialogDescription>
                            Update the current market price for {investment.symbol.ticker}
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
                                    <p className="text-sm text-muted-foreground">Current Value</p>
                                    <p className="font-semibold">
                                        {investment.currentValue?.amount.toFixed(2)} {investment.currentValue?.currency}
                                    </p>
                                </div>
                            </div>
                        </div>

                        {/* New Price Input */}
                        <div className="grid gap-2">
                            <Label htmlFor="newPrice">
                                New Current Price (per unit) <span className="text-destructive">*</span>
                            </Label>
                            <div className="flex gap-2">
                                <span className="flex items-center px-3 border rounded-l-md bg-muted text-muted-foreground">
                                    {investment.purchasePrice.currency}
                                </span>
                                <Input
                                    id="newPrice"
                                    type="number"
                                    step="0.01"
                                    min="0.01"
                                    value={newPrice}
                                    onChange={(e) => setNewPrice(parseFloat(e.target.value) || 0)}
                                    className="flex-1 rounded-l-none"
                                    required
                                />
                            </div>
                            <p className="text-xs text-muted-foreground">
                                Enter the current market price per unit
                            </p>
                        </div>

                        {/* Summary */}
                        <div className="rounded-lg border p-4 space-y-2">
                            <div className="flex justify-between text-sm">
                                <span className="text-muted-foreground">Quantity</span>
                                <span>{investment.quantity.toFixed(2)}</span>
                            </div>
                            <div className="h-px bg-border" />
                            <div className="flex justify-between">
                                <span className="text-muted-foreground">New Total Value</span>
                                <span className="font-semibold text-primary">
                                    {(newPrice * investment.quantity).toFixed(2)} {investment.purchasePrice.currency}
                                </span>
                            </div>
                            <div className="h-px bg-border" />
                            <div className="flex justify-between text-sm">
                                <span className="text-muted-foreground">Change</span>
                                <span className={change >= 0 ? 'text-green-600' : 'text-red-600'}>
                                    {change.toFixed(2)} ({changePercent.toFixed(2)}%)
                                </span>
                            </div>
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
                        <Button type="submit" disabled={mutation.isPending}>
                            {mutation.isPending && (
                                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                            )}
                            Update
                        </Button>
                    </DialogFooter>
                </form>
            </DialogContent>
        </Dialog>
    )
}
