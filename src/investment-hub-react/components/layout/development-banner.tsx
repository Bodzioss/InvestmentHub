'use client'

import { AlertTriangle } from 'lucide-react'

export function DevelopmentBanner() {
    return (
        <div className="fixed bottom-0 left-0 right-0 z-50 flex items-center justify-center gap-2 bg-amber-500/90 py-2 px-4 text-center text-sm font-semibold text-amber-950 backdrop-blur-sm dark:bg-amber-600/90 dark:text-amber-50 shadow-lg">
            <AlertTriangle className="h-4 w-4" />
            <span>
                Development Preview: Features may be unstable. Data persistence is not guaranteed.
            </span>
            <AlertTriangle className="h-4 w-4" />
        </div>
    )
}
