import { render, screen } from '@testing-library/react'
import { describe, it, expect } from 'vitest'
import { AssetAllocationChart } from './asset-allocation-chart'
import React from 'react'

// ResizeObserver mock needed for Recharts in JSDOM
global.ResizeObserver = class ResizeObserver {
    observe() { }
    unobserve() { }
    disconnect() { }
}

describe('AssetAllocationChart', () => {
    const mockData = [
        { name: 'Stocks', value: 1000 },
        { name: 'Bonds', value: 500 },
    ]

    it('renders correctly with data', () => {
        // Note: Recharts is tricky to test in JSDOM because it renders SVG based on layout.
        // We check if the container renders without crashing.
        const { container } = render(<AssetAllocationChart data={mockData} />)
        expect(container).toBeInTheDocument()
        expect(screen.getByText('Asset Allocation')).toBeInTheDocument()
    })

    it('renders correctly with empty data', () => {
        const { container } = render(<AssetAllocationChart data={[]} />)
        expect(container).toBeInTheDocument()
        expect(screen.getByText('Asset Allocation')).toBeInTheDocument()
    })

    it('renders correctly with undefined data', () => {
        // @ts-expect-error Testing runtime safety
        const { container } = render(<AssetAllocationChart data={undefined} />)
        expect(container).toBeInTheDocument()
    })
})
