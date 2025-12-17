'use client'

import { useState, useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Search } from 'lucide-react'
import { useAddInvestment, useInstrumentSearch, useMarketPrice } from '@/lib/hooks'
import { addInvestmentSchema, type AddInvestmentFormData } from '@/lib/validation'
import { ASSET_TYPES, CURRENCIES } from '@/lib/constants'
import type { Instrument } from '@/lib/types'
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
import {
    Command,
    CommandEmpty,
    CommandGroup,
    CommandInput,
    CommandItem,
    CommandList,
} from '@/components/ui/command'
import {
    Popover,
    PopoverContent,
    PopoverTrigger,
} from '@/components/ui/popover'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'

interface AddInvestmentDialogProps {
    portfolioId: string
    children: React.ReactNode
}

export function AddInvestmentDialog({ portfolioId, children }: AddInvestmentDialogProps) {
    const [open, setOpen] = useState(false)
    const [searchQuery, setSearchQuery] = useState('')
    const [selectedInstrument, setSelectedInstrument] = useState<Instrument | null>(null)
    const [instrumentPopoverOpen, setInstrumentPopoverOpen] = useState(false)

    const addInvestment = useAddInvestment()

    const form = useForm<AddInvestmentFormData>({
        resolver: zodResolver(addInvestmentSchema),
        defaultValues: {
            ticker: '',
            exchange: 'NASDAQ',
            assetType: 'Stock',
            quantity: 0,
            purchasePrice: 0,
            purchaseCurrency: 'USD',
            purchaseDate: new Date().toISOString().split('T')[0],
        },
    })

    //  Watch asset type for filtering
    const assetType = form.watch('assetType')

    // Search instruments
    const { data: instruments, isLoading: isSearching } = useInstrumentSearch(
        searchQuery,
        { assetType }
    )

    // Fetch market price when instrument selected
    const { data: priceData, isFetching: isFetchingPrice } = useMarketPrice(
        selectedInstrument?.ticker,
        !!selectedInstrument
    )

    // Auto-fill price when fetched
    useEffect(() => {
        if (priceData) {
            form.setValue('purchasePrice', priceData.price)
            form.setValue('purchaseCurrency', priceData.currency)
        }
    }, [priceData, form])

    function handleInstrumentSelect(instrument: Instrument) {
        setSelectedInstrument(instrument)
        form.setValue('ticker', instrument.ticker)
        form.setValue('exchange', instrument.exchange)
        form.setValue('assetType', instrument.assetType)
        setInstrumentPopoverOpen(false)
    }

    function onSubmit(data: AddInvestmentFormData) {
        if (!portfolioId) return

        addInvestment.mutate(
            {
                portfolioId: portfolioId,
                symbol: {
                    ticker: data.ticker,
                    exchange: data.exchange,
                    assetType: data.assetType,
                },
                quantity: data.quantity,
                purchasePrice: {
                    amount: data.purchasePrice,
                    currency: data.purchaseCurrency,
                },
                purchaseDate: data.purchaseDate,
            },
            {
                onSuccess: () => {
                    setOpen(false)
                    form.reset()
                    setSelectedInstrument(null)
                    setSearchQuery('')
                },
            }
        )
    }

    return (
        <Dialog open={open} onOpenChange={setOpen}>
            <DialogTrigger asChild>
                {children}
            </DialogTrigger>
            <DialogContent className="sm:max-w-[600px] max-h-[90vh] overflow-y-auto">
                <DialogHeader>
                    <DialogTitle>Add Investment</DialogTitle>
                    <DialogDescription>
                        Search for an instrument and add it to your portfolio.
                    </DialogDescription>
                </DialogHeader>

                <Form {...form}>
                    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
                        {/* Asset Type */}
                        <FormField
                            control={form.control}
                            name="assetType"
                            render={({ field }) => (
                                <FormItem>
                                    <FormLabel>Asset Type</FormLabel>
                                    <Select
                                        onValueChange={field.onChange}
                                        defaultValue={field.value}
                                        disabled={addInvestment.isPending}
                                    >
                                        <FormControl>
                                            <SelectTrigger>
                                                <SelectValue placeholder="Select asset type" />
                                            </SelectTrigger>
                                        </FormControl>
                                        <SelectContent>
                                            {Object.values(ASSET_TYPES).map((type) => (
                                                <SelectItem key={type} value={type}>
                                                    {type}
                                                </SelectItem>
                                            ))}
                                        </SelectContent>
                                    </Select>
                                    <FormMessage />
                                </FormItem>
                            )}
                        />

                        {/* Instrument Search Autocomplete */}
                        <div className="space-y-2">
                            <label className="text-sm font-medium">Search Instrument</label>
                            <Popover open={instrumentPopoverOpen} onOpenChange={setInstrumentPopoverOpen}>
                                <PopoverTrigger asChild>
                                    <Button
                                        variant="outline"
                                        role="combobox"
                                        className="w-full justify-between"
                                        disabled={addInvestment.isPending}
                                    >
                                        {selectedInstrument
                                            ? `${selectedInstrument.ticker} - ${selectedInstrument.name}`
                                            : 'Search by ticker or name...'}
                                        <Search className="ml-2 h-4 w-4 shrink-0 opacity-50" />
                                    </Button>
                                </PopoverTrigger>
                                <PopoverContent className="w-[400px] p-0">
                                    <Command>
                                        <CommandInput
                                            placeholder="Type ticker or name..."
                                            value={searchQuery}
                                            onValueChange={setSearchQuery}
                                        />
                                        <CommandList>
                                            <CommandEmpty>
                                                {isSearching ? 'Searching...' : 'No instruments found.'}
                                            </CommandEmpty>
                                            {instruments && instruments.length > 0 && (
                                                <CommandGroup>
                                                    {instruments.map((instrument) => (
                                                        <CommandItem
                                                            key={instrument.id}
                                                            value={instrument.ticker}
                                                            onSelect={() => handleInstrumentSelect(instrument)}
                                                        >
                                                            <div className="flex flex-col">
                                                                <div className="flex items-center gap-2">
                                                                    <span className="font-semibold">{instrument.ticker}</span>
                                                                    <Badge variant="secondary" className="text-xs">
                                                                        {instrument.assetType}
                                                                    </Badge>
                                                                </div>
                                                                <span className="text-sm text-muted-foreground">
                                                                    {instrument.name} ({instrument.exchange})
                                                                </span>
                                                            </div>
                                                        </CommandItem>
                                                    ))}
                                                </CommandGroup>
                                            )}
                                        </CommandList>
                                    </Command>
                                </PopoverContent>
                            </Popover>
                            <p className="text-sm text-muted-foreground">
                                Selected: {selectedInstrument ? `${selectedInstrument.ticker} - ${selectedInstrument.exchange}` : 'None'}
                            </p>
                        </div>

                        {/* Quantity and Purchase Price Row */}
                        <div className="grid gap-4 sm:grid-cols-2">
                            {/* Quantity */}
                            <FormField
                                control={form.control}
                                name="quantity"
                                render={({ field }) => (
                                    <FormItem>
                                        <FormLabel>Quantity</FormLabel>
                                        <FormControl>
                                            <Input
                                                type="number"
                                                step="0.000001"
                                                placeholder="10"
                                                disabled={addInvestment.isPending}
                                                {...field}
                                                onChange={(e) => field.onChange(parseFloat(e.target.value) || undefined)}
                                            />
                                        </FormControl>
                                        <FormMessage />
                                    </FormItem>
                                )}
                            />

                            {/* Purchase Price */}
                            <FormField
                                control={form.control}
                                name="purchasePrice"
                                render={({ field }) => (
                                    <FormItem>
                                        <FormLabel>Purchase Price</FormLabel>
                                        <FormControl>
                                            <Input
                                                type="number"
                                                step="0.01"
                                                placeholder="150.00"
                                                disabled={addInvestment.isPending}
                                                {...field}
                                                onChange={(e) => field.onChange(parseFloat(e.target.value) || undefined)}
                                            />
                                        </FormControl>
                                        <FormDescription>
                                            {isFetchingPrice ? 'Fetching latest price...' : null}
                                        </FormDescription>
                                        <FormMessage />
                                    </FormItem>
                                )}
                            />
                        </div>

                        {/* Currency and Date Row */}
                        <div className="grid gap-4 sm:grid-cols-2">
                            {/* Currency */}
                            <FormField
                                control={form.control}
                                name="purchaseCurrency"
                                render={({ field }) => (
                                    <FormItem>
                                        <FormLabel>Currency</FormLabel>
                                        <Select
                                            onValueChange={field.onChange}
                                            defaultValue={field.value}
                                            disabled={addInvestment.isPending}
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
                                        <FormMessage />
                                    </FormItem>
                                )}
                            />

                            {/* Purchase Date */}
                            <FormField
                                control={form.control}
                                name="purchaseDate"
                                render={({ field }) => (
                                    <FormItem>
                                        <FormLabel>Purchase Date</FormLabel>
                                        <FormControl>
                                            <Input
                                                type="date"
                                                disabled={addInvestment.isPending}
                                                {...field}
                                            />
                                        </FormControl>
                                        <FormMessage />
                                    </FormItem>
                                )}
                            />
                        </div>

                        {/* Submit Buttons */}
                        <div className="flex justify-end gap-3 pt-4">
                            <Button
                                type="button"
                                variant="outline"
                                onClick={() => setOpen(false)}
                                disabled={addInvestment.isPending}
                            >
                                Cancel
                            </Button>
                            <Button type="submit" disabled={addInvestment.isPending}>
                                {addInvestment.isPending ? 'Adding...' : 'Add Investment'}
                            </Button>
                        </div>
                    </form>
                </Form>
            </DialogContent>
        </Dialog>
    )
}
