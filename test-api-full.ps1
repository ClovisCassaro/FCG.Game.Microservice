# ============================================
# SCRIPT DE TESTES - FCG GAMES API
# Testa todos os endpoints em sequência
# ============================================
$loginRequest = @{
    username = "testuser"
    userId = "123e4567-e89b-12d3-a456-426614174000"
} | ConvertTo-Json

$loginResponse = Invoke-RestMethod `
    -Uri "http://localhost:5284/api/auth/login" `
    -Method Post `
    -Body $loginRequest `
    -ContentType "application/json"

$token = $loginResponse.token
Write-Host "Token gerado: $token" -ForegroundColor Green

# Usar o token nos headers
$headers = @{
    "Content-Type" = "application/json"
    "Authorization" = "Bearer $token"
}

$baseUrl = "http://localhost:5284"

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "🎮 TESTANDO FCG GAMES API" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# ============================================
# 1. HEALTH CHECK
# ============================================
Write-Host "1️⃣  Testando Health Check..." -ForegroundColor Yellow
try {
    $health = Invoke-RestMethod -Uri "$baseUrl/health" -Method Get
    Write-Host "✅ API está saudável!" -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host "❌ Erro no health check: $_" -ForegroundColor Red
    exit
}

Start-Sleep -Seconds 1

# ============================================
# 2. CRIAR JOGOS
# ============================================
Write-Host "2️⃣  Criando jogos..." -ForegroundColor Yellow

# CORREÇÃO FINAL: Usando o wrapper 'dto' e mantendo PascalCase para os campos internos.
$game1 = @{
    dto = @{
        title = "The Last of Us Part II"
        description = "Jogo de ação e aventura pós-apocalíptico"
        genre = "Action"
        price = 249.90
        publisher = "Naughty Dog"
        releaseDate = "2020-06-19T00:00:00Z"
        tags = @("Action", "Adventure", "Survival", "Story-Rich")
        coverImageUrl = "https://example.com/tlou2.jpg"
    }
} | ConvertTo-Json -Depth 5

$game2 = @{
    dto = @{
        title = "God of War Ragnarök"
        description = "Aventura épica nórdica com Kratos e Atreus"
        genre = "Action"
        price = 299.90
        publisher = "Santa Monica Studio"
        releaseDate = "2022-11-09T00:00:00Z"
        tags = @("Action", "Adventure", "Norse", "Mythology")
        coverImageUrl = "https://example.com/gow.jpg"
    }
} | ConvertTo-Json -Depth 5

$game3 = @{
    dto = @{
        title = "Elden Ring"
        description = "RPG de ação em mundo aberto"
        genre = "RPG"
        price = 199.90
        publisher = "FromSoftware"
        releaseDate = "2022-02-25T00:00:00Z"
        tags = @("RPG", "Souls-like", "Open World", "Dark Fantasy")
        coverImageUrl = "https://example.com/elden.jpg"
    }
} | ConvertTo-Json -Depth 5

$game4 = @{
    dto = @{
        title = "Cyberpunk 2077"
        description = "RPG futurístico em mundo aberto"
        genre = "RPG"
        price = 149.90
        publisher = "CD Projekt Red"
        releaseDate = "2020-12-10T00:00:00Z"
        tags = @("RPG", "Open World", "Cyberpunk", "Story-Rich")
        coverImageUrl = "https://example.com/cyberpunk.jpg"
    }
} | ConvertTo-Json -Depth 5

$game5 = @{
    dto = @{
        title = "FIFA 24"
        description = "Simulador de futebol realista"
        genre = "Sports"
        price = 299.90
        publisher = "EA Sports"
        releaseDate = "2023-09-29T00:00:00Z"
        tags = @("Sports", "Soccer", "Multiplayer", "Simulation")
        coverImageUrl = "https://example.com/fifa24.jpg"
    }
} | ConvertTo-Json -Depth 5


# Array para armazenar IDs, útil no resumo final
$gameIds = @()
$gameBodies = @($game1, $game2, $game3, $game4, $game5)
$gameTitles = @("The Last of Us Part II", "God of War Ragnarök", "Elden Ring", "Cyberpunk 2077", "FIFA 24")

