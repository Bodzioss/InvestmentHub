import { z } from 'zod'

// ============================================
// INVESTMENT VALIDATION SCHEMAS
// ============================================

export const addInvestmentSchema = z.object({
    ticker: z
        .string()
        .min(1, 'Ticker symbol is required')
        .max(10, 'Ticker must be less than 10 characters')
        .transform(val => val.toUpperCase()),
    exchange: z
        .string()
        .max(20, 'Exchange must be less than 20 characters')
        .optional()
        .default('NASDAQ'),  // Default exchange - optional for bonds
    assetType: z.enum(['Stock', 'Bond', 'ETF', 'Commodity', 'Cryptocurrency', 'Other']),
    quantity: z
        .number()
        .positive('Quantity must be positive')
        .min(0.000001, 'Quantity must be greater than 0'),
    purchasePrice: z
        .number()
        .positive('Purchase price must be positive')
        .min(0.01, 'Purchase price must be at least 0.01'),
    purchaseCurrency: z
        .string()
        .min(3, 'Currency is required')
        .max(3, 'Currency must be 3 characters')
        .regex(/^[A-Z]{3}$/, 'Currency must be a valid 3-letter code'),
    purchaseDate: z
        .string()
        .min(1, 'Purchase date is required')
        .refine((date) => !isNaN(Date.parse(date)), 'Invalid date format'),
})

export type AddInvestmentFormData = z.infer<typeof addInvestmentSchema>
