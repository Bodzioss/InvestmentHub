'use client'

import { useState, useEffect } from 'react'
import { FileText, RefreshCw, Check } from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Checkbox } from '@/components/ui/checkbox'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'

interface Report {
    id: string
    instrumentId: string
    ticker: string
    instrumentName: string
    year: number
    quarter: number | null
    reportType: string
    fileName: string
    status: string
    chunkCount: number
    createdAt: string
}

const API_BASE = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5146'

interface ReportLibraryProps {
    onSelectionChange: (ids: string[]) => void
    refreshTrigger?: number
}

export function ReportLibrary({ onSelectionChange, refreshTrigger }: ReportLibraryProps) {
    const [reports, setReports] = useState<Report[]>([])
    const [selected, setSelected] = useState<Set<string>>(new Set())
    const [loading, setLoading] = useState(true)

    const fetchReports = async () => {
        setLoading(true)
        try {
            const response = await fetch(`${API_BASE}/api/ai/reports`)
            const data = await response.json()
            // Ensure we always have an array
            setReports(Array.isArray(data) ? data : [])
        } catch (error) {
            console.error('Failed to fetch reports', error)
            setReports([])
        } finally {
            setLoading(false)
        }
    }

    useEffect(() => {
        fetchReports()
    }, [refreshTrigger])

    const toggleReport = (id: string) => {
        const report = reports.find(r => r.id === id)
        if (report?.status !== 'Ready') return // Can only select ready reports

        const newSelected = new Set(selected)
        if (newSelected.has(id)) {
            newSelected.delete(id)
        } else {
            newSelected.add(id)
        }
        setSelected(newSelected)
        onSelectionChange(Array.from(newSelected))
    }

    const selectAll = () => {
        const readyReports = reports.filter(r => r.status === 'Ready')
        const newSelected = new Set(readyReports.map(r => r.id))
        setSelected(newSelected)
        onSelectionChange(Array.from(newSelected))
    }

    const deselectAll = () => {
        setSelected(new Set())
        onSelectionChange([])
    }

    const getStatusBadge = (status: string) => {
        switch (status) {
            case 'Ready':
                return <Badge variant="default" className="bg-green-500">Ready</Badge>
            case 'Processing':
                return <Badge variant="secondary">Processing...</Badge>
            case 'Failed':
                return <Badge variant="destructive">Failed</Badge>
            default:
                return <Badge variant="outline">{status}</Badge>
        }
    }

    const formatReportType = (type: string) => {
        switch (type) {
            case 'Annual10K': return '10-K'
            case 'Quarterly10Q': return '10-Q'
            case 'AnnualReport': return 'Annual'
            case 'QuarterlyReport': return 'Quarterly'
            case 'Earnings': return 'Earnings'
            default: return type
        }
    }

    if (loading) {
        return (
            <Card>
                <CardHeader>
                    <CardTitle className="flex items-center gap-2">
                        <FileText className="h-5 w-5" />
                        Report Library
                    </CardTitle>
                </CardHeader>
                <CardContent>
                    <div className="space-y-2">
                        {[1, 2, 3].map(i => (
                            <Skeleton key={i} className="h-16 w-full" />
                        ))}
                    </div>
                </CardContent>
            </Card>
        )
    }

    return (
        <Card>
            <CardHeader>
                <div className="flex items-center justify-between">
                    <CardTitle className="flex items-center gap-2">
                        <FileText className="h-5 w-5" />
                        Report Library
                        {selected.size > 0 && (
                            <Badge variant="outline" className="ml-2">
                                {selected.size} selected
                            </Badge>
                        )}
                    </CardTitle>
                    <div className="flex gap-2">
                        <Button variant="ghost" size="sm" onClick={fetchReports}>
                            <RefreshCw className="h-4 w-4" />
                        </Button>
                        {selected.size > 0 ? (
                            <Button variant="ghost" size="sm" onClick={deselectAll}>
                                Deselect All
                            </Button>
                        ) : (
                            <Button variant="ghost" size="sm" onClick={selectAll}>
                                Select All
                            </Button>
                        )}
                    </div>
                </div>
            </CardHeader>
            <CardContent>
                <div className="space-y-2 max-h-[400px] overflow-y-auto pr-2">
                    {reports.length === 0 ? (
                        <p className="text-muted-foreground text-center py-8">
                            No reports uploaded yet. Upload your first financial report to get started.
                        </p>
                    ) : (
                        reports.map(r => (
                            <div
                                key={r.id}
                                className={`flex items-center gap-3 p-3 rounded-lg border cursor-pointer transition-colors ${selected.has(r.id)
                                    ? 'border-primary bg-primary/5'
                                    : 'hover:bg-muted/50'
                                    } ${r.status !== 'Ready' ? 'opacity-60 cursor-not-allowed' : ''}`}
                                onClick={() => toggleReport(r.id)}
                            >
                                <Checkbox
                                    checked={selected.has(r.id)}
                                    disabled={r.status !== 'Ready'}
                                />
                                <div className="flex-1 min-w-0">
                                    <div className="font-medium truncate">
                                        {r.ticker} - {r.instrumentName}
                                    </div>
                                    <div className="text-sm text-muted-foreground">
                                        {r.year} {r.quarter ? `Q${r.quarter}` : ''} • {formatReportType(r.reportType)}
                                        {r.chunkCount > 0 && (
                                            <span className="ml-2">• {r.chunkCount} chunks</span>
                                        )}
                                    </div>
                                </div>
                                {getStatusBadge(r.status)}
                            </div>
                        ))
                    )}
                </div>
            </CardContent>
        </Card>
    )
}
