'use client'

import { useState, useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Search, Landmark } from 'lucide-react'
import { useAddInvestment, useInstrumentSearch, useMarketPrice } from '@/lib/hooks'
import { addInvestmentSchema, type AddInvestmentFormData } from '@/lib/validation'
import { ASSET_TYPES, CURRENCIES } from '@/lib/constants'
import type { Instrument } from '@/lib/types'
import { getBondTypes, getBondSeries, type BondTypeInfo, type BondSeries } from '@/lib/api/bonds'
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

    // Bond-specific state
    const [bondTypes, setBondTypes] = useState<BondTypeInfo[]>([])
    const [bondSeries, setBondSeries] = useState<BondSeries[]>([])
    const [selectedBondType, setSelectedBondType] = useState<string>('')
    const [selectedBondSeries, setSelectedBondSeries] = useState<BondSeries | null>(null)
    const [bondSeriesPopoverOpen, setBondSeriesPopoverOpen] = useState(false)
    const [bondSeriesSearch, setBondSeriesSearch] = useState('')

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

    // Watch asset type for conditional rendering
    const assetType = form.watch('assetType')
    const isBond = assetType === 'Bond'

    // Load bond data when dialog opens and Bond is selected
    useEffect(() => {
        if (open && isBond && bondTypes.length === 0) {
            Promise.all([getBondTypes(), getBondSeries()])
                .then(([types, series]) => {
                    setBondTypes(types)
                    setBondSeries(series)
                })
                .catch(console.error)
        }
    }, [open, isBond, bondTypes.length])

    // Filter bond series by selected type
    const filteredBondSeries = bondSeries.filter(
        s => !selectedBondType || s.type === selectedBondType
    ).filter(
        s => !bondSeriesSearch ||
            s.symbol.toLowerCase().includes(bondSeriesSearch.toLowerCase())
    )

    // Search instruments (for non-bond assets)
    const { data: instruments, isLoading: isSearching } = useInstrumentSearch(
        searchQuery,
        { assetType: isBond ? undefined : assetType }
    )

    // Fetch market price when instrument selected
    const { data: priceData, isFetching: isFetchingPrice } = useMarketPrice(
        selectedInstrument?.ticker,
        !!selectedInstrument && !isBond
    )

    // Auto-fill price when fetched
    useEffect(() => {
        if (priceData && !isBond) {
            form.setValue('purchasePrice', priceData.price)
            form.setValue('purchaseCurrency', priceData.currency)
        }
    }, [priceData, form, isBond])

    // Reset bond selection when switching asset types
    useEffect(() => {
        if (!isBond) {
            setSelectedBondType('')
            setSelectedBondSeries(null)
        } else {
            setSelectedInstrument(null)
            form.setValue('purchasePrice', 100) // Bond nominal value
            form.setValue('purchaseCurrency', 'PLN')
            form.setValue('exchange', 'PKO')
        }
    }, [isBond, form])

    function handleInstrumentSelect(instrument: Instrument) {
        setSelectedInstrument(instrument)
        form.setValue('ticker', instrument.ticker)
        form.setValue('exchange', instrument.exchange)
        form.setValue('assetType', instrument.assetType)
        setInstrumentPopoverOpen(false)
    }

    function handleBondSeriesSelect(series: BondSeries) {
        setSelectedBondSeries(series)
        form.setValue('ticker', series.symbol)
        form.setValue('exchange', 'PKO')
        form.setValue('assetType', 'Bond')
        form.setValue('purchasePrice', 100) // Bonds always nominal 100 PLN
        form.setValue('purchaseCurrency', 'PLN')
        setBondSeriesPopoverOpen(false)
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
                    setSelectedBondSeries(null)
                    setSelectedBondType('')
                    setSearchQuery('')
                    setBondSeriesSearch('')
                },
            }
        )
    }

    const formatDate = (dateStr: string) =>
        new Date(dateStr).toLocaleDateString('pl-PL')

    return (
        <Dialog open={open} onOpenChange={setOpen}>
            <DialogTrigger asChild>
                {children}
            </DialogTrigger>
            <DialogContent className="sm:max-w-[600px] max-h-[90vh] overflow-y-auto">
                <DialogHeader>
                    <DialogTitle>Add Investment</DialogTitle>
                    <DialogDescription>
                        {isBond
                            ? 'Select a Polish Treasury Bond series to add to your portfolio.'
                            : 'Search for an instrument and add it to your portfolio.'}
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
                                        value={field.value}
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
                                                    {type === 'Bond' ? '🏛️ Obligacje Skarbowe' : type}
                                                </SelectItem>
                                            ))}
                                        </SelectContent>
                                    </Select>
                                    <FormMessage />
                                </FormItem>
                            )}
                        />

                        {/* BOND-SPECIFIC FIELDS */}
                        {isBond && (
                            <>
                                {/* Bond Type Selector */}
                                <div className="space-y-2">
                                    <label className="text-sm font-medium">Typ obligacji</label>
                                    <Select
                                        value={selectedBondType}
                                        onValueChange={setSelectedBondType}
                                        disabled={addInvestment.isPending}
                                    >
                                        <SelectTrigger>
                                            <SelectValue placeholder="Wybierz typ obligacji" />
                                        </SelectTrigger>
                                        <SelectContent>
                                            <SelectItem value="">Wszystkie typy</SelectItem>
                                            {bondTypes.map((type) => (
                                                <SelectItem key={type.code} value={type.code}>
                                                    <div className="flex items-center gap-2">
                                                        <span className="font-mono">{type.code}</span>
                                                        <span className="text-muted-foreground">
                                                            - {type.name}
                                                        </span>
                                                        {type.isInflationIndexed && (
                                                            <Badge variant="secondary" className="text-xs">
                                                                Indeksowane
                                                            </Badge>
                                                        )}
                                                    </div>
                                                </SelectItem>
                                            ))}
                                        </SelectContent>
                                    </Select>
                                </div>

                                {/* Bond Series Selector */}
                                <div className="space-y-2">
                                    <label className="text-sm font-medium flex items-center gap-2">
                                        <Landmark className="h-4 w-4" />
                                        Wybierz emisję
                                    </label>
                                    <Popover open={bondSeriesPopoverOpen} onOpenChange={setBondSeriesPopoverOpen}>
                                        <PopoverTrigger asChild>
                                            <Button
                                                variant="outline"
                                                role="combobox"
                                                className="w-full justify-between"
                                                disabled={addInvestment.isPending}
                                            >
                                                {selectedBondSeries
                                                    ? `${selectedBondSeries.symbol} - Zapadalność: ${formatDate(selectedBondSeries.maturityDate)}`
                                                    : 'Wyszukaj serię obligacji...'}
                                                <Search className="ml-2 h-4 w-4 shrink-0 opacity-50" />
                                            </Button>
                                        </PopoverTrigger>
                                        <PopoverContent className="w-[450px] p-0">
                                            <Command>
                                                <CommandInput
                                                    placeholder="Wpisz symbol (np. EDO0136)..."
                                                    value={bondSeriesSearch}
                                                    onValueChange={setBondSeriesSearch}
                                                />
                                                <CommandList className="max-h-[300px]">
                                                    <CommandEmpty>
                                                        {bondSeries.length === 0
                                                            ? 'Ładowanie serii...'
                                                            : 'Nie znaleziono serii.'}
                                                    </CommandEmpty>
                                                    {filteredBondSeries.length > 0 && (
                                                        <CommandGroup>
                                                            {filteredBondSeries.slice(0, 50).map((series) => (
                                                                <CommandItem
                                                                    key={series.instrumentId}
                                                                    value={series.symbol}
                                                                    onSelect={() => handleBondSeriesSelect(series)}
                                                                >
                                                                    <div className="flex flex-col w-full">
                                                                        <div className="flex items-center justify-between">
                                                                            <span className="font-mono font-semibold">
                                                                                {series.symbol}
                                                                            </span>
                                                                            <Badge variant="outline">{series.type}</Badge>
                                                                        </div>
                                                                        <div className="flex justify-between text-sm text-muted-foreground">
                                                                            <span>Zapadalność: {formatDate(series.maturityDate)}</span>
                                                                            <span>{series.firstYearRate}% / rok</span>
                                                                        </div>
                                                                    </div>
                                                                </CommandItem>
                                                            ))}
                                                        </CommandGroup>
                                                    )}
                                                </CommandList>
                                            </Command>
                                        </PopoverContent>
                                    </Popover>
                                    {selectedBondSeries && (
                                        <div className="p-3 rounded-lg bg-muted/50 text-sm space-y-1">
                                            <p><strong>Seria:</strong> {selectedBondSeries.symbol}</p>
                                            <p><strong>Oprocentowanie I rok:</strong> {selectedBondSeries.firstYearRate}%</p>
                                            <p><strong>Marża:</strong> {selectedBondSeries.margin}%</p>
                                            <p><strong>Zapadalność:</strong> {formatDate(selectedBondSeries.maturityDate)}</p>
                                        </div>
                                    )}
                                </div>
                            </>
                        )}

                        {/* NON-BOND INSTRUMENT SEARCH */}
                        {!isBond && (
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
                        )}

                        {/* Quantity and Purchase Price Row */}
                        <div className="grid gap-4 sm:grid-cols-2">
                            {/* Quantity */}
                            <FormField
                                control={form.control}
                                name="quantity"
                                render={({ field }) => (
                                    <FormItem>
                                        <FormLabel>{isBond ? 'Ilość obligacji' : 'Quantity'}</FormLabel>
                                        <FormControl>
                                            <Input
                                                type="number"
                                                step={isBond ? '1' : '0.000001'}
                                                min="1"
                                                placeholder={isBond ? '10' : '10'}
                                                disabled={addInvestment.isPending}
                                                {...field}
                                                onChange={(e) => field.onChange(parseFloat(e.target.value) || undefined)}
                                            />
                                        </FormControl>
                                        {isBond && (
                                            <FormDescription>
                                                Wartość: {(field.value || 0) * 100} PLN
                                            </FormDescription>
                                        )}
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
                                        <FormLabel>{isBond ? 'Cena jednostkowa' : 'Purchase Price'}</FormLabel>
                                        <FormControl>
                                            <Input
                                                type="number"
                                                step="0.01"
                                                placeholder={isBond ? '100.00' : '150.00'}
                                                disabled={addInvestment.isPending || isBond}
                                                {...field}
                                                onChange={(e) => field.onChange(parseFloat(e.target.value) || undefined)}
                                            />
                                        </FormControl>
                                        <FormDescription>
                                            {isBond
                                                ? 'Cena nominalna: 100 PLN'
                                                : isFetchingPrice ? 'Fetching latest price...' : null}
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
                                            value={field.value}
                                            disabled={addInvestment.isPending || isBond}
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
                                        <FormLabel>{isBond ? 'Data zakupu' : 'Purchase Date'}</FormLabel>
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
                            <Button
                                type="submit"
                                disabled={addInvestment.isPending || (isBond && !selectedBondSeries)}
                            >
                                {addInvestment.isPending ? 'Adding...' : 'Add Investment'}
                            </Button>
                        </div>
                    </form>
                </Form>
            </DialogContent>
        </Dialog>
    )
}
