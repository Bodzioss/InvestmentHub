'use client'

import { useState, useEffect } from 'react'
import { Search, Filter, TrendingUp, Building2, PieChart } from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from '@/components/ui/select'
import { useRouter } from 'next/navigation'

const API_BASE = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5146'

interface EtfDetails {
    region?: string
    theme?: string
    manager?: string
    distributionType?: string
    domicile?: string
    replication?: string
    annualFeePercent?: number
    assetsMillionsEur?: number
    currency?: string
}

interface BondDetails {
    bondType?: string
    nominalValue?: number
    interestRate?: number
    maturityDate?: string
    issuer?: string
}

interface Instrument {
    id: string
    ticker: string
    name: string
    isin: string
    exchange: string
    assetType: 'ETF' | 'Bond' | 'Stock'
    etfDetails?: EtfDetails
    bondDetails?: BondDetails
}

interface InstrumentsResponse {
    instruments: Instrument[]
    totalCount: number
    page: number
    pageSize: number
}

export default function InstrumentsPage() {
    const router = useRouter()
    const [instruments, setInstruments] = useState<Instrument[]>([])
    const [loading, setLoading] = useState(true)
    const [error, setError] = useState<string | null>(null)
    const [search, setSearch] = useState('')
    const [typeFilter, setTypeFilter] = useState<string>('all')
    const [stats, setStats] = useState({ total: 0, etfs: 0, bonds: 0, stocks: 0 })
    const [page, setPage] = useState(1)
    const [pageSize] = useState(50) // Reduce fetching size for better UX
    const [totalPages, setTotalPages] = useState(0)

    useEffect(() => {
        fetchInstruments()
    }, [typeFilter, page]) // Re-fetch on page change

    const fetchInstruments = async () => {
        setLoading(true)
        setError(null)
        try {
            const params = new URLSearchParams()
            if (typeFilter && typeFilter !== 'all') {
                params.append('type', typeFilter)
            }
            if (search) {
                params.append('search', search)
            }
            params.append('page', page.toString())
            params.append('pageSize', pageSize.toString())

            const response = await fetch(`${API_BASE}/api/instruments?${params}`)
            if (!response.ok) throw new Error('Failed to fetch instruments')

            const data: InstrumentsResponse = await response.json()
            setInstruments(data.instruments || [])
            setTotalPages(Math.ceil((data.totalCount || 0) / pageSize))

            // Calculate stats
            // Note: Stats here might be limited to current page if backend doesn't return global aggregates
            // Ideally backend should return global stats, but for now we trust totalCount
            setStats({
                total: data.totalCount || 0,
                etfs: stats.etfs, // Keep previous or fetch separately if needed
                bonds: stats.bonds,
                stocks: stats.stocks,
            })
        } catch (err) {
            setError(err instanceof Error ? err.message : 'Unknown error')
        } finally {
            setLoading(false)
        }
    }

    const handleSearch = () => {
        setPage(1) // Reset to first page on search
        fetchInstruments()
    }

    const filteredInstruments = instruments.filter(inst => {
        if (!search) return true
        const searchLower = search.toLowerCase()
        return (
            inst.ticker.toLowerCase().includes(searchLower) ||
            inst.name.toLowerCase().includes(searchLower) ||
            inst.isin.toLowerCase().includes(searchLower)
        )
    })

    const getAssetTypeBadgeColor = (type: string) => {
        switch (type) {
            case 'ETF': return 'bg-blue-500/20 text-blue-400 border-blue-500/50'
            case 'Bond': return 'bg-green-500/20 text-green-400 border-green-500/50'
            case 'Stock': return 'bg-purple-500/20 text-purple-400 border-purple-500/50'
            default: return 'bg-gray-500/20 text-gray-400 border-gray-500/50'
        }
    }

    const getAssetTypeIcon = (type: string) => {
        switch (type) {
            case 'ETF': return <PieChart className="h-4 w-4" />
            case 'Bond': return <Building2 className="h-4 w-4" />
            case 'Stock': return <TrendingUp className="h-4 w-4" />
            default: return null
        }
    }

    return (
        <div className="min-h-screen bg-background">
            {/* Header */}
            <header className="border-b bg-card">
                <div className="container mx-auto flex h-16 items-center justify-between px-4">
                    <div>
                        <h1 className="text-xl font-bold cursor-pointer" onClick={() => router.push('/')}>
                            InvestmentHub
                        </h1>
                    </div>
                    <Button variant="outline" onClick={() => router.push('/')}>
                        Back to Dashboard
                    </Button>
                </div>
            </header>

            <main className="container mx-auto p-6">
                {/* Page Header */}
                <div className="mb-6">
                    <h2 className="text-3xl font-bold flex items-center gap-2">
                        <Filter className="h-8 w-8 text-primary" />
                        Available Instruments
                    </h2>
                    <p className="text-muted-foreground mt-1">
                        Browse all available financial instruments for trading
                    </p>
                </div>

                {/* Stats Cards */}
                <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
                    <Card>
                        <CardContent className="pt-4">
                            <div className="flex items-center justify-between">
                                <div>
                                    <p className="text-sm text-muted-foreground">Total</p>
                                    <p className="text-2xl font-bold">{stats.total}</p>
                                </div>
                                <Filter className="h-8 w-8 text-muted-foreground/50" />
                            </div>
                        </CardContent>
                    </Card>
                    <Card>
                        <CardContent className="pt-4">
                            <div className="flex items-center justify-between">
                                <div>
                                    <p className="text-sm text-muted-foreground">ETFs</p>
                                    <p className="text-2xl font-bold text-blue-500">{stats.etfs}</p>
                                </div>
                                <PieChart className="h-8 w-8 text-blue-500/50" />
                            </div>
                        </CardContent>
                    </Card>
                    <Card>
                        <CardContent className="pt-4">
                            <div className="flex items-center justify-between">
                                <div>
                                    <p className="text-sm text-muted-foreground">Bonds</p>
                                    <p className="text-2xl font-bold text-green-500">{stats.bonds}</p>
                                </div>
                                <Building2 className="h-8 w-8 text-green-500/50" />
                            </div>
                        </CardContent>
                    </Card>
                    <Card>
                        <CardContent className="pt-4">
                            <div className="flex items-center justify-between">
                                <div>
                                    <p className="text-sm text-muted-foreground">Stocks</p>
                                    <p className="text-2xl font-bold text-purple-500">{stats.stocks}</p>
                                </div>
                                <TrendingUp className="h-8 w-8 text-purple-500/50" />
                            </div>
                        </CardContent>
                    </Card>
                </div>

                {/* Search and Filters */}
                <Card className="mb-6">
                    <CardContent className="pt-4">
                        <div className="flex flex-col md:flex-row gap-4">
                            <div className="flex-1 flex gap-2">
                                <div className="relative flex-1">
                                    <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                                    <Input
                                        placeholder="Search by ticker, name, or ISIN..."
                                        value={search}
                                        onChange={(e) => setSearch(e.target.value)}
                                        onKeyDown={(e) => e.key === 'Enter' && handleSearch()}
                                        className="pl-10"
                                    />
                                </div>
                                <Button onClick={handleSearch}>Search</Button>
                            </div>
                            <Select value={typeFilter} onValueChange={setTypeFilter}>
                                <SelectTrigger className="w-[180px]">
                                    <SelectValue placeholder="Filter by type" />
                                </SelectTrigger>
                                <SelectContent>
                                    <SelectItem value="all">All Types</SelectItem>
                                    <SelectItem value="ETF">ETF</SelectItem>
                                    <SelectItem value="Bond">Bond</SelectItem>
                                    <SelectItem value="Stock">Stock</SelectItem>
                                </SelectContent>
                            </Select>
                        </div>
                    </CardContent>
                </Card>

                {/* Loading State */}
                {loading && (
                    <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
                        {[1, 2, 3, 4, 5, 6].map((i) => (
                            <Card key={i} className="animate-pulse">
                                <CardHeader className="space-y-2">
                                    <div className="h-6 w-3/4 rounded bg-muted"></div>
                                    <div className="h-4 w-1/2 rounded bg-muted"></div>
                                </CardHeader>
                                <CardContent>
                                    <div className="h-20 rounded bg-muted"></div>
                                </CardContent>
                            </Card>
                        ))}
                    </div>
                )}

                {/* Error State */}
                {error && (
                    <Card className="border-destructive">
                        <CardHeader>
                            <CardTitle className="text-destructive">Error Loading Instruments</CardTitle>
                            <p className="text-muted-foreground">{error}</p>
                        </CardHeader>
                    </Card>
                )}

                {/* Empty State */}
                {!loading && !error && filteredInstruments.length === 0 && (
                    <Card className="border-dashed">
                        <CardHeader className="text-center">
                            <CardTitle>No Instruments Found</CardTitle>
                            <p className="text-muted-foreground">
                                {search ? 'Try adjusting your search criteria' : 'No instruments available'}
                            </p>
                        </CardHeader>
                    </Card>
                )}

                {/* Instruments Grid */}
                {!loading && !error && filteredInstruments.length > 0 && (
                    <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
                        {filteredInstruments.map((instrument) => (
                            <InstrumentCard key={instrument.id} instrument={instrument} />
                        ))}
                    </div>
                )}

                {/* Pagination Controls */}
                {!loading && !error && (
                    <div className="flex justify-center items-center gap-4 mt-8">
                        <Button
                            variant="outline"
                            onClick={() => setPage(p => Math.max(1, p - 1))}
                            disabled={page === 1}
                        >
                            Previous
                        </Button>
                        <span className="text-sm text-muted-foreground">
                            Page {page} of {totalPages || 1}
                        </span>
                        <Button
                            variant="outline"
                            onClick={() => setPage(p => Math.min(totalPages, p + 1))}
                            disabled={page >= totalPages}
                        >
                            Next
                        </Button>
                    </div>
                )}
            </main>
        </div>
    )
}

