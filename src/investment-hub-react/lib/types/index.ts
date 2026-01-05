// ============================================
// PORTFOLIO TYPES
// ============================================

export interface Portfolio {
    id: string
    ownerId: string
    name: string
    description?: string
    currency: string
    createdDate: string
    activeInvestmentCount: number
    totalValue?: Money
    totalCost?: Money
    unrealizedGainLoss?: Money
}

export interface CreatePortfolioRequest {
    portfolioId: string
    ownerId: string
    name: string
    description?: string
    currency: string
}

export interface UpdatePortfolioRequest {
    name: string
    description?: string
}

// ============================================
// INVESTMENT TYPES
// ============================================

export interface Investment {
    id: string
    portfolioId: string
    symbol: Symbol
    quantity: number
    purchasePrice: Money
    purchaseDate: string
    salePrice?: Money
    saleDate?: string
    status: InvestmentStatus
    currentValue?: Money
    currentPrice?: Money
    unrealizedGainLoss?: Money
    realizedGainLoss?: Money
}

export interface AddInvestmentRequest {
    portfolioId: string  // Backend expects PascalCase but axios transforms
    symbol: {
        ticker: string
        exchange: string  // REQUIRED by backend
        assetType: string
    }
    quantity: number
    purchasePrice: {
        amount: number
        currency: string
    }
    purchaseDate: string  // ISO date string
}

export interface UpdateInvestmentRequest {
    quantity: number
    purchasePrice: Money
    purchaseDate: string
}

export interface UpdateInvestmentValueRequest {
    investmentId: string
    currentPrice: Money
}

export interface SellInvestmentRequest {
    investmentId: string
    salePrice: Money
    quantityToSell?: number
    saleDate: string
}

export enum InvestmentStatus {
    Active = 'Active',
    Sold = 'Sold',
    Deleted = 'Deleted'
}

// ============================================
// INSTRUMENT TYPES
// ============================================

export interface Instrument {
    id: string
    name: string
    ticker: string
    exchange: string
    assetType: string
    isin: string
}

// ============================================
// MARKET DATA TYPES
// ============================================

export interface MarketPrice {
    symbol: string
    price: number
    currency: string
    timestamp: string
    source?: string
    open?: number
    high?: number
    low?: number
    close?: number
    volume?: number
}

export interface MarketPriceRefreshResult {
    symbol: string
    price?: number
    currency?: string
    timestamp?: string
    source?: string
    traceLogs: string[]
    success: boolean
}

// ============================================
// USER TYPES
// ============================================

export interface User {
    id: string
    email: string
    name: string
    role: string
}

export interface LoginRequest {
    email: string
    password: string
}

export interface LoginResponse {
    token: string
    user: User
}

export interface RegisterRequest {
    email: string
    name: string
    password: string
}

export interface ChangePasswordRequest {
    currentPassword: string
    newPassword: string
}

// ============================================
// COMMON VALUE OBJECTS
// ============================================

export interface Money {
    amount: number
    currency: string
}

export interface Symbol {
    ticker: string
    name: string
    assetType: AssetType
}

export enum AssetType {
    Stock = 'Stock',
    Bond = 'Bond',
    ETF = 'ETF',
    Commodity = 'Commodity',
    Cryptocurrency = 'Cryptocurrency',
    Other = 'Other'
}

// ============================================
// MARKET DATA TYPES
// ============================================

export interface MarketPrice {
    symbol: string
    price: number
    currency: string
    timestamp: string
}

export interface PriceHistory {
    symbol: string
    prices: PricePoint[]
}

export interface PricePoint {
    date: string
    price: number
}

// ============================================
// API RESPONSE TYPES
// ============================================

export interface ApiError {
    error: string
    details?: Record<string, string[]>
}

export interface PaginatedResponse<T> {
    items: T[]
    totalCount: number
    page: number
    pageSize: number
}

// ============================================
// TRANSACTION TYPES
// ============================================

export enum TransactionType {
    BUY = 'BUY',
    SELL = 'SELL',
    DIVIDEND = 'DIVIDEND',
    INTEREST = 'INTEREST'
}

export enum TransactionStatus {
    Active = 'Active',
    Cancelled = 'Cancelled'
}

export interface Transaction {
    transactionId: string
    type: TransactionType
    status: TransactionStatus
    ticker: string
    exchange: string
    assetType: string
    quantity?: number
    pricePerUnit?: number
    currency?: string
    fee?: number
    grossAmount?: number
    netAmount?: number
    taxRate?: number
    transactionDate: string
    maturityDate?: string
    notes?: string
}

export interface TransactionCreatedResponse {
    transactionId: string
    message: string
    netAmount?: number
}

export interface TransactionsListResponse {
    transactions: Transaction[]
    totalCount: number
}

// Transaction Request Types
export interface RecordBuyRequest {
    ticker: string
    exchange: string
    assetType: string
    quantity: number
    pricePerUnit: number
    currency: string
    fee?: number
    transactionDate: string
    maturityDate?: string
    notes?: string
}

export interface RecordSellRequest {
    ticker: string
    exchange: string
    assetType: string
    quantity: number
    salePrice: number
    currency: string
    fee?: number
    transactionDate: string
    notes?: string
}

export interface RecordDividendRequest {
    ticker: string
    exchange: string
    grossAmount: number
    currency: string
    paymentDate: string
    taxRate?: number
    notes?: string
}

export interface RecordInterestRequest {
    ticker: string
    exchange: string
    grossAmount: number
    currency: string
    paymentDate: string
    taxRate?: number
    notes?: string
}

export interface UpdateTransactionRequest {
    quantity?: number
    pricePerUnit?: number
    fee?: number
    grossAmount?: number
    taxRate?: number
    transactionDate?: string
    notes?: string
}

// ============================================
// POSITION TYPES
// ============================================

export interface Position {
    ticker: string
    exchange: string
    assetType: string
    totalQuantity: number
    averageCost: number
    totalCost: number
    currentPrice: number
    currentValue: number
    currency: string
    unrealizedGainLoss: number
    unrealizedGainLossPercent: number
    realizedGainLoss: number
    totalDividends: number
    totalInterest: number
    totalIncome: number
    maturityDate?: string
}

export interface PositionsSummary {
    totalValue: number
    totalCost: number
    totalUnrealizedGainLoss: number
    totalRealizedGainLoss: number
    totalDividends: number
    totalInterest: number
    currency: string
}

export interface PositionsListResponse {
    positions: Position[]
    totalCount: number
    summary: PositionsSummary
}

// ============================================
// INCOME TYPES
// ============================================

export interface IncomeBySymbol {
    ticker: string
    exchange: string
    dividends: number
    interest: number
    total: number
}

export interface IncomeByMonth {
    year: number
    month: number
    dividends: number
    interest: number
    total: number
}

export interface IncomeSummaryResponse {
    totalDividends: number
    totalInterest: number
    totalIncome: number
    currency: string
    bySymbol: IncomeBySymbol[]
    byMonth: IncomeByMonth[]
}

