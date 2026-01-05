'use client'

import { useState, useEffect } from 'react'
import { Bot, FileText, BarChart3 } from 'lucide-react'
import { ReportUploadDialog } from '@/components/ai/report-upload-dialog'
import { ReportLibrary } from '@/components/ai/report-library'
import { ChatWindow } from '@/components/ai/chat-window'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

const API_BASE = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5146'

interface Stats {
    totalReports: number
    readyReports: number
    processingReports: number
    totalChunks: number
    uniqueInstruments: number
}

export default function AIPage() {
    const [selectedReportIds, setSelectedReportIds] = useState<string[]>([])
    const [refreshTrigger, setRefreshTrigger] = useState(0)
    const [stats, setStats] = useState<Stats | null>(null)

    // Load stats on mount
    useEffect(() => {
        fetch(`${API_BASE}/api/ai/stats`)
            .then(r => r.json())
            .then(setStats)
            .catch(console.error)
    }, [])

    const handleUploadComplete = () => {
        setRefreshTrigger(prev => prev + 1)
        // Refresh stats
        fetch(`${API_BASE}/api/ai/stats`)
            .then(r => r.json())
            .then(setStats)
            .catch(console.error)
    }

    return (
        <div className="container mx-auto py-6 px-4">
            {/* Header */}
            <div className="flex items-center justify-between mb-6">
                <div>
                    <h1 className="text-3xl font-bold flex items-center gap-2">
                        <Bot className="h-8 w-8 text-primary" />
                        Financial Analyst AI
                    </h1>
                    <p className="text-muted-foreground mt-1">
                        Upload financial reports and ask questions about them
                    </p>
                </div>
                <ReportUploadDialog onUploadComplete={handleUploadComplete} />
            </div>

            {/* Stats */}
            {stats && (
                <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
                    <Card>
                        <CardContent className="pt-4">
                            <div className="flex items-center justify-between">
                                <div>
                                    <p className="text-sm text-muted-foreground">Reports</p>
                                    <p className="text-2xl font-bold">{stats.totalReports}</p>
                                </div>
                                <FileText className="h-8 w-8 text-muted-foreground/50" />
                            </div>
                        </CardContent>
                    </Card>
                    <Card>
                        <CardContent className="pt-4">
                            <div className="flex items-center justify-between">
                                <div>
                                    <p className="text-sm text-muted-foreground">Ready</p>
                                    <p className="text-2xl font-bold text-green-500">{stats.readyReports}</p>
                                </div>
                                <div className="h-8 w-8 rounded-full bg-green-500/20 flex items-center justify-center">
                                    <div className="h-3 w-3 rounded-full bg-green-500" />
                                </div>
                            </div>
                        </CardContent>
                    </Card>
                    <Card>
                        <CardContent className="pt-4">
                            <div className="flex items-center justify-between">
                                <div>
                                    <p className="text-sm text-muted-foreground">Chunks</p>
                                    <p className="text-2xl font-bold">{stats.totalChunks}</p>
                                </div>
                                <BarChart3 className="h-8 w-8 text-muted-foreground/50" />
                            </div>
                        </CardContent>
                    </Card>
                    <Card>
                        <CardContent className="pt-4">
                            <div className="flex items-center justify-between">
                                <div>
                                    <p className="text-sm text-muted-foreground">Companies</p>
                                    <p className="text-2xl font-bold">{stats.uniqueInstruments}</p>
                                </div>
                                <div className="h-8 w-8 rounded-full bg-primary/20 flex items-center justify-center text-primary font-bold">
                                    {stats.uniqueInstruments}
                                </div>
                            </div>
                        </CardContent>
                    </Card>
                </div>
            )}

            {/* Main content */}
            <div className="grid lg:grid-cols-3 gap-6">
                {/* Report Library - Left side */}
                <div className="lg:col-span-1">
                    <ReportLibrary
                        onSelectionChange={setSelectedReportIds}
                        refreshTrigger={refreshTrigger}
                    />
                </div>

                {/* Chat Window - Right side */}
                <div className="lg:col-span-2">
                    <ChatWindow selectedReportIds={selectedReportIds} />
                </div>
            </div>
        </div>
    )
}
