'use client'

import { useState } from 'react'
import { useRouter } from 'next/navigation'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { toast } from 'sonner'
import { Loader2 } from 'lucide-react'
import apiClient from '@/lib/api/client'

export default function ChangePasswordPage() {
    const router = useRouter()
    const [currentPassword, setCurrentPassword] = useState('')
    const [newPassword, setNewPassword] = useState('')
    const [confirmPassword, setConfirmPassword] = useState('')
    const [isSubmitting, setIsSubmitting] = useState(false)

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault()

        if (newPassword !== confirmPassword) {
            toast.error('New passwords do not match')
            return
        }

        if (newPassword.length < 6) {
            toast.error('New password must be at least 6 characters')
            return
        }

        setIsSubmitting(true)

        try {
            await apiClient.post('/api/auth/change-password', {
                currentPassword,
                newPassword,
            })

            toast.success('Password changed successfully')
            setCurrentPassword('')
            setNewPassword('')
            setConfirmPassword('')

            // Optionally redirect after success
            setTimeout(() => router.push('/'), 2000)
        } catch (error: any) {
            toast.error(error.response?.data?.error || error.message || 'Failed to change password')
        } finally {
            setIsSubmitting(false)
        }
    }

    return (
        <div className="min-h-screen bg-background">
            {/* Header */}
            <header className="border-b bg-card">
                <div className="container mx-auto flex h-16 items-center justify-between px-4">
                    <h1 className="text-xl font-bold">InvestmentHub</h1>
                    <Button variant="outline" onClick={() => router.push('/')}>
                        Back to Dashboard
                    </Button>
                </div>
            </header>

            {/* Main Content */}
            <main className="container mx-auto p-6 max-w-md">
                <Card>
                    <CardHeader>
                        <CardTitle>Change Password</CardTitle>
                        <CardDescription>
                            Update your account password
                        </CardDescription>
                    </CardHeader>
                    <CardContent>
                        <form onSubmit={handleSubmit} className="space-y-4">
                            <div className="space-y-2">
                                <Label htmlFor="currentPassword">
                                    Current Password <span className="text-destructive">*</span>
                                </Label>
                                <Input
                                    id="currentPassword"
                                    type="password"
                                    value={currentPassword}
                                    onChange={(e) => setCurrentPassword(e.target.value)}
                                    placeholder="Enter current password"
                                    required
                                    disabled={isSubmitting}
                                />
                            </div>

                            <div className="space-y-2">
                                <Label htmlFor="newPassword">
                                    New Password <span className="text-destructive">*</span>
                                </Label>
                                <Input
                                    id="newPassword"
                                    type="password"
                                    value={newPassword}
                                    onChange={(e) => setNewPassword(e.target.value)}
                                    placeholder="Enter new password"
                                    minLength={6}
                                    required
                                    disabled={isSubmitting}
                                />
                                <p className="text-xs text-muted-foreground">
                                    Must be at least 6 characters
                                </p>
                            </div>

                            <div className="space-y-2">
                                <Label htmlFor="confirmPassword">
                                    Confirm New Password <span className="text-destructive">*</span>
                                </Label>
                                <Input
                                    id="confirmPassword"
                                    type="password"
                                    value={confirmPassword}
                                    onChange={(e) => setConfirmPassword(e.target.value)}
                                    placeholder="Confirm new password"
                                    minLength={6}
                                    required
                                    disabled={isSubmitting}
                                />
                            </div>

                            <div className="flex gap-3 pt-4">
                                <Button
                                    type="button"
                                    variant="outline"
                                    onClick={() => router.push('/')}
                                    disabled={isSubmitting}
                                    className="flex-1"
                                >
                                    Cancel
                                </Button>
                                <Button type="submit" disabled={isSubmitting} className="flex-1">
                                    {isSubmitting && (
                                        <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                                    )}
                                    Change Password
                                </Button>
                            </div>
                        </form>
                    </CardContent>
                </Card>
            </main>
        </div>
    )
}
