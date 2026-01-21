'use client'

import { useState, useEffect, useMemo, useCallback } from 'react'
import { Upload, Loader2, Search, Check, ChevronsUpDown } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Command, CommandEmpty, CommandGroup, CommandInput, CommandItem, CommandList } from '@/components/ui/command'
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover'
import { toast } from 'sonner'
import { cn } from '@/lib/utils'

interface Instrument {
    id: string
    ticker: string
    name: string
    exchange: string
}

const reportTypes = [
    { value: 'Annual10K', label: '10-K (Annual)' },
    { value: 'Quarterly10Q', label: '10-Q (Quarterly)' },
    { value: 'AnnualReport', label: 'Annual Report' },
    { value: 'QuarterlyReport', label: 'Quarterly Report' },
    { value: 'Earnings', label: 'Earnings Report' },
    { value: 'Other', label: 'Other' },
]

const API_BASE = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5146'

export function ReportUploadDialog({ onUploadComplete }: { onUploadComplete?: () => void }) {
    const [open, setOpen] = useState(false)
    const [comboOpen, setComboOpen] = useState(false)
    const [instruments, setInstruments] = useState<Instrument[]>([])
    const [selectedInstrument, setSelectedInstrument] = useState<Instrument | null>(null)
    const [searchQuery, setSearchQuery] = useState('')
    const [year, setYear] = useState(new Date().getFullYear())
    const [quarter, setQuarter] = useState<string>('annual')
    const [reportType, setReportType] = useState('Annual10K')
    const [file, setFile] = useState<File | null>(null)
    const [uploading, setUploading] = useState(false)
    const [exists, setExists] = useState(false)
    const [loading, setLoading] = useState(false)

    // Debounced search for instruments
    useEffect(() => {
        if (!open) return

        const timer = setTimeout(() => {
            setLoading(true)
            const url = searchQuery
                ? `${API_BASE}/api/ai/instruments?search=${encodeURIComponent(searchQuery)}&limit=20`
                : `${API_BASE}/api/ai/instruments?limit=20`

            fetch(url)
                .then(r => r.json())
                .then(data => {
                    setInstruments(Array.isArray(data) ? data : [])
                    setLoading(false)
                })
                .catch(err => {
                    console.error('Failed to load instruments', err)
                    setLoading(false)
                })
        }, 300) // 300ms debounce

        return () => clearTimeout(timer)
    }, [open, searchQuery])

    // Check if report exists when selection changes
    useEffect(() => {
        if (!selectedInstrument || !year || !reportType) {
            setExists(false)
            return
        }

        const params = new URLSearchParams({
            instrumentId: selectedInstrument.id,
            year: year.toString(),
            reportType
        })
        if (quarter && quarter !== 'annual') params.set('quarter', quarter)

        fetch(`${API_BASE}/api/ai/reports/exists?${params}`)
            .then(r => r.json())
            .then(data => setExists(data.exists))
            .catch(() => setExists(false))
    }, [selectedInstrument, year, quarter, reportType])

    const handleUpload = async () => {
        if (!file || !selectedInstrument) return
        setUploading(true)

        try {
            const formData = new FormData()
            formData.append('file', file)

            const params = new URLSearchParams({
                instrumentId: selectedInstrument.id,
                year: year.toString(),
                reportType,
                userId: 'demo-user'
            })
            if (quarter && quarter !== 'annual') params.set('quarter', quarter)

            const response = await fetch(`${API_BASE}/api/ai/reports/upload?${params}`, {
                method: 'POST',
                body: formData
            })

            const data = await response.json()

            if (response.ok) {
                toast.success(data.message || 'Report is being processed')
                setOpen(false)
                setFile(null)
                setSelectedInstrument(null)
                setSearchQuery('')
                onUploadComplete?.()
            } else {
                toast.error(data.error || 'Failed to upload report')
            }
        } catch (error) {
            toast.error('Failed to upload report')
        } finally {
            setUploading(false)
        }
    }

    return (
        <Dialog open={open} onOpenChange={setOpen}>
            <DialogTrigger asChild>
                <Button>
                    <Upload className="h-4 w-4 mr-2" />
                    Upload Report
                </Button>
            </DialogTrigger>
            <DialogContent className="sm:max-w-[500px]">
                <DialogHeader>
                    <DialogTitle>Upload Financial Report</DialogTitle>
                </DialogHeader>
                <div className="space-y-4 py-4">
                    <div className="space-y-2">
                        <Label>Instrument / Company</Label>
                        <Popover open={comboOpen} onOpenChange={setComboOpen}>
                            <PopoverTrigger asChild>
                                <Button
                                    variant="outline"
                                    role="combobox"
                                    aria-expanded={comboOpen}
                                    className="w-full justify-between"
                                >
                                    {selectedInstrument
                                        ? `${selectedInstrument.ticker} - ${selectedInstrument.name}`
                                        : "Search for instrument..."}
                                    <ChevronsUpDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
                                </Button>
                            </PopoverTrigger>
                            <PopoverContent className="w-[400px] p-0">
                                <Command shouldFilter={false}>
                                    <CommandInput
                                        placeholder="Type ticker or company name..."
                                        value={searchQuery}
                                        onValueChange={setSearchQuery}
                                    />
                                    <CommandList>
                                        {loading && (
                                            <div className="flex items-center justify-center p-4">
                                                <Loader2 className="h-4 w-4 animate-spin mr-2" />
                                                Searching...
                                            </div>
                                        )}
                                        {!loading && instruments.length === 0 && (
                                            <CommandEmpty>
                                                {searchQuery ? "No instruments found." : "Start typing to search..."}
                                            </CommandEmpty>
                                        )}
                                        {!loading && instruments.length > 0 && (
                                            <CommandGroup>
                                                {instruments.map((inst) => (
                                                    <CommandItem
                                                        key={inst.id}
                                                        value={inst.id}
                                                        onSelect={() => {
                                                            setSelectedInstrument(inst)
                                                            setComboOpen(false)
                                                        }}
                                                    >
                                                        <Check
                                                            className={cn(
                                                                "mr-2 h-4 w-4",
                                                                selectedInstrument?.id === inst.id ? "opacity-100" : "opacity-0"
                                                            )}
                                                        />
                                                        <div className="flex flex-col">
                                                            <span className="font-medium">{inst.ticker}</span>
                                                            <span className="text-xs text-muted-foreground">{inst.name}</span>
                                                        </div>
                                                    </CommandItem>
                                                ))}
                                            </CommandGroup>
                                        )}
                                    </CommandList>
                                </Command>
                            </PopoverContent>
                        </Popover>
                        {selectedInstrument && (
                            <p className="text-xs text-muted-foreground">
                                Exchange: {selectedInstrument.exchange}
                            </p>
                        )}
                    </div>

                    <div className="grid grid-cols-2 gap-4">
                        <div className="space-y-2">
                            <Label>Year</Label>
                            <Input
                                type="number"
                                min={1990}
                                max={new Date().getFullYear()}
                                value={year}
                                onChange={e => setYear(parseInt(e.target.value) || new Date().getFullYear())}
                            />
                        </div>
                        <div className="space-y-2">
                            <Label>Quarter (optional)</Label>
                            <Select value={quarter} onValueChange={setQuarter}>
                                <SelectTrigger>
                                    <SelectValue placeholder="Annual" />
                                </SelectTrigger>
                                <SelectContent>
                                    <SelectItem value="annual">Annual</SelectItem>
                                    <SelectItem value="1">Q1</SelectItem>
                                    <SelectItem value="2">Q2</SelectItem>
                                    <SelectItem value="3">Q3</SelectItem>
                                    <SelectItem value="4">Q4</SelectItem>
                                </SelectContent>
                            </Select>
                        </div>
                    </div>

                    <div className="space-y-2">
                        <Label>Report Type</Label>
                        <Select value={reportType} onValueChange={setReportType}>
                            <SelectTrigger>
                                <SelectValue />
                            </SelectTrigger>
                            <SelectContent>
                                {reportTypes.map(t => (
                                    <SelectItem key={t.value} value={t.value}>
                                        {t.label}
                                    </SelectItem>
                                ))}
                            </SelectContent>
                        </Select>
                    </div>

                    {exists && (
                        <Alert variant="destructive">
                            <AlertDescription>
                                ⚠️ This report already exists in the library!
                            </AlertDescription>
                        </Alert>
                    )}

                    <div className="space-y-2">
                        <Label>PDF File</Label>
                        <Input
                            type="file"
                            accept=".pdf"
                            onChange={e => setFile(e.target.files?.[0] || null)}
                        />
                        {file && (
                            <p className="text-xs text-muted-foreground">
                                {file.name} ({(file.size / 1024 / 1024).toFixed(2)} MB)
                            </p>
                        )}
                    </div>

                    <Button
                        onClick={handleUpload}
                        disabled={uploading || !file || !selectedInstrument || exists}
                        className="w-full"
                    >
                        {uploading ? (
                            <>
                                <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                                Uploading...
                            </>
                        ) : (
                            <>
                                <Upload className="h-4 w-4 mr-2" />
                                Upload Report
                            </>
                        )}
                    </Button>
                </div>
            </DialogContent>
        </Dialog>
    )
}
