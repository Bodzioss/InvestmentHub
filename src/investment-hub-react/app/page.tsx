import { Button } from "@/components/ui/button"

export default function Home() {
  return (
    <main className="flex min-h-screen flex-col items-center justify-center p-24">
      <div className="text-center space-y-4">
        <h1 className="text-4xl font-bold">InvestmentHub React 🚀</h1>
        <p className="text-xl text-muted-foreground">Migracja rozpoczęta!</p>
        <div className="flex gap-4 justify-center">
          <Button>Zaloguj się</Button>
          <Button variant="outline">Dowiedz się więcej</Button>
        </div>
      </div>
    </main>
  )
}