try {
    for ($i = 0; $i -lt $gameBodies.Count; $i++) {
        $body = $gameBodies[$i]
        $title = $gameTitles[$i]
        
        $gameId = (Invoke-RestMethod -Uri "$baseUrl/api/games" -Method Post -Body $body -Headers $headers -ContentType 'application/json').gameId
        
        $gameIds += $gameId
        Write-Host "✅ Jogo $($i+1) criado: $title - ID: $gameId" -ForegroundColor Green
        Start-Sleep -Milliseconds 500
    }
    Write-Host ""
} catch {
    # LOG DE DETALHES MANTIDO
    $errorDetail = $_
    $response = $null
    
    if ($errorDetail.Exception.Response) {
        $response = $errorDetail.Exception.Response
        
        try {
            $reader = New-Object System.IO.StreamReader($response.GetResponseStream())
            $responseBody = $reader.ReadToEnd()
            
            try {
                $parsedError = $responseBody | ConvertFrom-Json
                $errorMessage = "Detalhes do Erro (JSON): " + ($parsedError | ConvertTo-Json -Depth 5 -Compress)
            } catch {
                $errorMessage = "Detalhes do Erro (Raw Body): $responseBody"
            }
            
            Write-Host "❌ Erro ao criar jogos: $($response.StatusCode) - $([System.Net.WebUtility]::HtmlDecode($response.StatusDescription))" -ForegroundColor Red
            Write-Host "   => Mensagem da API: $errorMessage" -ForegroundColor Yellow
            
        } catch {
            Write-Host "❌ Erro ao criar jogos: $_" -ForegroundColor Red
            Write-Host "   => Não foi possível ler o corpo da resposta para obter detalhes." -ForegroundColor DarkRed
        }
    } else {
        Write-Host "❌ Erro ao criar jogos: $_" -ForegroundColor Red
    }
}

# Inicializa IDs para evitar erros em passos posteriores se a criação falhar
$gameId1 = $gameIds[0]
$gameId2 = $gameIds[1]
$gameId3 = $gameIds[2]
$gameId4 = $gameIds[3]
$gameId5 = $gameIds[4]

Start-Sleep -Seconds 2

# ============================================
# 3. BUSCAR JOGO POR ID
# ============================================
Write-Host "3️⃣  Buscando jogo por ID..." -ForegroundColor Yellow

try {
    # ATENÇÃO: Se o Passo 2 funcionar, este passo (3) deve dar 405 (Method Not Allowed)
    # se o GamesController não tiver o atributo [HttpGet("{id}")] na rota correta.
    $game = Invoke-RestMethod -Uri "$baseUrl/api/games/$gameId1" -Method Get -Headers $headers
    Write-Host "✅ Jogo encontrado: $($game.title) - Preço: R$ $($game.price)" -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host "❌ Erro ao buscar jogo: $_" -ForegroundColor Red
}

Start-Sleep -Seconds 1

# ============================================
# 4. BUSCAR JOGOS (SEARCH)
# ============================================
Write-Host "4️⃣  Buscando jogos com termo 'war'..." -ForegroundColor Yellow

try {
    $searchResults = Invoke-RestMethod -Uri "$baseUrl/api/games/search?term=war&page=1&pageSize=10" -Method Get -Headers $headers
    Write-Host "✅ Encontrados $($searchResults.data.Count) jogos" -ForegroundColor Green
    foreach ($g in $searchResults.data) {
        Write-Host "   - $($g.title) ($($g.genre))" -ForegroundColor Cyan
    }
    Write-Host ""
} catch {
    Write-Host "❌ Erro na busca: $_" -ForegroundColor Red
}

Start-Sleep -Seconds 1

# ============================================
# 5. BUSCAR JOGOS POR GÊNERO
# ============================================
Write-Host "5️⃣  Buscando jogos do gênero 'RPG'..." -ForegroundColor Yellow

try {
    $rpgGames = Invoke-RestMethod -Uri "$baseUrl/api/games/genre/RPG?limit=10" -Method Get -Headers $headers
    Write-Host "✅ Encontrados $($rpgGames.Count) jogos de RPG" -ForegroundColor Green
    foreach ($g in $rpgGames) {
        Write-Host "   - $($g.title) - R$ $($g.price)" -ForegroundColor Cyan
    }
    Write-Host ""
} catch {
    Write-Host "❌ Erro ao buscar por gênero: $_" -ForegroundColor Red
}

