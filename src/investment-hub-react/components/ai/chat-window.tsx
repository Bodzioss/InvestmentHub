'use client'

import { useState, useRef, useEffect } from 'react'
import { Send, Bot, User, Loader2, FileText } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Badge } from '@/components/ui/badge'

interface Message {
    role: 'user' | 'assistant'
    content: string
    sources?: SourceReference[]
}

interface SourceReference {
    reportId: string
    ticker: string
    name: string
    year: number
    quarter: number | null
    reportType: string
    pages: number[]
}

const API_BASE = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5146'

interface ChatWindowProps {
    selectedReportIds: string[]
}

export function ChatWindow({ selectedReportIds }: ChatWindowProps) {
    const [messages, setMessages] = useState<Message[]>([])
    const [input, setInput] = useState('')
    const [loading, setLoading] = useState(false)
    const scrollRef = useRef<HTMLDivElement>(null)

    // Auto-scroll to bottom when new messages arrive
    useEffect(() => {
        if (scrollRef.current) {
            scrollRef.current.scrollTop = scrollRef.current.scrollHeight
        }
    }, [messages])

    const sendMessage = async () => {
        if (!input.trim() || loading) return

        const userMessage: Message = { role: 'user', content: input }
        setMessages(prev => [...prev, userMessage])
        setInput('')
        setLoading(true)

        try {
            const response = await fetch(`${API_BASE}/api/ai/chat`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    message: input,
                    reportIds: selectedReportIds.length > 0 ? selectedReportIds : null
                })
            })

            const data = await response.json()

            if (response.ok) {
                setMessages(prev => [...prev, {
                    role: 'assistant',
                    content: data.response,
                    sources: data.sources
                }])
            } else {
                setMessages(prev => [...prev, {
                    role: 'assistant',
                    content: `Error: ${data.error || 'Failed to get response'}`
                }])
            }
        } catch (error) {
            setMessages(prev => [...prev, {
                role: 'assistant',
                content: 'Error: Failed to connect to the AI service'
            }])
        } finally {
            setLoading(false)
        }
    }

    const handleKeyDown = (e: React.KeyboardEvent) => {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault()
            sendMessage()
        }
    }

    const formatSources = (sources: SourceReference[]) => {
        return sources.map(s => (
            <Badge key={s.reportId} variant="outline" className="text-xs">
                <FileText className="h-3 w-3 mr-1" />
                {s.ticker} {s.year} {s.quarter ? `Q${s.quarter}` : ''}
                {s.pages.length > 0 && ` (p.${s.pages.join(', ')})`}
            </Badge>
        ))
    }

    return (
        <Card className="h-[600px] flex flex-col">
            <CardHeader className="pb-3">
                <CardTitle className="flex items-center gap-2">
                    <Bot className="h-5 w-5 text-primary" />
                    Financial Analyst AI
                    {selectedReportIds.length > 0 && (
                        <Badge variant="secondary" className="ml-2">
                            {selectedReportIds.length} report{selectedReportIds.length > 1 ? 's' : ''} selected
                        </Badge>
                    )}
                </CardTitle>
            </CardHeader>
            <CardContent className="flex-1 flex flex-col overflow-hidden">
                <ScrollArea className="flex-1 pr-4" ref={scrollRef}>
                    {messages.length === 0 ? (
                        <div className="text-center text-muted-foreground py-12">
                            <Bot className="h-12 w-12 mx-auto mb-4 opacity-50" />
                            <p className="mb-2">Ask me about your financial reports!</p>
                            <p className="text-sm">
                                {selectedReportIds.length > 0
                                    ? `I'll analyze the ${selectedReportIds.length} selected report${selectedReportIds.length > 1 ? 's' : ''}.`
                                    : 'Select some reports from the library, then ask a question.'}
                            </p>
                            <div className="mt-4 space-y-2 text-sm">
                                <p className="font-medium">Example questions:</p>
                                <p className="text-muted-foreground">"What was the total revenue?"</p>
                                <p className="text-muted-foreground">"Summarize the key risks mentioned"</p>
                                <p className="text-muted-foreground">"What's the net income trend?"</p>
                            </div>
                        </div>
                    ) : (
                        <div className="space-y-4">
                            {messages.map((msg, i) => (
                                <div
                                    key={i}
                                    className={`flex gap-3 ${msg.role === 'user' ? 'justify-end' : 'justify-start'}`}
                                >
                                    {msg.role === 'assistant' && (
                                        <div className="flex-shrink-0 h-8 w-8 rounded-full bg-primary/10 flex items-center justify-center">
                                            <Bot className="h-5 w-5 text-primary" />
                                        </div>
                                    )}
                                    <div className={`max-w-[80%] ${msg.role === 'user' ? 'order-1' : ''}`}>
                                        <div
                                            className={`rounded-lg px-4 py-2 ${msg.role === 'user'
                                                    ? 'bg-primary text-primary-foreground'
                                                    : 'bg-muted'
                                                }`}
                                        >
                                            <p className="whitespace-pre-wrap">{msg.content}</p>
                                        </div>
                                        {msg.sources && msg.sources.length > 0 && (
                                            <div className="flex flex-wrap gap-1 mt-2">
                                                {formatSources(msg.sources)}
                                            </div>
                                        )}
                                    </div>
                                    {msg.role === 'user' && (
                                        <div className="flex-shrink-0 h-8 w-8 rounded-full bg-primary flex items-center justify-center order-2">
                                            <User className="h-5 w-5 text-primary-foreground" />
                                        </div>
                                    )}
                                </div>
                            ))}
                            {loading && (
                                <div className="flex gap-3">
                                    <div className="h-8 w-8 rounded-full bg-primary/10 flex items-center justify-center">
                                        <Bot className="h-5 w-5 text-primary" />
                                    </div>
                                    <div className="bg-muted rounded-lg px-4 py-2">
                                        <div className="flex items-center gap-2">
                                            <Loader2 className="h-4 w-4 animate-spin" />
                                            <span>Analyzing documents...</span>
                                        </div>
                                    </div>
                                </div>
                            )}
                        </div>
                    )}
                </ScrollArea>
                <div className="flex gap-2 mt-4 pt-4 border-t">
                    <Input
                        value={input}
                        onChange={e => setInput(e.target.value)}
                        onKeyDown={handleKeyDown}
                        placeholder={
                            selectedReportIds.length > 0
                                ? "Ask about your financial reports..."
                                : "Select reports first, then ask a question..."
                        }
                        disabled={loading || selectedReportIds.length === 0}
                    />
                    <Button
                        onClick={sendMessage}
                        disabled={loading || !input.trim() || selectedReportIds.length === 0}
                    >
                        {loading ? (
                            <Loader2 className="h-4 w-4 animate-spin" />
                        ) : (
                            <Send className="h-4 w-4" />
                        )}
                    </Button>
                </div>
            </CardContent>
        </Card>
    )
}
