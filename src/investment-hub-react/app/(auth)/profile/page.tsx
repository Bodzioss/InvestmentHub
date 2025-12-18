'use client'

import { useState } from 'react'
import { useRouter } from 'next/navigation'
import { useCurrentUser } from '@/lib/hooks'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { toast } from 'sonner'
import { Loader2 } from 'lucide-react'
import apiClient from '@/lib/api/client'

export default function ProfilePage() {
    const user = useCurrentUser()
    const router = useRouter()
    const [name, setName] = useState(user?.name || '')
    const [isSubmitting, setIsSubmitting] = useState(false)

    if (!user) {
        return null
    }

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault()

        if (!name.trim()) {
            toast.error('Name is required')
            return
        }

        setIsSubmitting(true)

        try {
            await apiClient.put('/api/auth/profile', {
                name: name.trim(),
            })

            toast.success('Profile updated successfully')
        } catch (error: any) {
            toast.error(error.response?.data?.error || error.message || 'Failed to update profile')
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
            <main className="container mx-auto p-6 max-w-2xl">
                <div className="space-y-6">
                    {/* Profile Information */}
                    <Card>
                        <CardHeader>
                            <CardTitle>Profile Information</CardTitle>
                            <CardDescription>
                                View and update your account details
                            </CardDescription>
                        </CardHeader>
                        <CardContent>
                            <form onSubmit={handleSubmit} className="space-y-4">
                                <div className="space-y-2">
                                    <Label htmlFor="name">
                                        Name <span className="text-destructive">*</span>
                                    </Label>
                                    <Input
                                        id="name"
                                        type="text"
                                        value={name}
                                        onChange={(e) => setName(e.target.value)}
                                        placeholder="Your name"
                                        required
                                        disabled={isSubmitting}
                                    />
                                </div>

                                <div className="space-y-2">
                                    <Label htmlFor="email">Email</Label>
                                    <Input
                                        id="email"
                                        type="email"
                                        value={user.email}
                                        disabled
                                        className="bg-muted"
                                    />
                                    <p className="text-xs text-muted-foreground">
                                        Email cannot be changed
                                    </p>
                                </div>

                                <div className="space-y-2">
                                    <Label>User ID</Label>
                                    <Input
                                        type="text"
                                        value={user.id}
                                        disabled
                                        className="bg-muted font-mono text-xs"
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
                                        Save Changes
                                    </Button>
                                </div>
                            </form>
                        </CardContent>
                    </Card>

                    {/* Security */}
                    <Card>
                        <CardHeader>
                            <CardTitle>Security</CardTitle>
                            <CardDescription>
                                Manage your account security settings
                            </CardDescription>
                        </CardHeader>
                        <CardContent>
                            <Button
                                variant="outline"
                                onClick={() => router.push('/change-password')}
                            >
                                Change Password
                            </Button>
                        </CardContent>
                    </Card>
                </div>
            </main>
        </div>
    )
}