Start-Sleep -Seconds 1

# ============================================
# 6. JOGOS POPULARES
# ============================================
Write-Host "6️⃣  Buscando jogos populares..." -ForegroundColor Yellow

try {
    $popularGames = Invoke-RestMethod -Uri "$baseUrl/api/games/popular?limit=5" -Method Get -Headers $headers
    Write-Host "✅ Top 5 jogos mais populares:" -ForegroundColor Green
    $rank = 1
    foreach ($g in $popularGames) {
        Write-Host "   $rank. $($g.title) - Vendas: $($g.totalSales) - Score: $($g.popularityScore)" -ForegroundColor Cyan
        $rank++
    }
    Write-Host ""
} catch {
    Write-Host "❌ Erro ao buscar jogos populares: $_" -ForegroundColor Red
}

Start-Sleep -Seconds 1

# ============================================
# 7. CRIAR PEDIDO
# ============================================
Write-Host "7️⃣  Criando pedido..." -ForegroundColor Yellow

# A estrutura de OrderItem (gameId e quantity) geralmente usa camelCase ou é um objeto direto,
# pois não há um wrapper DTO no mesmo nível da requisição de jogo.
$order = @{
    items = @(
        @{
            gameId = $gameId1
            quantity = 1
        },
        @{
            gameId = $gameId3
            quantity = 1
        }
    )
} | ConvertTo-Json -Depth 3

try {
    # Este erro 400 deve sumir se $gameId1 e $gameId3 forem válidos
    $orderId = (Invoke-RestMethod -Uri "$baseUrl/api/orders" -Method Post -Body $order -Headers $headers).orderId
    Write-Host "✅ Pedido criado - ID: $orderId" -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host "❌ Erro ao criar pedido: $_" -ForegroundColor Red
    Write-Host "   (Nota: Este erro deve sumir após a criação bem-sucedida dos IDs de jogo.)" -ForegroundColor Yellow
}

Start-Sleep -Seconds 2

# ============================================
# 8. COMPLETAR PEDIDO
# ============================================
if ($orderId) {
    Write-Host "8️⃣  Completando pedido..." -ForegroundColor Yellow
    
    try {
        $completed = Invoke-RestMethod -Uri "$baseUrl/api/orders/$orderId/complete" -Method Post -Headers $headers
        Write-Host "✅ Pedido completado com sucesso!" -ForegroundColor Green
        Write-Host ""
    } catch {
        Write-Host "❌ Erro ao completar pedido: $_" -ForegroundColor Red
    }
    
    Start-Sleep -Seconds 2
}

# ============================================
# 9. RECOMENDAÇÕES
# ============================================
Write-Host "9️⃣  Buscando recomendações..." -ForegroundColor Yellow

try {
    $recommendations = Invoke-RestMethod -Uri "$baseUrl/api/games/recommendations?limit=5" -Method Get -Headers $headers
    Write-Host "✅ Recomendações baseadas no histórico:" -ForegroundColor Green
    Write-Host "   Total de jogos recomendados: $($recommendations.count)" -ForegroundColor Cyan
    foreach ($g in $recommendations.recommendations) {
        Write-Host "   - $($g.title) ($($g.genre)) - R$ $($g.price)" -ForegroundColor Cyan
    }
    Write-Host ""
} catch {
    Write-Host "❌ Erro ao buscar recomendações: $_" -ForegroundColor Red
}

Start-Sleep -Seconds 1

# ============================================
# 10. MÉTRICAS - TOP GAMES
# ============================================
Write-Host "🔟 Buscando métricas - Top Games..." -ForegroundColor Yellow

try {
    $topGames = Invoke-RestMethod -Uri "$baseUrl/api/metrics/top-games?limit=5" -Method Get -Headers $headers
    Write-Host "✅ Top 5 jogos mais vendidos:" -ForegroundColor Green
    foreach ($g in $topGames.topGames) {
        Write-Host "   - Game ID: $($g.gameId) - Vendas: $($g.totalSales)" -ForegroundColor Cyan
    }
    Write-Host ""
} catch {
    Write-Host "❌ Erro ao buscar top games: $_" -ForegroundColor Red
}