function InstrumentCard({ instrument }: { instrument: Instrument }) {
    const getAssetTypeBadgeColor = (type: string) => {
        switch (type) {
            case 'ETF': return 'bg-blue-500/20 text-blue-400 border-blue-500/50'
            case 'Bond': return 'bg-green-500/20 text-green-400 border-green-500/50'
            case 'Stock': return 'bg-purple-500/20 text-purple-400 border-purple-500/50'
            default: return 'bg-gray-500/20 text-gray-400 border-gray-500/50'
        }
    }

    return (
        <Card className="hover:shadow-lg transition-shadow">
            <CardHeader className="pb-2">
                <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2">
                        <span className="text-lg font-bold">{instrument.ticker}</span>
                        <Badge variant="outline" className={getAssetTypeBadgeColor(instrument.assetType)}>
                            {instrument.assetType}
                        </Badge>
                    </div>
                    <span className="text-sm text-muted-foreground">{instrument.exchange}</span>
                </div>
                <p className="text-sm text-muted-foreground line-clamp-1">{instrument.name}</p>
            </CardHeader>
            <CardContent className="pt-0">
                <div className="text-xs text-muted-foreground mb-3">ISIN: {instrument.isin}</div>

                {/* Type-specific details */}
                {instrument.assetType === 'ETF' && instrument.etfDetails && (
                    <EtfDetailsSection details={instrument.etfDetails} />
                )}

                {instrument.assetType === 'Bond' && instrument.bondDetails && (
                    <BondDetailsSection details={instrument.bondDetails} />
                )}

                {instrument.assetType === 'Stock' && (
                    <StockDetailsSection />
                )}
            </CardContent>
        </Card>
    )
}

