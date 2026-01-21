'use client'

import { useState, useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Search, Plus, Minus, Wallet, Percent, Loader2, Landmark, FileSpreadsheet } from 'lucide-react'
import { z } from 'zod'
import { useInstrumentSearch, useMarketPrice } from '@/lib/hooks'
import { useRecordBuy, useRecordSell, useRecordDividend, useRecordInterest } from '@/lib/hooks/use-transactions'
import { ASSET_TYPES, CURRENCIES } from '@/lib/constants'
import type { Instrument } from '@/lib/types'
import { getBondTypes, getBondSeries, type BondTypeInfo, type BondSeries } from '@/lib/api/bonds'
import { ImportCsvDialog } from './import-csv-dialog'
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
    Tabs,
    TabsContent,
    TabsList,
    TabsTrigger,
} from '@/components/ui/tabs'
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
import { Textarea } from '@/components/ui/textarea'

// Form Schemas
const buySchema = z.object({
    ticker: z.string().min(1, 'Ticker is required'),
    exchange: z.string().min(1, 'Exchange is required'),
    assetType: z.string().min(1, 'Asset type is required'),
    quantity: z.number().positive('Quantity must be positive'),
    pricePerUnit: z.number().positive('Price must be positive'),
    currency: z.string().min(1, 'Currency is required'),
    fee: z.number().min(0).optional(),
    transactionDate: z.string().min(1, 'Date is required'),
    maturityDate: z.string().optional(),
    notes: z.string().optional(),
})

const sellSchema = z.object({
    ticker: z.string().min(1, 'Ticker is required'),
    exchange: z.string().min(1, 'Exchange is required'),
    assetType: z.string().min(1, 'Asset type is required'),
    quantity: z.number().positive('Quantity must be positive'),
    salePrice: z.number().positive('Price must be positive'),
    currency: z.string().min(1, 'Currency is required'),
    fee: z.number().min(0).optional(),
    transactionDate: z.string().min(1, 'Date is required'),
    notes: z.string().optional(),
})

const incomeSchema = z.object({
    ticker: z.string().min(1, 'Ticker is required'),
    exchange: z.string().min(1, 'Exchange is required'),
    grossAmount: z.number().positive('Amount must be positive'),
    currency: z.string().min(1, 'Currency is required'),
    paymentDate: z.string().min(1, 'Date is required'),
    taxRate: z.number().min(0).max(100).optional(),
    notes: z.string().optional(),
})

type BuyFormData = z.infer<typeof buySchema>
type SellFormData = z.infer<typeof sellSchema>
type IncomeFormData = z.infer<typeof incomeSchema>

interface RecordTransactionDialogProps {
    portfolioId: string
    children: React.ReactNode
    defaultTab?: 'buy' | 'sell' | 'dividend' | 'interest'
}

