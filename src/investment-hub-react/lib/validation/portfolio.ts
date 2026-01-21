import { z } from 'zod'

// ============================================
// PORTFOLIO VALIDATION SCHEMAS
// ============================================

export const createPortfolioSchema = z.object({
    name: z
        .string()
        .min(1, 'Portfolio name is required')
        .max(100, 'Portfolio name must be less than 100 characters'),
    description: z
        .string()
        .max(500, 'Description must be less than 500 characters')
        .optional(),
    currency: z
        .string()
        .min(3, 'Currency code is required')
        .max(3, 'Currency code must be 3 characters')
        .regex(/^[A-Z]{3}$/, 'Currency must be a valid 3-letter code (e.g., USD, EUR)'),
})

export type CreatePortfolioFormData = z.infer<typeof createPortfolioSchema>

export const updatePortfolioSchema = z.object({
    name: z
        .string()
        .min(1, 'Portfolio name is required')
        .max(100, 'Portfolio name must be less than 100 characters'),
    description: z
        .string()
        .max(500, 'Description must be less than 500 characters')
        .optional(),
})

export type UpdatePortfolioFormData = z.infer<typeof updatePortfolioSchema>
