import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { AddInvestmentDialog } from './add-investment-dialog'
import React from 'react'

// Mock all hooks from the barrel file
const mockMutateAsync = vi.fn()
vi.mock('@/lib/hooks', () => ({
    useAuth: vi.fn(() => ({ userId: 'test-user-id' })),
    useAddInvestment: vi.fn(() => ({
        mutate: mockMutateAsync,
        mutateAsync: mockMutateAsync,
        isPending: false,
    })),
    useInstruments: vi.fn(() => ({
        data: [],
        isLoading: false
    })),
    useInstrumentSearch: vi.fn(() => ({
        // Mock returning a search result so we can select it
        data: [{ id: '1', ticker: 'AAPL', name: 'Apple Inc.', exchange: 'NASDAQ', assetType: 'Stock' }],
        isLoading: false
    })),
    useMarketPrice: vi.fn(() => ({
        data: null,
        isFetching: false
    }))
}))

// Mock Radix UI Dialog with Context
const DialogContext = React.createContext<{ open: boolean, onOpenChange: (open: boolean) => void }>({ open: false, onOpenChange: () => { } })

vi.mock('@/components/ui/dialog', () => ({
    Dialog: ({ children, open, onOpenChange }: { children: React.ReactNode, open: boolean, onOpenChange: (o: boolean) => void }) => (
        <DialogContext.Provider value={{ open, onOpenChange: onOpenChange || (() => { }) }}>
            <div data-testid="dialog-root">{children}</div>
        </DialogContext.Provider>
    ),
    DialogContent: ({ children }: { children: React.ReactNode }) => (
        <DialogContext.Consumer>
            {({ open }) => open ? <div data-testid="dialog-content">{children}</div> : null}
        </DialogContext.Consumer>
    ),
    DialogHeader: ({ children }: { children: React.ReactNode }) => <div data-testid="dialog-header">{children}</div>,
    DialogTitle: ({ children }: { children: React.ReactNode }) => <h1>{children}</h1>,
    DialogDescription: ({ children }: { children: React.ReactNode }) => <p>{children}</p>,
    DialogTrigger: ({ children, asChild, onClick }: any) => (
        <DialogContext.Consumer>
            {({ onOpenChange }) => (
                <div onClick={(e) => {
                    if (onClick) onClick(e)
                    if (onOpenChange) onOpenChange(true)
                }}>
                    {children}
                </div>
            )}
        </DialogContext.Consumer>
    ),
}))

describe('AddInvestmentDialog', () => {
    beforeEach(() => {
        vi.clearAllMocks()
        // Reset console too just in case
    })

    const TestComponent = ({ onOpenChange }: { onOpenChange?: any }) => (
        <AddInvestmentDialog portfolioId="port-1">
            <button>Open Dialog</button>
        </AddInvestmentDialog>
    )

    it('opens dialog when trigger is clicked', async () => {
        const user = userEvent.setup()
        render(<TestComponent />)

        expect(screen.queryByText('Add Investment')).not.toBeInTheDocument()
        await user.click(screen.getByText('Open Dialog'))
        expect(screen.getByRole('heading', { name: /Add Investment/i })).toBeInTheDocument()
    })

    it('shows validation error on empty submit', async () => {
        const user = userEvent.setup()
        render(<TestComponent />)

        await user.click(screen.getByText('Open Dialog'))
        const submitBtn = screen.getByRole('button', { name: /Add Investment/i })
        await user.click(submitBtn)

        await waitFor(() => {
            expect(screen.getByText(/Ticker/i)).toBeInTheDocument()
            expect(mockMutateAsync).not.toHaveBeenCalled()
        })
    })

    it('submits form with correct data', async () => {
        const user = userEvent.setup()
        render(<TestComponent />)

        // Open
        await user.click(screen.getByText('Open Dialog'))

        // Select Instrument
        const searchTrigger = screen.getByText(/Search by ticker/i)
        await user.click(searchTrigger)

        // Click result "AAPL"
        const option = await screen.findByText('AAPL')
        await user.click(option)

        // Fill form
        await user.type(screen.getByLabelText(/Quantity/i), '10')
        await user.type(screen.getByLabelText(/Purchase Price/i), '200')

        // Submit
        const submitBtn = screen.getByRole('button', { name: /Add Investment/i })
        await user.click(submitBtn)

        await waitFor(() => {
            expect(mockMutateAsync).toHaveBeenCalled()
            const args = mockMutateAsync.mock.calls[0][0]

            expect(args.portfolioId).toBe('port-1')

            // Nested Symbol Check
            expect(args.symbol).toBeDefined()
            expect(args.symbol.ticker).toBe('AAPL')
            expect(args.symbol.assetType).toBe('Stock')

            // Quantity Check
            expect(args.quantity).toBe(10)

            // Purchase Price Check
            expect(args.purchasePrice).toBeDefined()
            expect(args.purchasePrice.amount).toBe(200)
            expect(args.purchasePrice.currency).toBe('USD') // Default
        })
    })
})