export function RecordTransactionDialog({ portfolioId, children, defaultTab = 'buy' }: RecordTransactionDialogProps) {
    const [open, setOpen] = useState(false)
    const [activeTab, setActiveTab] = useState(defaultTab)
    const [searchQuery, setSearchQuery] = useState('')
    const [selectedInstrument, setSelectedInstrument] = useState<Instrument | null>(null)
    const [popoverOpen, setPopoverOpen] = useState(false)

    // Asset type and bond selection state
    const [selectedAssetType, setSelectedAssetType] = useState<string>('Stock')
    const [bondTypes, setBondTypes] = useState<BondTypeInfo[]>([])
    const [bondSeries, setBondSeries] = useState<BondSeries[]>([])
    const [selectedBondType, setSelectedBondType] = useState<string>('all')
    const [selectedBondSeries, setSelectedBondSeries] = useState<BondSeries | null>(null)
    const [bondPopoverOpen, setBondPopoverOpen] = useState(false)
    const [bondSearchQuery, setBondSearchQuery] = useState('')

    const isBond = selectedAssetType === 'Bond'

    const recordBuy = useRecordBuy()
    const recordSell = useRecordSell()
    const recordDividend = useRecordDividend()
    const recordInterest = useRecordInterest()

    const { data: instruments, isLoading: isSearching } = useInstrumentSearch(searchQuery)

    // Load bond data when bond is selected
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

    // Filter bond series by type
    const filteredBondSeries = bondSeries.filter(
        s => (selectedBondType === 'all' || s.type === selectedBondType) &&
            (!bondSearchQuery || s.symbol.toLowerCase().includes(bondSearchQuery.toLowerCase()))
    )

    // Fetch market price when instrument is selected
    const { data: marketPrice, isFetching: isFetchingPrice } = useMarketPrice(
        selectedInstrument?.ticker,
        !!selectedInstrument
    )

    const buyForm = useForm<BuyFormData>({
        resolver: zodResolver(buySchema),
        defaultValues: {
            ticker: '', exchange: 'NASDAQ', assetType: 'Stock',
            quantity: 0, pricePerUnit: 0, currency: 'USD', fee: 0,
            transactionDate: new Date().toISOString().split('T')[0],
        },
    })

    const sellForm = useForm<SellFormData>({
        resolver: zodResolver(sellSchema),
        defaultValues: {
            ticker: '', exchange: 'NASDAQ', assetType: 'Stock',
            quantity: 0, salePrice: 0, currency: 'USD', fee: 0,
            transactionDate: new Date().toISOString().split('T')[0],
        },
    })

    const dividendForm = useForm<IncomeFormData>({
        resolver: zodResolver(incomeSchema),
        defaultValues: {
            ticker: '', exchange: 'NASDAQ', grossAmount: 0, currency: 'USD',
            paymentDate: new Date().toISOString().split('T')[0], taxRate: 19,
        },
    })

    const interestForm = useForm<IncomeFormData>({
        resolver: zodResolver(incomeSchema),
        defaultValues: {
            ticker: '', exchange: 'NASDAQ', grossAmount: 0, currency: 'USD',
            paymentDate: new Date().toISOString().split('T')[0], taxRate: 19,
        },
    })

    // Auto-fill price when market price is fetched
    useEffect(() => {
        if (marketPrice) {
            if (activeTab === 'buy') {
                buyForm.setValue('pricePerUnit', marketPrice.price)
                buyForm.setValue('currency', marketPrice.currency)
            } else if (activeTab === 'sell') {
                sellForm.setValue('salePrice', marketPrice.price)
                sellForm.setValue('currency', marketPrice.currency)
            }
        }
    }, [marketPrice, activeTab, buyForm, sellForm])

    function handleInstrumentSelect(instrument: Instrument, form: any) {
        setSelectedInstrument(instrument)
        form.setValue('ticker', instrument.ticker)
        form.setValue('exchange', instrument.exchange)
        if (form.getValues('assetType') !== undefined) {
            form.setValue('assetType', instrument.assetType)
        }
        setPopoverOpen(false)
        setSearchQuery('')
    }

    function handleBondSeriesSelect(series: BondSeries, form: any) {
        setSelectedBondSeries(series)
        form.setValue('ticker', series.symbol)
        form.setValue('exchange', 'PKO')
        form.setValue('assetType', 'Bond')
        form.setValue('pricePerUnit', 100) // Nominal value
        form.setValue('currency', 'PLN')
        setBondPopoverOpen(false)
        setBondSearchQuery('')
    }

    function handleAssetTypeChange(type: string, form: any) {
        setSelectedAssetType(type)
        form.setValue('assetType', type)
        if (type === 'Bond') {
            form.setValue('currency', 'PLN')
            form.setValue('exchange', 'PKO')
            form.setValue('pricePerUnit', 100)
            setSelectedInstrument(null)
        } else {
            setSelectedBondSeries(null)
            setSelectedBondType('all')
        }
    }

    function onBuySubmit(data: BuyFormData) {
        recordBuy.mutate({ portfolioId, request: data }, {
            onSuccess: () => { setOpen(false); buyForm.reset(); setSelectedInstrument(null) }
        })
    }

    function onSellSubmit(data: SellFormData) {
        recordSell.mutate({ portfolioId, request: data }, {
            onSuccess: () => { setOpen(false); sellForm.reset(); setSelectedInstrument(null) }
        })
    }

    function onDividendSubmit(data: IncomeFormData) {
        recordDividend.mutate({ portfolioId, request: data }, {
            onSuccess: () => { setOpen(false); dividendForm.reset(); setSelectedInstrument(null) }
        })
    }

    function onInterestSubmit(data: IncomeFormData) {
        recordInterest.mutate({ portfolioId, request: data }, {
            onSuccess: () => { setOpen(false); interestForm.reset(); setSelectedInstrument(null) }
        })
    }

    const isPending = recordBuy.isPending || recordSell.isPending || recordDividend.isPending || recordInterest.isPending

    const AssetTypeField = ({ form }: { form: any }) => (
        <div className="space-y-2">
            <label className="text-sm font-medium">Asset Type</label>
            <Select value={selectedAssetType} onValueChange={(v) => handleAssetTypeChange(v, form)} disabled={isPending}>
                <SelectTrigger>
                    <SelectValue placeholder="Select asset type" />
                </SelectTrigger>
                <SelectContent>
                    <SelectItem value="Stock">Stock</SelectItem>
                    <SelectItem value="Bond">🏛️ Obligacje Skarbowe</SelectItem>
                    <SelectItem value="ETF">ETF</SelectItem>
                    <SelectItem value="Commodity">Commodity</SelectItem>
                    <SelectItem value="Cryptocurrency">Cryptocurrency</SelectItem>
                    <SelectItem value="Other">Other</SelectItem>
                </SelectContent>
            </Select>
        </div>
    )

    const BondSeriesField = ({ form }: { form: any }) => (
        <div className="space-y-3">
            {/* Bond Type Selector */}
            <div className="space-y-2">
                <label className="text-sm font-medium">Typ obligacji</label>
                <Select value={selectedBondType} onValueChange={setSelectedBondType} disabled={isPending}>
                    <SelectTrigger>
                        <SelectValue placeholder="Wszystkie typy" />
                    </SelectTrigger>
                    <SelectContent>
                        <SelectItem value="all">Wszystkie typy</SelectItem>
                        {bondTypes.map((type) => (
                            <SelectItem key={type.code} value={type.code}>
                                {type.code} - {type.name}
                            </SelectItem>
                        ))}
                    </SelectContent>
                </Select>
            </div>

            {/* Bond Series Search */}
            <div className="space-y-2">
                <label className="text-sm font-medium flex items-center gap-2">
                    <Landmark className="h-4 w-4" /> Wybierz emisję
                </label>
                <Popover open={bondPopoverOpen} onOpenChange={setBondPopoverOpen}>
                    <PopoverTrigger asChild>
                        <Button variant="outline" role="combobox" className="w-full justify-between" disabled={isPending}>
                            {selectedBondSeries
                                ? `${selectedBondSeries.symbol} - ${new Date(selectedBondSeries.maturityDate).toLocaleDateString('pl-PL')}`
                                : 'Wyszukaj serię obligacji...'}
                            <Search className="ml-2 h-4 w-4 shrink-0 opacity-50" />
                        </Button>
                    </PopoverTrigger>
                    <PopoverContent className="w-[450px] p-0" onOpenAutoFocus={(e) => e.preventDefault()}>
                        <div className="p-2 border-b">
                            <Input
                                placeholder="Wpisz symbol (np. EDO0136)..."
                                value={bondSearchQuery}
                                onChange={(e) => setBondSearchQuery(e.target.value)}
                                autoFocus
                            />
                        </div>
                        <Command shouldFilter={false}>
                            <CommandList className="max-h-[300px]">
                                <CommandEmpty>
                                    {bondSeries.length === 0 ? 'Ładowanie serii...' : 'Nie znaleziono serii.'}
                                </CommandEmpty>
                                {filteredBondSeries.length > 0 && (
                                    <CommandGroup>
                                        {filteredBondSeries.slice(0, 50).map((s) => (
                                            <CommandItem key={s.symbol} value={s.symbol} onSelect={() => handleBondSeriesSelect(s, form)}>
                                                <div className="flex flex-col w-full">
                                                    <div className="flex items-center justify-between">
                                                        <span className="font-mono font-semibold">{s.symbol}</span>
                                                        <Badge variant="outline">{s.type}</Badge>
                                                    </div>
                                                    <div className="flex justify-between text-sm text-muted-foreground">
                                                        <span>Zapadalność: {new Date(s.maturityDate).toLocaleDateString('pl-PL')}</span>
                                                        <span>{s.firstYearRate}%</span>
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
                    </div>
                )}
            </div>
        </div>
    )

    const InstrumentSearchField = ({ form }: { form: any }) => (
        <div className="space-y-2">
            <label className="text-sm font-medium">Search Instrument</label>
            <Popover open={popoverOpen} onOpenChange={setPopoverOpen}>
                <PopoverTrigger asChild>
                    <Button variant="outline" role="combobox" className="w-full justify-between" disabled={isPending}>
                        {selectedInstrument ? `${selectedInstrument.ticker} - ${selectedInstrument.name}` : 'Search by ticker...'}
                        {isFetchingPrice ? (
                            <Loader2 className="ml-2 h-4 w-4 shrink-0 animate-spin" />
                        ) : (
                            <Search className="ml-2 h-4 w-4 shrink-0 opacity-50" />
                        )}
                    </Button>
                </PopoverTrigger>
                <PopoverContent className="w-[400px] p-0" onOpenAutoFocus={(e) => e.preventDefault()}>
                    <div className="p-2 border-b">
                        <Input
                            placeholder="Type ticker..."
                            value={searchQuery}
                            onChange={(e) => setSearchQuery(e.target.value)}
                            autoFocus
                        />
                    </div>
                    <Command shouldFilter={false}>
                        <CommandList>
                            <CommandEmpty>{isSearching ? 'Searching...' : searchQuery.length > 0 ? 'No instruments found.' : 'Start typing to search...'}</CommandEmpty>
                            {instruments && instruments.length > 0 && (
                                <CommandGroup>
                                    {instruments.map((i) => (
                                        <CommandItem key={i.id} value={i.ticker} onSelect={() => handleInstrumentSelect(i, form)}>
                                            <div className="flex flex-col">
                                                <div className="flex items-center gap-2">
                                                    <span className="font-semibold">{i.ticker}</span>
                                                    <Badge variant="secondary" className="text-xs">{i.assetType}</Badge>
                                                </div>
                                                <span className="text-sm text-muted-foreground">{i.name}</span>
                                            </div>
                                        </CommandItem>
                                    ))}
                                </CommandGroup>
                            )}
                        </CommandList>
                    </Command>
                </PopoverContent>
            </Popover>
            {isFetchingPrice && (
                <p className="text-sm text-muted-foreground">Fetching current price...</p>
            )}
        </div>
    )

    return (
        <Dialog open={open} onOpenChange={setOpen}>
            <DialogTrigger asChild>{children}</DialogTrigger>
            <DialogContent className="sm:max-w-[600px] max-h-[90vh] overflow-y-auto">
                <DialogHeader className="flex flex-row items-center justify-between">
                    <div>
                        <DialogTitle>Record Transaction</DialogTitle>
                        <DialogDescription>Add a new transaction to your portfolio.</DialogDescription>
                    </div>
                    <div className="flex items-center gap-2 mr-6">
                        <ImportCsvDialog portfolioId={portfolioId}>
                            <Button variant="outline" size="sm" className="flex items-center gap-2">
                                <FileSpreadsheet className="h-4 w-4" />
                                Import MyFund CSV
                            </Button>
                        </ImportCsvDialog>
                    </div>
                </DialogHeader>

                <Tabs value={activeTab} onValueChange={(v) => setActiveTab(v as any)}>
                    <TabsList className="grid w-full grid-cols-4">
                        <TabsTrigger value="buy" className="flex items-center gap-1">
                            <Plus className="h-4 w-4" /> Buy
                        </TabsTrigger>
                        <TabsTrigger value="sell" className="flex items-center gap-1">
                            <Minus className="h-4 w-4" /> Sell
                        </TabsTrigger>
                        <TabsTrigger value="dividend" className="flex items-center gap-1">
                            <Wallet className="h-4 w-4" /> Dividend
                        </TabsTrigger>
                        <TabsTrigger value="interest" className="flex items-center gap-1">
                            <Percent className="h-4 w-4" /> Interest
                        </TabsTrigger>
                    </TabsList>

                    {/* BUY TAB */}
                    <TabsContent value="buy">
                        <Form {...buyForm}>
                            <form onSubmit={buyForm.handleSubmit(onBuySubmit)} className="space-y-4">
                                <AssetTypeField form={buyForm} />
                                {isBond ? (
                                    <BondSeriesField form={buyForm} />
                                ) : (
                                    <InstrumentSearchField form={buyForm} />
                                )}
                                <div className="grid gap-4 sm:grid-cols-2">
                                    <FormField control={buyForm.control} name="quantity" render={({ field }) => (
                                        <FormItem>
                                            <FormLabel>Quantity</FormLabel>
                                            <FormControl>
                                                <Input type="number" step="0.000001" disabled={isPending} {...field}
                                                    onChange={(e) => field.onChange(parseFloat(e.target.value) || 0)} />
                                            </FormControl>
                                            <FormMessage />
                                        </FormItem>
                                    )} />
                                    <FormField control={buyForm.control} name="pricePerUnit" render={({ field }) => (
                                        <FormItem>
                                            <FormLabel>Price per Unit</FormLabel>
                                            <FormControl>
                                                <Input type="number" step="0.01" disabled={isPending} {...field}
                                                    onChange={(e) => field.onChange(parseFloat(e.target.value) || 0)} />
                                            </FormControl>
                                            <FormMessage />
                                        </FormItem>
                                    )} />
                                </div>
                                <div className="grid gap-4 sm:grid-cols-3">
                                    <FormField control={buyForm.control} name="currency" render={({ field }) => (
                                        <FormItem>
                                            <FormLabel>Currency</FormLabel>
                                            <Select onValueChange={field.onChange} defaultValue={field.value}>
                                                <FormControl><SelectTrigger><SelectValue /></SelectTrigger></FormControl>
                                                <SelectContent>
                                                    {CURRENCIES.map((c) => <SelectItem key={c} value={c}>{c}</SelectItem>)}
                                                </SelectContent>
                                            </Select>
                                        </FormItem>
                                    )} />
                                    <FormField control={buyForm.control} name="fee" render={({ field }) => (
                                        <FormItem>
                                            <FormLabel>Fee</FormLabel>
                                            <FormControl>
                                                <Input type="number" step="0.01" disabled={isPending} {...field}
                                                    onChange={(e) => field.onChange(parseFloat(e.target.value) || 0)} />
                                            </FormControl>
                                        </FormItem>
                                    )} />
                                    <FormField control={buyForm.control} name="transactionDate" render={({ field }) => (
                                        <FormItem>
                                            <FormLabel>Date</FormLabel>
                                            <FormControl><Input type="date" disabled={isPending} {...field} /></FormControl>
                                        </FormItem>
                                    )} />
                                </div>
                                <FormField control={buyForm.control} name="notes" render={({ field }) => (
                                    <FormItem>
                                        <FormLabel>Notes (optional)</FormLabel>
                                        <FormControl><Textarea disabled={isPending} {...field} /></FormControl>
                                    </FormItem>
                                )} />
                                {/* Total Cost Display */}
                                {(() => {
                                    const qty = buyForm.watch('quantity') || 0
                                    const price = buyForm.watch('pricePerUnit') || 0
                                    const fee = buyForm.watch('fee') || 0
                                    const currency = buyForm.watch('currency') || 'USD'
                                    const total = qty * price + fee
                                    return total > 0 ? (
                                        <div className="rounded-lg border p-3 bg-muted/50">
                                            <div className="flex justify-between items-center">
                                                <span className="text-sm text-muted-foreground">Total Cost:</span>
                                                <span className="text-lg font-bold">{total.toFixed(2)} {currency}</span>
                                            </div>
                                            <div className="text-xs text-muted-foreground mt-1">
                                                ({qty} × {price.toFixed(2)}) + {fee.toFixed(2)} fee
                                            </div>
                                        </div>
                                    ) : null
                                })()}
                                <div className="flex justify-end gap-3 pt-4">
                                    <Button type="button" variant="outline" onClick={() => setOpen(false)}>Cancel</Button>
                                    <Button type="submit" disabled={isPending}>
                                        {recordBuy.isPending ? 'Recording...' : 'Record Buy'}
                                    </Button>
                                </div>
                            </form>
                        </Form>
                    </TabsContent>

                    {/* SELL TAB */}
                    <TabsContent value="sell">
                        <Form {...sellForm}>
                            <form onSubmit={sellForm.handleSubmit(onSellSubmit)} className="space-y-4">
                                <InstrumentSearchField form={sellForm} />
                                <div className="grid gap-4 sm:grid-cols-2">
                                    <FormField control={sellForm.control} name="quantity" render={({ field }) => (
                                        <FormItem>
                                            <FormLabel>Quantity</FormLabel>
                                            <FormControl>
                                                <Input type="number" step="0.000001" disabled={isPending} {...field}
                                                    onChange={(e) => field.onChange(parseFloat(e.target.value) || 0)} />
                                            </FormControl>
                                            <FormMessage />
                                        </FormItem>
                                    )} />
                                    <FormField control={sellForm.control} name="salePrice" render={({ field }) => (
                                        <FormItem>
                                            <FormLabel>Sale Price</FormLabel>
                                            <FormControl>
                                                <Input type="number" step="0.01" disabled={isPending} {...field}
                                                    onChange={(e) => field.onChange(parseFloat(e.target.value) || 0)} />
                                            </FormControl>
                                            <FormMessage />
                                        </FormItem>
                                    )} />
                                </div>
                                <div className="grid gap-4 sm:grid-cols-3">
                                    <FormField control={sellForm.control} name="currency" render={({ field }) => (
                                        <FormItem>
                                            <FormLabel>Currency</FormLabel>
                                            <Select onValueChange={field.onChange} defaultValue={field.value}>
                                                <FormControl><SelectTrigger><SelectValue /></SelectTrigger></FormControl>
                                                <SelectContent>
                                                    {CURRENCIES.map((c) => <SelectItem key={c} value={c}>{c}</SelectItem>)}
                                                </SelectContent>
                                            </Select>
                                        </FormItem>
                                    )} />
                                    <FormField control={sellForm.control} name="fee" render={({ field }) => (
                                        <FormItem>
                                            <FormLabel>Fee</FormLabel>
                                            <FormControl>
                                                <Input type="number" step="0.01" disabled={isPending} {...field}
                                                    onChange={(e) => field.onChange(parseFloat(e.target.value) || 0)} />
                                            </FormControl>
                                        </FormItem>
                                    )} />
                                    <FormField control={sellForm.control} name="transactionDate" render={({ field }) => (
                                        <FormItem>
                                            <FormLabel>Date</FormLabel>
                                            <FormControl><Input type="date" disabled={isPending} {...field} /></FormControl>
                                        </FormItem>
                                    )} />
                                </div>
                                {/* Total Proceeds Display */}
                                {(() => {
                                    const qty = sellForm.watch('quantity') || 0
                                    const price = sellForm.watch('salePrice') || 0
                                    const fee = sellForm.watch('fee') || 0
                                    const currency = sellForm.watch('currency') || 'USD'
                                    const total = qty * price - fee
                                    return total > 0 ? (
                                        <div className="rounded-lg border p-3 bg-muted/50">
                                            <div className="flex justify-between items-center">
                                                <span className="text-sm text-muted-foreground">Net Proceeds:</span>
                                                <span className="text-lg font-bold">{total.toFixed(2)} {currency}</span>
                                            </div>
                                            <div className="text-xs text-muted-foreground mt-1">
                                                ({qty} × {price.toFixed(2)}) - {fee.toFixed(2)} fee
                                            </div>
                                        </div>
                                    ) : null
                                })()}
                                <div className="flex justify-end gap-3 pt-4">
                                    <Button type="button" variant="outline" onClick={() => setOpen(false)}>Cancel</Button>
                                    <Button type="submit" disabled={isPending} variant="destructive">
                                        {recordSell.isPending ? 'Recording...' : 'Record Sell'}
                                    </Button>
                                </div>
                            </form>
                        </Form>
                    </TabsContent>

                    {/* DIVIDEND TAB */}
                    <TabsContent value="dividend">
                        <Form {...dividendForm}>
                            <form onSubmit={dividendForm.handleSubmit(onDividendSubmit)} className="space-y-4">
                                <InstrumentSearchField form={dividendForm} />
                                <div className="grid gap-4 sm:grid-cols-2">
                                    <FormField control={dividendForm.control} name="grossAmount" render={({ field }) => (
                                        <FormItem>
                                            <FormLabel>Gross Amount</FormLabel>
                                            <FormControl>
                                                <Input type="number" step="0.01" disabled={isPending} {...field}
                                                    onChange={(e) => field.onChange(parseFloat(e.target.value) || 0)} />
                                            </FormControl>
                                            <FormMessage />
                                        </FormItem>
                                    )} />
                                    <FormField control={dividendForm.control} name="taxRate" render={({ field }) => (
                                        <FormItem>
                                            <FormLabel>Tax Rate (%)</FormLabel>
                                            <FormControl>
                                                <Input type="number" step="0.1" disabled={isPending} {...field}
                                                    onChange={(e) => field.onChange(parseFloat(e.target.value) || 0)} />
                                            </FormControl>
                                            <FormDescription>Default: 19% (Polish tax rate)</FormDescription>
                                        </FormItem>
                                    )} />
                                </div>
                                <div className="grid gap-4 sm:grid-cols-2">
                                    <FormField control={dividendForm.control} name="currency" render={({ field }) => (
                                        <FormItem>
                                            <FormLabel>Currency</FormLabel>
                                            <Select onValueChange={field.onChange} defaultValue={field.value}>
                                                <FormControl><SelectTrigger><SelectValue /></SelectTrigger></FormControl>
                                                <SelectContent>
                                                    {CURRENCIES.map((c) => <SelectItem key={c} value={c}>{c}</SelectItem>)}
                                                </SelectContent>
                                            </Select>
                                        </FormItem>
                                    )} />
                                    <FormField control={dividendForm.control} name="paymentDate" render={({ field }) => (
                                        <FormItem>
                                            <FormLabel>Payment Date</FormLabel>
                                            <FormControl><Input type="date" disabled={isPending} {...field} /></FormControl>
                                        </FormItem>
                                    )} />
                                </div>
                                <FormField control={dividendForm.control} name="notes" render={({ field }) => (
                                    <FormItem>
                                        <FormLabel>Notes (optional)</FormLabel>
                                        <FormControl><Textarea disabled={isPending} {...field} /></FormControl>
                                    </FormItem>
                                )} />
                                <div className="flex justify-end gap-3 pt-4">
                                    <Button type="button" variant="outline" onClick={() => setOpen(false)}>Cancel</Button>
                                    <Button type="submit" disabled={isPending} className="bg-green-600 hover:bg-green-700">
                                        {recordDividend.isPending ? 'Recording...' : 'Record Dividend'}
                                    </Button>
                                </div>
                            </form>
                        </Form>
                    </TabsContent>

                    {/* INTEREST TAB */}
                    <TabsContent value="interest">
                        <Form {...interestForm}>
                            <form onSubmit={interestForm.handleSubmit(onInterestSubmit)} className="space-y-4">
                                <InstrumentSearchField form={interestForm} />
                                <div className="grid gap-4 sm:grid-cols-2">
                                    <FormField control={interestForm.control} name="grossAmount" render={({ field }) => (
                                        <FormItem>
                                            <FormLabel>Gross Amount</FormLabel>
                                            <FormControl>
                                                <Input type="number" step="0.01" disabled={isPending} {...field}
                                                    onChange={(e) => field.onChange(parseFloat(e.target.value) || 0)} />
                                            </FormControl>
                                            <FormMessage />
                                        </FormItem>
                                    )} />
                                    <FormField control={interestForm.control} name="taxRate" render={({ field }) => (
                                        <FormItem>
                                            <FormLabel>Tax Rate (%)</FormLabel>
                                            <FormControl>
                                                <Input type="number" step="0.1" disabled={isPending} {...field}
                                                    onChange={(e) => field.onChange(parseFloat(e.target.value) || 0)} />
                                            </FormControl>
                                            <FormDescription>Default: 19% (Polish tax rate)</FormDescription>
                                        </FormItem>
                                    )} />
                                </div>
                                <div className="grid gap-4 sm:grid-cols-2">
                                    <FormField control={interestForm.control} name="currency" render={({ field }) => (
                                        <FormItem>
                                            <FormLabel>Currency</FormLabel>
                                            <Select onValueChange={field.onChange} defaultValue={field.value}>
                                                <FormControl><SelectTrigger><SelectValue /></SelectTrigger></FormControl>
                                                <SelectContent>
                                                    {CURRENCIES.map((c) => <SelectItem key={c} value={c}>{c}</SelectItem>)}
                                                </SelectContent>
                                            </Select>
                                        </FormItem>
                                    )} />
                                    <FormField control={interestForm.control} name="paymentDate" render={({ field }) => (
                                        <FormItem>
                                            <FormLabel>Payment Date</FormLabel>
                                            <FormControl><Input type="date" disabled={isPending} {...field} /></FormControl>
                                        </FormItem>
                                    )} />
                                </div>
                                <FormField control={interestForm.control} name="notes" render={({ field }) => (
                                    <FormItem>
                                        <FormLabel>Notes (optional)</FormLabel>
                                        <FormControl><Textarea disabled={isPending} {...field} /></FormControl>
                                    </FormItem>
                                )} />
                                <div className="flex justify-end gap-3 pt-4">
                                    <Button type="button" variant="outline" onClick={() => setOpen(false)}>Cancel</Button>
                                    <Button type="submit" disabled={isPending} className="bg-blue-600 hover:bg-blue-700">
                                        {recordInterest.isPending ? 'Recording...' : 'Record Interest'}
                                    </Button>
                                </div>
                            </form>
                        </Form>
                    </TabsContent>
                </Tabs>
            </DialogContent>
        </Dialog>
    )
}
