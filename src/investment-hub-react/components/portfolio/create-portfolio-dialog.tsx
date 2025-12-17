'use client'

import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { v4 as uuidv4 } from 'uuid'
import { useCreatePortfolio } from '@/lib/hooks'
import { useCurrentUser } from '@/lib/hooks'
import { createPortfolioSchema, type CreatePortfolioFormData } from '@/lib/validation'
import { CURRENCIES } from '@/lib/constants'
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogHeader,
    DialogTitle,
    DialogTrigger,
} from '@/components/ui/dialog'
import {
    Form,
    FormControl,
    FormDescription,
    FormField,
    FormItem,
    FormLabel,
    FormMessage,
} from '@/components/ui/form'
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from '@/components/ui/select'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { Button } from '@/components/ui/button'

interface CreatePortfolioDialogProps {
    children: React.ReactNode
}

export function CreatePortfolioDialog({ children }: CreatePortfolioDialogProps) {
    const [open, setOpen] = useState(false)
    const user = useCurrentUser()
    const createPortfolio = useCreatePortfolio()

    const form = useForm<CreatePortfolioFormData>({
        resolver: zodResolver(createPortfolioSchema),
        defaultValues: {
            name: '',
            description: '',
            currency: 'USD',
        },
    })

    function onSubmit(data: CreatePortfolioFormData) {
        if (!user?.id) return

        createPortfolio.mutate(
            {
                portfolioId: uuidv4(),
                ownerId: user.id,
                name: data.name,
                description: data.description,
                currency: data.currency,
            },
            {
                onSuccess: () => {
                    setOpen(false)
                    form.reset()
                },
            }
        )
    }

    return (
        <Dialog open={open} onOpenChange={setOpen}>
            <DialogTrigger asChild>
                {children}
            </DialogTrigger>
            <DialogContent className="sm:max-w-[500px]">
                <DialogHeader>
                    <DialogTitle>Create Portfolio</DialogTitle>
                    <DialogDescription>
                        Create a new portfolio to track your investments.
                    </DialogDescription>
                </DialogHeader>

                <Form {...form}>
                    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
                        {/* Name Field */}
                        <FormField
                            control={form.control}
                            name="name"
                            render={({ field }) => (
                                <FormItem>
                                    <FormLabel>Portfolio Name</FormLabel>
                                    <FormControl>
                                        <Input
                                            placeholder="My Tech Stocks"
                                            disabled={createPortfolio.isPending}
                                            {...field}
                                        />
                                    </FormControl>
                                    <FormDescription>
                                        A descriptive name for your portfolio
                                    </FormDescription>
                                    <FormMessage />
                                </FormItem>
                            )}
                        />

                        {/* Description Field */}
                        <FormField
                            control={form.control}
                            name="description"
                            render={({ field }) => (
                                <FormItem>
                                    <FormLabel>Description (Optional)</FormLabel>
                                    <FormControl>
                                        <Textarea
                                            placeholder="Long-term technology investments"
                                            disabled={createPortfolio.isPending}
                                            rows={3}
                                            {...field}
                                        />
                                    </FormControl>
                                    <FormMessage />
                                </FormItem>
                            )}
                        />

                        {/* Currency Field */}
                        <FormField
                            control={form.control}
                            name="currency"
                            render={({ field }) => (
                                <FormItem>
                                    <FormLabel>Currency</FormLabel>
                                    <Select
                                        onValueChange={field.onChange}
                                        defaultValue={field.value}
                                        disabled={createPortfolio.isPending}
                                    >
                                        <FormControl>
                                            <SelectTrigger>
                                                <SelectValue placeholder="Select currency" />
                                            </SelectTrigger>
                                        </FormControl>
                                        <SelectContent>
                                            {CURRENCIES.map((currency) => (
                                                <SelectItem key={currency} value={currency}>
                                                    {currency}
                                                </SelectItem>
                                            ))}
                                        </SelectContent>
                                    </Select>
                                    <FormDescription>
                                        The base currency for this portfolio
                                    </FormDescription>
                                    <FormMessage />
                                </FormItem>
                            )}
                        />

                        {/* Submit Button */}
                        <div className="flex justify-end gap-3 pt-4">
                            <Button
                                type="button"
                                variant="outline"
                                onClick={() => setOpen(false)}
                                disabled={createPortfolio.isPending}
                            >
                                Cancel
                            </Button>
                            <Button type="submit" disabled={createPortfolio.isPending}>
                                {createPortfolio.isPending ? 'Creating...' : 'Create Portfolio'}
                            </Button>
                        </div>
                    </form>
                </Form>
            </DialogContent>
        </Dialog>
    )
}