function EtfDetailsSection({ details }: { details: EtfDetails }) {
    return (
        <div className="space-y-1.5 text-sm">
            {details.manager && (
                <div className="flex justify-between">
                    <span className="text-muted-foreground">Manager:</span>
                    <span>{details.manager}</span>
                </div>
            )}
            {details.region && (
                <div className="flex justify-between">
                    <span className="text-muted-foreground">Region:</span>
                    <span>{details.region}</span>
                </div>
            )}
            {details.annualFeePercent !== undefined && (
                <div className="flex justify-between">
                    <span className="text-muted-foreground">Expense Ratio:</span>
                    <span>{details.annualFeePercent.toFixed(2)}%</span>
                </div>
            )}
            {details.distributionType && (
                <div className="flex justify-between">
                    <span className="text-muted-foreground">Type:</span>
                    <span>{details.distributionType}</span>
                </div>
            )}
            {details.replication && (
                <div className="flex justify-between">
                    <span className="text-muted-foreground">Replication:</span>
                    <span>{details.replication}</span>
                </div>
            )}
            {details.assetsMillionsEur !== undefined && details.assetsMillionsEur > 0 && (
                <div className="flex justify-between">
                    <span className="text-muted-foreground">Assets:</span>
                    <span>{details.assetsMillionsEur.toFixed(0)}M EUR</span>
                </div>
            )}
        </div>
    )
}

function BondDetailsSection({ details }: { details: BondDetails }) {
    return (
        <div className="space-y-1.5 text-sm">
            {details.issuer && (
                <div className="flex justify-between">
                    <span className="text-muted-foreground">Issuer:</span>
                    <span>{details.issuer}</span>
                </div>
            )}
            {details.bondType && (
                <div className="flex justify-between">
                    <span className="text-muted-foreground">Type:</span>
                    <span>{details.bondType}</span>
                </div>
            )}
            {details.interestRate !== undefined && (
                <div className="flex justify-between">
                    <span className="text-muted-foreground">Interest Rate:</span>
                    <span className="text-green-500">{details.interestRate.toFixed(2)}%</span>
                </div>
            )}
            {details.maturityDate && (
                <div className="flex justify-between">
                    <span className="text-muted-foreground">Maturity:</span>
                    <span>{new Date(details.maturityDate).toLocaleDateString()}</span>
                </div>
            )}
            {details.nominalValue !== undefined && (
                <div className="flex justify-between">
                    <span className="text-muted-foreground">Nominal:</span>
                    <span>{details.nominalValue} PLN</span>
                </div>
            )}
        </div>
    )
}

function StockDetailsSection() {
    return (
        <div className="space-y-1.5 text-sm">
            <div className="flex justify-between">
                <span className="text-muted-foreground">Type:</span>
                <span>Common Stock</span>
            </div>
            <p className="text-xs text-muted-foreground italic">
                Detailed stock information coming soon...
            </p>
        </div>
    )
}
