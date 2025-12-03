param (
    [Parameter(Mandatory=$true)]
    [string]$AcrName,

    [string]$ResourceGroup = "InvestmentHub-RG",
    [string]$Location = "westeurope",
    [string]$ClusterName = "InvestmentHub-AKS"
)

$ErrorActionPreference = "Stop"

Write-Host "=== Rozpoczynanie wdrażania InvestmentHub na Azure ===" -ForegroundColor Cyan

# 1. Sprawdzenie logowania
Write-Host "1. Sprawdzanie statusu logowania Azure..." -ForegroundColor Yellow
try {
    az account show | Out-Null
} catch {
    Write-Error "Nie jesteś zalogowany do Azure CLI. Uruchom 'az login' i spróbuj ponownie."
    exit 1
}

# 2. Tworzenie Resource Group
Write-Host "2. Tworzenie grupy zasobów '$ResourceGroup' w lokalizacji '$Location'..." -ForegroundColor Yellow
az group create --name $ResourceGroup --location $Location | Out-Null

# 3. Tworzenie ACR
Write-Host "3. Tworzenie Azure Container Registry '$AcrName'..." -ForegroundColor Yellow
$acrExists = az acr check-name --name $AcrName --query "nameAvailable" -o tsv
if ($acrExists -eq "false") {
    Write-Host "ACR o nazwie '$AcrName' już istnieje (lub nazwa zajęta). Próba użycia istniejącego..." -ForegroundColor Gray
} else {
    az acr create --resource-group $ResourceGroup --name $AcrName --sku Basic --admin-enabled true | Out-Null
}

$acrLoginServer = az acr show --name $AcrName --resource-group $ResourceGroup --query "loginServer" --output tsv
Write-Host "Adres serwera logowania ACR: $acrLoginServer" -ForegroundColor Green

# 4. Tworzenie AKS
Write-Host "4. Tworzenie klastra AKS '$ClusterName' (to może potrwać kilka minut)..." -ForegroundColor Yellow
# Sprawdź czy klaster istnieje
$aksExists = az aks list --resource-group $ResourceGroup --query "[?name=='$ClusterName'] | length(@)" -o tsv
if ($aksExists -eq "0") {
    az aks create --resource-group $ResourceGroup --name $ClusterName --node-count 1 --enable-addons monitoring --generate-ssh-keys --attach-acr $AcrName | Out-Null
} else {
    Write-Host "Klaster AKS już istnieje. Aktualizacja powiązania ACR..." -ForegroundColor Gray
    az aks update --resource-group $ResourceGroup --name $ClusterName --attach-acr $AcrName | Out-Null
}

# 5. Pobranie credentials
Write-Host "5. Konfiguracja kubectl..." -ForegroundColor Yellow
az aks get-credentials --resource-group $ResourceGroup --name $ClusterName --overwrite-existing

# 6. Budowanie i wypychanie obrazów
Write-Host "6. Budowanie i wypychanie obrazów Docker..." -ForegroundColor Yellow
az acr login --name $AcrName

$images = @(
    @{ Name="investmenthub-api"; Dockerfile="src/InvestmentHub.API/Dockerfile"; Context="." },
    @{ Name="investmenthub-workers"; Dockerfile="src/InvestmentHub.Workers/Dockerfile"; Context="." },
    @{ Name="investmenthub-webclient"; Dockerfile="src/InvestmentHub.Web.Client/Dockerfile"; Context="." }
)

foreach ($img in $images) {
    $imageName = $img.Name
    $dockerfile = $img.Dockerfile
    $context = $img.Context
    $tag = "$acrLoginServer/$($imageName):latest"

    Write-Host "   - Przetwarzanie $imageName..." -ForegroundColor Cyan
    docker build -f $dockerfile -t $imageName $context
    docker tag "$($imageName):latest" $tag
    docker push $tag
}

# 7. Aktualizacja manifestów k8s
Write-Host "7. Aktualizacja manifestów Kubernetes..." -ForegroundColor Yellow
$k8sFiles = Get-ChildItem "k8s/*.yaml"
foreach ($file in $k8sFiles) {
    $content = Get-Content $file.FullName
    # Zamień placeholder lub stary ACR na nowy
    $newContent = $content -replace "image: .*investmenthub-api:latest", "image: $acrLoginServer/investmenthub-api:latest" `
                           -replace "image: .*investmenthub-workers:latest", "image: $acrLoginServer/investmenthub-workers:latest" `
                           -replace "image: .*investmenthub-webclient:latest", "image: $acrLoginServer/investmenthub-webclient:latest"
    
    if ($content -ne $newContent) {
        Set-Content -Path $file.FullName -Value $newContent
        Write-Host "   Zaktualizowano $($file.Name)" -ForegroundColor Gray
    }
}

# 8. Wdrożenie na k8s
Write-Host "8. Aplikowanie konfiguracji Kubernetes..." -ForegroundColor Yellow
kubectl apply -f k8s/

Write-Host "=== Wdrożenie zakończone sukcesem! ===" -ForegroundColor Green
Write-Host "Sprawdź status podów: kubectl get pods"
Write-Host "Sprawdź usługi: kubectl get services"
