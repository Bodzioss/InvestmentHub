'use client'

import { format } from 'date-fns'
import { ArrowDownCircle, ArrowUpCircle, Wallet, Percent, MoreHorizontal, XCircle, Edit } from 'lucide-react'
import { useTransactions, useCancelTransaction } from '@/lib/hooks/use-transactions'
import { TransactionType, TransactionStatus } from '@/lib/types'
import type { Transaction } from '@/lib/types'
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow,
} from '@/components/ui/table'
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'

interface TransactionsListProps {
    portfolioId: string
}

const typeIcons = {
    [TransactionType.BUY]: <ArrowDownCircle className="h-4 w-4 text-green-500" />,
    [TransactionType.SELL]: <ArrowUpCircle className="h-4 w-4 text-red-500" />,
    [TransactionType.DIVIDEND]: <Wallet className="h-4 w-4 text-blue-500" />,
    [TransactionType.INTEREST]: <Percent className="h-4 w-4 text-purple-500" />,
}

const typeColors = {
    [TransactionType.BUY]: 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200',
    [TransactionType.SELL]: 'bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-200',
    [TransactionType.DIVIDEND]: 'bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200',
    [TransactionType.INTEREST]: 'bg-purple-100 text-purple-800 dark:bg-purple-900 dark:text-purple-200',
}

export function TransactionsList({ portfolioId }: TransactionsListProps) {
    const { data, isLoading, error } = useTransactions(portfolioId)
    const cancelTransaction = useCancelTransaction()

    if (isLoading) {
        return (
            <Card>
                <CardHeader>
                    <CardTitle>Transactions</CardTitle>
                </CardHeader>
                <CardContent>
                    <div className="space-y-3">
                        {[1, 2, 3].map((i) => (
                            <Skeleton key={i} className="h-12 w-full" />
                        ))}
                    </div>
                </CardContent>
            </Card>
        )
    }

    if (error) {
        return (
            <Card>
                <CardHeader>
                    <CardTitle>Transactions</CardTitle>
                </CardHeader>
                <CardContent>
                    <p className="text-destructive">Failed to load transactions</p>
                </CardContent>
            </Card>
        )
    }

    const transactions = data?.transactions ?? []

    if (transactions.length === 0) {
        return (
            <Card>
                <CardHeader>
                    <CardTitle>Transactions</CardTitle>
                    <CardDescription>No transactions recorded yet</CardDescription>
                </CardHeader>
                <CardContent>
                    <p className="text-muted-foreground text-sm">
                        Use the &quot;Record Transaction&quot; button to add your first transaction.
                    </p>
                </CardContent>
            </Card>
        )
    }

    function handleCancel(transaction: Transaction) {
        if (confirm(`Cancel transaction for ${transaction.ticker}?`)) {
            cancelTransaction.mutate({ portfolioId, transactionId: transaction.transactionId })
        }
    }

    function formatAmount(transaction: Transaction): string {
        if (transaction.type === TransactionType.BUY || transaction.type === TransactionType.SELL) {
            const total = (transaction.quantity ?? 0) * (transaction.pricePerUnit ?? 0)
            return `${total.toFixed(2)} ${transaction.currency}`
        }
        return `${(transaction.netAmount ?? transaction.grossAmount ?? 0).toFixed(2)} ${transaction.currency}`
    }

    return (
        <Card>
            <CardHeader>
                <CardTitle>Transactions ({transactions.length})</CardTitle>
                <CardDescription>All recorded transactions for this portfolio</CardDescription>
            </CardHeader>
            <CardContent>
                <div className="rounded-md border">
                    <Table>
                        <TableHeader>
                            <TableRow>
                                <TableHead>Type</TableHead>
                                <TableHead>Symbol</TableHead>
                                <TableHead className="text-right">Quantity</TableHead>
                                <TableHead className="text-right">Price</TableHead>
                                <TableHead className="text-right">Total</TableHead>
                                <TableHead>Date</TableHead>
                                <TableHead>Status</TableHead>
                                <TableHead className="w-[50px]"></TableHead>
                            </TableRow>
                        </TableHeader>
                        <TableBody>
                            {transactions.map((t) => (
                                <TableRow key={t.transactionId} className={t.status === TransactionStatus.Cancelled ? 'opacity-50' : ''}>
                                    <TableCell>
                                        <div className="flex items-center gap-2">
                                            {typeIcons[t.type]}
                                            <Badge variant="secondary" className={typeColors[t.type]}>
                                                {t.type}
                                            </Badge>
                                        </div>
                                    </TableCell>
                                    <TableCell>
                                        <div className="font-medium">{t.ticker}</div>
                                        <div className="text-xs text-muted-foreground">{t.exchange}</div>
                                    </TableCell>
                                    <TableCell className="text-right font-mono">
                                        {t.quantity?.toFixed(4) ?? '-'}
                                    </TableCell>
                                    <TableCell className="text-right font-mono">
                                        {t.pricePerUnit?.toFixed(2) ?? t.grossAmount?.toFixed(2) ?? '-'}
                                    </TableCell>
                                    <TableCell className="text-right font-mono font-medium">
                                        {formatAmount(t)}
                                    </TableCell>
                                    <TableCell>
                                        {format(new Date(t.transactionDate), 'MMM dd, yyyy')}
                                    </TableCell>
                                    <TableCell>
                                        <Badge variant={t.status === TransactionStatus.Active ? 'default' : 'secondary'}>
                                            {t.status}
                                        </Badge>
                                    </TableCell>
                                    <TableCell>
                                        {t.status === TransactionStatus.Active && (
                                            <DropdownMenu>
                                                <DropdownMenuTrigger asChild>
                                                    <Button variant="ghost" size="icon" className="h-8 w-8">
                                                        <MoreHorizontal className="h-4 w-4" />
                                                    </Button>
                                                </DropdownMenuTrigger>
                                                <DropdownMenuContent align="end">
                                                    <DropdownMenuItem onClick={() => handleCancel(t)} className="text-destructive">
                                                        <XCircle className="mr-2 h-4 w-4" />
                                                        Cancel
                                                    </DropdownMenuItem>
                                                </DropdownMenuContent>
                                            </DropdownMenu>
                                        )}
                                    </TableCell>
                                </TableRow>
                            ))}
                        </TableBody>
                    </Table>
                </div>
            </CardContent>
        </Card>
    )
}
