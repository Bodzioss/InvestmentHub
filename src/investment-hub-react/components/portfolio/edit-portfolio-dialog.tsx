'use client'

import { useState } from 'react'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { toast } from 'sonner'
import { updatePortfolio } from '@/lib/api/portfolios'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Loader2 } from 'lucide-react'
import type { Portfolio, UpdatePortfolioRequest } from '@/lib/types'

interface EditPortfolioDialogProps {
    portfolio: Portfolio
    open: boolean
    onOpenChange: (open: boolean) => void
}

export function EditPortfolioDialog({ portfolio, open, onOpenChange }: EditPortfolioDialogProps) {
    const [name, setName] = useState(portfolio.name)
    const [description, setDescription] = useState(portfolio.description || '')
    const queryClient = useQueryClient()

    const mutation = useMutation({
        mutationFn: async (request: UpdatePortfolioRequest) => {
            await updatePortfolio(portfolio.id, request)
        },
        onSuccess: () => {
            // Invalidate queries to refresh data
            queryClient.invalidateQueries({ queryKey: ['portfolios'] })
            queryClient.invalidateQueries({ queryKey: ['portfolio', portfolio.id] })

            toast.success('Portfolio updated successfully')
            onOpenChange(false)
        },
        onError: (error: Error) => {
            toast.error(`Failed to update portfolio: ${error.message}`)
        },
    })

    const handleSubmit = (e: React.FormEvent) => {
        e.preventDefault()

        if (!name.trim()) {
            toast.error('Portfolio name is required')
            return
        }

        mutation.mutate({
            name: name.trim(),
            description: description.trim() || undefined,
        })
    }

    return (
        <Dialog open={open} onOpenChange={onOpenChange}>
            <DialogContent className="sm:max-w-[525px]">
                <form onSubmit={handleSubmit}>
                    <DialogHeader>
                        <DialogTitle>Edit Portfolio</DialogTitle>
                        <DialogDescription>
                            Update your portfolio name and description
                        </DialogDescription>
                    </DialogHeader>

                    <div className="grid gap-4 py-4">
                        <div className="grid gap-2">
                            <Label htmlFor="name">
                                Name <span className="text-destructive">*</span>
                            </Label>
                            <Input
                                id="name"
                                value={name}
                                onChange={(e) => setName(e.target.value)}
                                placeholder="My Portfolio"
                                maxLength={100}
                                required
                            />
                        </div>

                        <div className="grid gap-2">
                            <Label htmlFor="description">Description</Label>
                            <Textarea
                                id="description"
                                value={description}
                                onChange={(e) => setDescription(e.target.value)}
                                placeholder="Optional description..."
                                rows={3}
                                maxLength={500}
                            />
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
                            Save Changes
                        </Button>
                    </DialogFooter>
                </form>
            </DialogContent>
        </Dialog>
    )
}
