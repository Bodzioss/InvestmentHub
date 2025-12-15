// API Base URL z environment variables
export const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000'
export const SIGNALR_HUB_URL = process.env.NEXT_PUBLIC_SIGNALR_URL || 'http://localhost:5000/hubs/notifications'

// Local Storage Keys
export const STORAGE_KEYS = {
    AUTH_TOKEN: 'investmenthub_token',
    SELECTED_USER: 'investmenthub_selected_user',
    THEME: 'investmenthub_theme',
} as const

// Query Keys dla React Query
export const QUERY_KEYS = {
    PORTFOLIOS: 'portfolios',
    PORTFOLIO: 'portfolio',
    INVESTMENTS: 'investments',
    INVESTMENT: 'investment',
    USERS: 'users',
    USER: 'user',
    INSTRUMENTS: 'instruments',
    MARKET_PRICE: 'market_price',
    PRICE_HISTORY: 'price_history',
} as const

// React Query Configuration
export const QUERY_CONFIG = {
    STALE_TIME: 5 * 60 * 1000, // 5 minut - dane są "fresh" przez 5 min
    RETRY: 1, // próbuj 1 raz w przypadku błędu
    REFETCH_ON_WINDOW_FOCUS: true, // odśwież gdy użytkownik wraca do karty
} as const

// Currencies
export const CURRENCIES = ['USD', 'EUR', 'PLN', 'GBP', 'CHF'] as const
export type Currency = (typeof CURRENCIES)[number]

// Asset Types (to samo co enum w types)
export const ASSET_TYPES = {
    Stock: 'Stock',
    Bond: 'Bond',
    ETF: 'ETF',
    Commodity: 'Commodity',
    Cryptocurrency: 'Cryptocurrency',
    Other: 'Other',
} as const
