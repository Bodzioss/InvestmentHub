'use client'

import { useState } from 'react'
import { useCurrentUser, useLogout, usePortfolios } from '@/lib/hooks'
import { useAuthStore } from '@/lib/stores'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card'
import { CreatePortfolioDialog } from '@/components/portfolio/create-portfolio-dialog'
import { EditPortfolioDialog } from '@/components/portfolio/edit-portfolio-dialog'
import { ConfirmDialog } from '@/components/shared/confirm-dialog'
import { ThemeToggle } from '@/components/theme-toggle'
import { toast } from 'sonner'
import { deletePortfolio } from '@/lib/api/portfolios'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useRouter } from 'next/navigation'
import { useEffect } from 'react'
import type { Portfolio } from '@/lib/types'

export default function HomePage() {
  const user = useCurrentUser()
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated)
  const logout = useLogout()
  const router = useRouter()
  const queryClient = useQueryClient()

  // State for edit/delete dialogs
  const [editingPortfolio, setEditingPortfolio] = useState<Portfolio | null>(null)
  const [deletingPortfolio, setDeletingPortfolio] = useState<Portfolio | null>(null)

  // Get portfolios for current user
  const { data: portfolios, isLoading, error } = usePortfolios(user?.id || '')

  // Delete portfolio mutation
  const deletePortfolioMutation = useMutation({
    mutationFn: async (portfolioId: string) => {
      await deletePortfolio(portfolioId)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['portfolios'] })
      toast.success('Portfolio deleted successfully')
      setDeletingPortfolio(null)
    },
    onError: (error: Error) => {
      toast.error(`Failed to delete portfolio: ${error.message}`)
    },
  })

  // Redirect to login if not authenticated
  useEffect(() => {
    if (!isAuthenticated) {
      router.replace('/login')
    }
  }, [isAuthenticated, router])

  if (!isAuthenticated || !user) {
    return null // Loading state while redirecting
  }

  const handleEdit = (e: React.MouseEvent, portfolio: Portfolio) => {
    e.stopPropagation() // Prevent card click
    setEditingPortfolio(portfolio)
  }

  const handleDelete = (e: React.MouseEvent, portfolio: Portfolio) => {
    e.stopPropagation() // Prevent card click
    setDeletingPortfolio(portfolio)
  }

  const handleConfirmDelete = () => {
    if (deletingPortfolio) {
      deletePortfolioMutation.mutate(deletingPortfolio.id)
    }
  }

  return (
    <div className="min-h-screen bg-background">
      {/* Header */}
      <header className="border-b bg-card">
        <div className="container mx-auto flex h-16 items-center justify-between px-4">
          <div>
            <h1 className="text-xl font-bold">InvestmentHub</h1>
            <p className="text-sm text-muted-foreground">Welcome back, {user.name}!</p>
          </div>
          <div className="flex items-center gap-3">
            <ThemeToggle />
            <Button variant="outline" onClick={() => router.push('/ai')}>
              AI Analyst
            </Button>
            <Button variant="outline" onClick={() => router.push('/instruments')}>
              Instruments
            </Button>
            <Button variant="outline" onClick={() => router.push('/profile')}>
              Profile
            </Button>
            <Button variant="outline" onClick={logout}>
              Logout
            </Button>
          </div>
        </div>
      </header>

      {/* Main Content */}
      <main className="container mx-auto p-6">
        <div className="mb-6 flex items-center justify-between">
          <div>
            <h2 className="text-3xl font-bold">My Portfolios</h2>
            <p className="text-muted-foreground">
              Manage your investment portfolios
            </p>
          </div>
          <CreatePortfolioDialog>
            <Button>
              Create Portfolio
            </Button>
          </CreatePortfolioDialog>
        </div>

        {/* Loading State */}
        {isLoading && (
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
            {[1, 2, 3].map((i) => (
              <Card key={i} className="animate-pulse">
                <CardHeader className="space-y-2">
                  <div className="h-6 w-3/4 rounded bg-muted"></div>
                  <div className="h-4 w-1/2 rounded bg-muted"></div>
                </CardHeader>
                <CardContent>
                  <div className="h-20 rounded bg-muted"></div>
                </CardContent>
              </Card>
            ))}
          </div>
        )}

        {/* Error State */}
        {error && (
          <Card className="border-destructive">
            <CardHeader>
              <CardTitle className="text-destructive">Error Loading Portfolios</CardTitle>
              <CardDescription>{error.message}</CardDescription>
            </CardHeader>
          </Card>
        )}

        {/* Empty State */}
        {!isLoading && !error && portfolios?.length === 0 && (
          <Card className="border-dashed">
            <CardHeader className="text-center">
              <CardTitle>No Portfolios Yet</CardTitle>
              <CardDescription>
                Create your first portfolio to start tracking investments
              </CardDescription>
            </CardHeader>
            <CardFooter className="justify-center">
              <CreatePortfolioDialog>
                <Button>
                  Create Your First Portfolio
                </Button>
              </CreatePortfolioDialog>
            </CardFooter>
          </Card>
        )}

        {/* Portfolio Grid */}
        {!isLoading && !error && portfolios && portfolios.length > 0 && (
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
            {portfolios.map((portfolio) => (
              <Card
                key={portfolio.id}
                className="hover:shadow-lg transition-shadow cursor-pointer"
                onClick={() => router.push(`/portfolio/${portfolio.id}`)}
              >
                <CardHeader>
                  <CardTitle>{portfolio.name}</CardTitle>
                  <CardDescription>
                    {portfolio.description || 'No description'}
                  </CardDescription>
                </CardHeader>
                <CardContent className="space-y-2">
                  <div className="flex justify-between text-sm">
                    <span className="text-muted-foreground">Total Value:</span>
                    <span className="font-semibold">
                      {portfolio.totalValue?.amount.toFixed(2)} {portfolio.totalValue?.currency}
                    </span>
                  </div>
                  <div className="flex justify-between text-sm">
                    <span className="text-muted-foreground">Total Cost:</span>
                    <span>
                      {portfolio.totalCost?.amount.toFixed(2)} {portfolio.totalCost?.currency}
                    </span>
                  </div>
                  <div className="flex justify-between text-sm">
                    <span className="text-muted-foreground">Gain/Loss:</span>
                    <span className={portfolio.unrealizedGainLoss && portfolio.unrealizedGainLoss.amount >= 0 ? 'text-green-600' : 'text-red-600'}>
                      {portfolio.unrealizedGainLoss?.amount.toFixed(2)} {portfolio.unrealizedGainLoss?.currency}
                    </span>
                  </div>
                  <div className="flex justify-between text-sm">
                    <span className="text-muted-foreground">Investments:</span>
                    <span>{portfolio.activeInvestmentCount}</span>
                  </div>
                </CardContent>
                <CardFooter className="flex gap-2">
                  <Button
                    variant="outline"
                    size="sm"
                    className="flex-1"
                    onClick={() => router.push(`/portfolio/${portfolio.id}`)}
                  >
                    View
                  </Button>
                  <Button
                    variant="outline"
                    size="sm"
                    className="flex-1"
                    onClick={(e) => handleEdit(e, portfolio)}
                  >
                    Edit
                  </Button>
                  <Button
                    variant="outline"
                    size="sm"
                    className="text-destructive hover:bg-destructive hover:text-destructive-foreground"
                    onClick={(e) => handleDelete(e, portfolio)}
                  >
                    Delete
                  </Button>
                </CardFooter>
              </Card>
            ))}
          </div>
        )}
      </main>

      {/* Edit Portfolio Dialog */}
      {editingPortfolio && (
        <EditPortfolioDialog
          portfolio={editingPortfolio}
          open={!!editingPortfolio}
          onOpenChange={(open) => !open && setEditingPortfolio(null)}
        />
      )}

      {/* Delete Confirmation Dialog */}
      <ConfirmDialog
        open={!!deletingPortfolio}
        onOpenChange={(open) => !open && setDeletingPortfolio(null)}
        onConfirm={handleConfirmDelete}
        title="Delete Portfolio"
        description={`Are you sure you want to delete "${deletingPortfolio?.name}"? This action cannot be undone and will delete all associated investments.`}
        confirmText="Delete"
        cancelText="Cancel"
        variant="destructive"
      />
    </div>
  )
}
