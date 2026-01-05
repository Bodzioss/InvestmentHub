'use client'

import { useState, useCallback } from 'react'
import { Upload, FileSpreadsheet, CheckCircle2, AlertCircle, Loader2, X, ArrowRight } from 'lucide-react'
import { useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogHeader,
    DialogTitle,
    DialogTrigger,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Checkbox } from '@/components/ui/checkbox'
import { ScrollArea } from '@/components/ui/scroll-area'
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow,
} from '@/components/ui/table'
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from '@/components/ui/select'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import {
    previewMyFundCsv,
    importTransactions,
    type ParsedTransactionDto,
    type ImportPreviewResponse,
    type TransactionToImport
} from '@/lib/api/import'

interface ImportCsvDialogProps {
    portfolioId: string
    children: React.ReactNode
}

export function ImportCsvDialog({ portfolioId, children }: ImportCsvDialogProps) {
    const [open, setOpen] = useState(false)
    const [step, setStep] = useState<'upload' | 'preview' | 'importing' | 'complete'>('upload')
    const [file, setFile] = useState<File | null>(null)
    const [isDragging, setIsDragging] = useState(false)
    const [previewData, setPreviewData] = useState<ImportPreviewResponse | null>(null)
    const [selectedTransactions, setSelectedTransactions] = useState<Set<number>>(new Set())
    const [exchange, setExchange] = useState('WSE')
    const [isLoading, setIsLoading] = useState(false)
    const [importResult, setImportResult] = useState<{ imported: number; failed: number; errors: string[] } | null>(null)

    const queryClient = useQueryClient()

    const resetState = useCallback(() => {
        setStep('upload')
        setFile(null)
        setPreviewData(null)
        setSelectedTransactions(new Set())
        setImportResult(null)
        setIsLoading(false)
    }, [])

    const handleDragOver = useCallback((e: React.DragEvent) => {
        e.preventDefault()
        setIsDragging(true)
    }, [])

    const handleDragLeave = useCallback(() => {
        setIsDragging(false)
    }, [])

    const handleDrop = useCallback((e: React.DragEvent) => {
        e.preventDefault()
        setIsDragging(false)
        const droppedFile = e.dataTransfer.files[0]
        if (droppedFile?.name.endsWith('.csv')) {
            setFile(droppedFile)
        } else {
            toast.error('Wybierz plik CSV')
        }
    }, [])

    const handleFileSelect = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
        const selectedFile = e.target.files?.[0]
        if (selectedFile) {
            setFile(selectedFile)
        }
    }, [])

    const handleUpload = async () => {
        if (!file) return

        setIsLoading(true)
        try {
            const result = await previewMyFundCsv(file)
            setPreviewData(result)

            // Select only existing instruments by default
            const validIndices = new Set(
                result.transactions
                    .map((tx, idx) => tx.instrumentExists ? idx : -1)
                    .filter(idx => idx !== -1)
            )
            setSelectedTransactions(validIndices)

            setStep('preview')
        } catch (error) {
            console.error('Error parsing CSV:', error)
            toast.error('Błąd parsowania pliku CSV')
        } finally {
            setIsLoading(false)
        }
    }

    const toggleTransaction = (index: number) => {
        setSelectedTransactions(prev => {
            const next = new Set(prev)
            if (next.has(index)) {
                next.delete(index)
            } else {
                next.add(index)
            }
            return next
        })
    }

    const toggleAll = () => {
        if (!previewData) return

        const validTransactions = previewData.transactions
            .map((tx, idx) => (tx.instrumentExists ? idx : -1))
            .filter((idx) => idx !== -1)

        if (selectedTransactions.size === validTransactions.length) {
            setSelectedTransactions(new Set())
        } else {
            setSelectedTransactions(new Set(validTransactions))
        }
    }

    const handleImport = async () => {
        if (!previewData) return

        const transactionsToImport: TransactionToImport[] = previewData.transactions
            .filter((_, idx) => selectedTransactions.has(idx))
            .map(tx => ({
                date: tx.date,
                transactionType: tx.transactionType,
                ticker: tx.ticker,
                currency: tx.currency,
                quantity: tx.quantity,
                pricePerUnit: tx.pricePerUnit,
                notes: tx.notes ?? undefined
            }))

        if (transactionsToImport.length === 0) {
            toast.error('Wybierz co najmniej jedną transakcję')
            return
        }

        setStep('importing')
        setIsLoading(true)

        try {
            const result = await importTransactions({
                portfolioId,
                exchange,
                transactions: transactionsToImport
            })

            setImportResult({
                imported: result.importedCount,
                failed: result.failedCount,
                errors: result.errors
            })

            // Invalidate queries to refresh data
            queryClient.invalidateQueries({ queryKey: ['transactions', portfolioId] })
            queryClient.invalidateQueries({ queryKey: ['positions', portfolioId] })
            queryClient.invalidateQueries({ queryKey: ['portfolio', portfolioId] })

            setStep('complete')

            if (result.isSuccess) {
                toast.success(`Zaimportowano ${result.importedCount} transakcji`)
            }
        } catch (error) {
            console.error('Error importing transactions:', error)
            toast.error('Błąd importu transakcji')
            setStep('preview')
        } finally {
            setIsLoading(false)
        }
    }

    const getTransactionTypeBadge = (type: string) => {
        switch (type.toUpperCase()) {
            case 'BUY':
                return <Badge className="bg-green-600">Kupno</Badge>
            case 'SELL':
                return <Badge variant="destructive">Sprzedaż</Badge>
            case 'DIVIDEND':
                return <Badge className="bg-purple-600">Dywidenda</Badge>
            case 'INTEREST':
                return <Badge className="bg-blue-600">Odsetki</Badge>
            default:
                return <Badge variant="secondary">{type}</Badge>
        }
    }

    return (
        <Dialog open={open} onOpenChange={(isOpen) => {
            setOpen(isOpen)
            if (!isOpen) resetState()
        }}>
            <DialogTrigger asChild>{children}</DialogTrigger>
            <DialogContent className="sm:max-w-[800px] max-h-[90vh]">
                <DialogHeader>
                    <DialogTitle className="flex items-center gap-2">
                        <FileSpreadsheet className="h-5 w-5" />
                        Import z MyFund CSV
                    </DialogTitle>
                    <DialogDescription>
                        Zaimportuj transakcje z pliku CSV wyeksportowanego z MyFund.pl
                    </DialogDescription>
                </DialogHeader>

                {step === 'upload' && (
                    <div className="space-y-4">
                        <div
                            className={`
                                border-2 border-dashed rounded-lg p-8 text-center cursor-pointer
                                transition-colors duration-200
                                ${isDragging ? 'border-primary bg-primary/5' : 'border-muted-foreground/25 hover:border-primary/50'}
                            `}
                            onDragOver={handleDragOver}
                            onDragLeave={handleDragLeave}
                            onDrop={handleDrop}
                            onClick={() => document.getElementById('csv-file-input')?.click()}
                        >
                            <input
                                id="csv-file-input"
                                type="file"
                                accept=".csv"
                                className="hidden"
                                onChange={handleFileSelect}
                            />
                            <Upload className="h-12 w-12 mx-auto mb-4 text-muted-foreground" />
                            <p className="text-lg font-medium">
                                {file ? file.name : 'Przeciągnij plik CSV lub kliknij'}
                            </p>
                            <p className="text-sm text-muted-foreground mt-1">
                                Obsługiwane formaty: CSV z MyFund.pl
                            </p>
                        </div>

                        {file && (
                            <div className="flex items-center justify-between p-3 rounded-lg bg-muted">
                                <div className="flex items-center gap-2">
                                    <FileSpreadsheet className="h-5 w-5 text-green-600" />
                                    <span className="font-medium">{file.name}</span>
                                    <span className="text-sm text-muted-foreground">
                                        ({(file.size / 1024).toFixed(1)} KB)
                                    </span>
                                </div>
                                <Button variant="ghost" size="sm" onClick={() => setFile(null)}>
                                    <X className="h-4 w-4" />
                                </Button>
                            </div>
                        )}

                        <div className="flex justify-end gap-3">
                            <Button variant="outline" onClick={() => setOpen(false)}>
                                Anuluj
                            </Button>
                            <Button onClick={handleUpload} disabled={!file || isLoading}>
                                {isLoading ? (
                                    <>
                                        <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                                        Przetwarzanie...
                                    </>
                                ) : (
                                    <>
                                        Dalej
                                        <ArrowRight className="ml-2 h-4 w-4" />
                                    </>
                                )}
                            </Button>
                        </div>
                    </div>
                )}

                {step === 'preview' && previewData && (
                    <div className="space-y-4">
                        {/* Summary */}
                        <div className="grid grid-cols-3 gap-4">
                            <div className="p-3 rounded-lg bg-muted text-center">
                                <p className="text-2xl font-bold">{previewData.totalRows}</p>
                                <p className="text-sm text-muted-foreground">Wierszy</p>
                            </div>
                            <div className="p-3 rounded-lg bg-green-500/10 text-center">
                                <p className="text-2xl font-bold text-green-600">{previewData.parsedTransactions}</p>
                                <p className="text-sm text-muted-foreground">Transakcji</p>
                            </div>
                            <div className="p-3 rounded-lg bg-orange-500/10 text-center">
                                <p className="text-2xl font-bold text-orange-600">{previewData.skippedRows}</p>
                                <p className="text-sm text-muted-foreground">Pominiętych</p>
                            </div>
                        </div>

                        {/* Errors */}
                        {previewData.errors.length > 0 && (
                            <Alert variant="destructive">
                                <AlertCircle className="h-4 w-4" />
                                <AlertTitle>Błędy parsowania</AlertTitle>
                                <AlertDescription>
                                    <ul className="list-disc list-inside mt-2">
                                        {previewData.errors.map((err, i) => (
                                            <li key={i}>{err}</li>
                                        ))}
                                    </ul>
                                </AlertDescription>
                            </Alert>
                        )}

                        {/* Warnings */}
                        {previewData.warnings.length > 0 && (
                            <Alert>
                                <AlertCircle className="h-4 w-4" />
                                <AlertTitle>Ostrzeżenia</AlertTitle>
                                <AlertDescription>
                                    <ul className="list-disc list-inside mt-2 max-h-20 overflow-y-auto">
                                        {previewData.warnings.slice(0, 5).map((warn, i) => (
                                            <li key={i}>{warn}</li>
                                        ))}
                                        {previewData.warnings.length > 5 && (
                                            <li className="text-muted-foreground">
                                                ...i {previewData.warnings.length - 5} więcej
                                            </li>
                                        )}
                                    </ul>
                                </AlertDescription>
                            </Alert>
                        )}

                        {/* Exchange selector */}
                        <div className="flex items-center gap-4">
                            <label className="text-sm font-medium">Giełda:</label>
                            <Select value={exchange} onValueChange={setExchange}>
                                <SelectTrigger className="w-[180px]">
                                    <SelectValue />
                                </SelectTrigger>
                                <SelectContent>
                                    <SelectItem value="WSE">GPW Warszawa</SelectItem>
                                    <SelectItem value="NASDAQ">NASDAQ</SelectItem>
                                    <SelectItem value="NYSE">NYSE</SelectItem>
                                    <SelectItem value="PKO">Obligacje PKO</SelectItem>
                                </SelectContent>
                            </Select>
                            <span className="text-sm text-muted-foreground">
                                Wybrano: {selectedTransactions.size} z {previewData.transactions.length}
                            </span>
                        </div>

                        {/* Transactions table */}
                        <ScrollArea className="h-[300px] rounded-md border">
                            <Table>
                                <TableHeader>
                                    <TableRow>
                                        <TableHead className="w-12">
                                            <Checkbox
                                                checked={selectedTransactions.size === previewData.transactions.length}
                                                onCheckedChange={() => toggleAll()}
                                            />
                                        </TableHead>
                                        <TableHead>Data</TableHead>
                                        <TableHead>Typ</TableHead>
                                        <TableHead>Ticker</TableHead>
                                        <TableHead>Instrument</TableHead>
                                        <TableHead>Status</TableHead>
                                        <TableHead className="text-right">Ilość</TableHead>
                                        <TableHead className="text-right">Cena</TableHead>
                                        <TableHead className="text-right">Wartość</TableHead>
                                    </TableRow>
                                </TableHeader>
                                <TableBody>
                                    {previewData.transactions.map((tx, idx) => (
                                        <TableRow
                                            key={idx}
                                            className={!tx.instrumentExists ? 'bg-destructive/5 opacity-80' : !selectedTransactions.has(idx) ? 'opacity-50' : ''}
                                        >
                                            <TableCell>
                                                <Checkbox
                                                    checked={selectedTransactions.has(idx)}
                                                    onCheckedChange={() => toggleTransaction(idx)}
                                                    disabled={!tx.instrumentExists}
                                                />
                                            </TableCell>
                                            <TableCell className="whitespace-nowrap">{new Date(tx.date).toLocaleDateString('pl-PL')}</TableCell>
                                            <TableCell>{getTransactionTypeBadge(tx.transactionType)}</TableCell>
                                            <TableCell className="font-mono font-semibold">{tx.ticker}</TableCell>
                                            <TableCell>
                                                {tx.assetType && (
                                                    <Badge variant="outline" className="text-[10px] uppercase">
                                                        {tx.assetType}
                                                    </Badge>
                                                )}
                                            </TableCell>
                                            <TableCell>
                                                {tx.instrumentExists ? (
                                                    <Badge variant="secondary" className="bg-green-100 text-green-700 hover:bg-green-100 border-none">
                                                        Rozpoznany
                                                    </Badge>
                                                ) : (
                                                    <Badge variant="destructive" className="bg-red-100 text-red-700 hover:bg-red-100 border-none">
                                                        Nieznany
                                                    </Badge>
                                                )}
                                            </TableCell>
                                            <TableCell className="text-right">{tx.quantity}</TableCell>
                                            <TableCell className="text-right">{tx.pricePerUnit.toFixed(2)} {tx.currency}</TableCell>
                                            <TableCell className="text-right font-medium">{tx.totalValue.toFixed(2)} {tx.currency}</TableCell>
                                        </TableRow>
                                    ))}
                                </TableBody>
                            </Table>
                        </ScrollArea>

                        <div className="flex justify-between pt-4">
                            <Button variant="outline" onClick={resetState}>
                                Wróć
                            </Button>
                            <Button onClick={handleImport} disabled={selectedTransactions.size === 0}>
                                Importuj {selectedTransactions.size} transakcji
                            </Button>
                        </div>
                    </div>
                )}

                {step === 'importing' && (
                    <div className="flex flex-col items-center justify-center py-12">
                        <Loader2 className="h-12 w-12 animate-spin text-primary mb-4" />
                        <p className="text-lg font-medium">Importowanie transakcji...</p>
                        <p className="text-sm text-muted-foreground">To może potrwać chwilę</p>
                    </div>
                )}

                {step === 'complete' && importResult && (
                    <div className="space-y-4">
                        <div className="flex flex-col items-center justify-center py-8">
                            {importResult.failed === 0 ? (
                                <CheckCircle2 className="h-16 w-16 text-green-600 mb-4" />
                            ) : (
                                <AlertCircle className="h-16 w-16 text-orange-600 mb-4" />
                            )}
                            <p className="text-xl font-semibold">Import zakończony</p>
                        </div>

                        <div className="grid grid-cols-2 gap-4">
                            <div className="p-4 rounded-lg bg-green-500/10 text-center">
                                <p className="text-3xl font-bold text-green-600">{importResult.imported}</p>
                                <p className="text-sm text-muted-foreground">Zaimportowano</p>
                            </div>
                            <div className="p-4 rounded-lg bg-red-500/10 text-center">
                                <p className="text-3xl font-bold text-red-600">{importResult.failed}</p>
                                <p className="text-sm text-muted-foreground">Błędów</p>
                            </div>
                        </div>

                        {importResult.errors.length > 0 && (
                            <Alert variant="destructive">
                                <AlertCircle className="h-4 w-4" />
                                <AlertTitle>Błędy importu</AlertTitle>
                                <AlertDescription>
                                    <ScrollArea className="h-24 mt-2">
                                        <ul className="list-disc list-inside">
                                            {importResult.errors.map((err, i) => (
                                                <li key={i}>{err}</li>
                                            ))}
                                        </ul>
                                    </ScrollArea>
                                </AlertDescription>
                            </Alert>
                        )}

                        <div className="flex justify-end pt-4">
                            <Button onClick={() => setOpen(false)}>
                                Zamknij
                            </Button>
                        </div>
                    </div>
                )}
            </DialogContent>
        </Dialog>
    )
}