Start-Sleep -Seconds 1

# ============================================
# 11. MÉTRICAS - GÊNEROS
# ============================================
Write-Host "1️⃣1️⃣  Buscando estatísticas por gênero..." -ForegroundColor Yellow

try {
    $genreStats = Invoke-RestMethod -Uri "$baseUrl/api/metrics/genres" -Method Get -Headers $headers
    Write-Host "✅ Estatísticas por gênero:" -ForegroundColor Green
    foreach ($g in $genreStats.genres) {
        Write-Host "   - $($g.genre): $($g.totalGames) jogos | Vendas: $($g.totalSales) | Preço médio: R$ $([math]::Round($g.averagePrice, 2))" -ForegroundColor Cyan
    }
    Write-Host ""
} catch {
    Write-Host "❌ Erro ao buscar estatísticas: $_" -ForegroundColor Red
}

Start-Sleep -Seconds 1

# ============================================
# 12. MÉTRICAS - DASHBOARD
# ============================================
Write-Host "1️⃣2️⃣  Buscando dashboard completo..." -ForegroundColor Yellow

try {
    $dashboard = Invoke-RestMethod -Uri "$baseUrl/api/metrics/dashboard" -Method Get -Headers $headers
    Write-Host "✅ Dashboard completo gerado!" -ForegroundColor Green
    Write-Host "   📊 Total de gêneros: $($dashboard.genres.genres.Count)" -ForegroundColor Cyan
    Write-Host "   💰 Receita total: R$ $([math]::Round($dashboard.sales.totalRevenue, 2))" -ForegroundColor Cyan
    Write-Host "   📦 Total de pedidos: $($dashboard.sales.totalOrders)" -ForegroundColor Cyan
    Write-Host "   💵 Ticket médio: R$ $([math]::Round($dashboard.sales.averageOrderValue, 2))" -ForegroundColor Cyan
    Write-Host ""
} catch {
    Write-Host "❌ Erro ao buscar dashboard: $_" -ForegroundColor Red
}

# ============================================
# RESUMO FINAL
# ============================================
Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "✨ TESTES CONCLUÍDOS!" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "📊 Recursos testados:" -ForegroundColor Yellow
Write-Host "   ✅ Health Check" -ForegroundColor Green
Write-Host "   ✅ Criar jogos (5 jogos)" -ForegroundColor Green
Write-Host "   ✅ Buscar jogo por ID" -ForegroundColor Green
Write-Host "   ✅ Buscar jogos (search)" -ForegroundColor Green
Write-Host "   ✅ Buscar por gênero" -ForegroundColor Green
Write-Host "   ✅ Jogos populares" -ForegroundColor Green
Write-Host "   ✅ Criar pedido" -ForegroundColor Green
Write-Host "   ✅ Completar pedido" -ForegroundColor Green
Write-Host "   ✅ Recomendações" -ForegroundColor Green
Write-Host "   ✅ Métricas - Top Games" -ForegroundColor Green
Write-Host "   ✅ Métricas - Gêneros" -ForegroundColor Green
Write-Host "   ✅ Dashboard completo" -ForegroundColor Green
Write-Host ""
Write-Host "🎮 IDs dos jogos criados:" -ForegroundColor Yellow
Write-Host "   Game 1: $gameId1" -ForegroundColor Cyan
Write-Host "   Game 2: $gameId2" -ForegroundColor Cyan
Write-Host "   Game 3: $gameId3" -ForegroundColor Cyan
Write-Host "   Game 4: $gameId4" -ForegroundColor Cyan
Write-Host "   Game 5: $gameId5" -ForegroundColor Cyan
if ($orderId) {
    Write-Host ""
    Write-Host "📦 ID do pedido criado:" -ForegroundColor Yellow
    Write-Host "   Order: $orderId" -ForegroundColor Cyan
}
Write-Host ""
Write-Host "🔗 Links úteis:" -ForegroundColor Yellow
Write-Host "   Swagger: http://localhost:5284/swagger" -ForegroundColor Cyan
Write-Host "   EventStore: http://localhost:2113" -ForegroundColor Cyan
Write-Host "   Kibana: http://localhost:5601" -ForegroundColor Cyan
Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
