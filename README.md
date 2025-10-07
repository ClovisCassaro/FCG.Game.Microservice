# FCG Game Microservice

Microsserviço de Jogos usando .NET 8, Elasticsearch e EventStore.

## Tecnologias

- .NET 8
- Elasticsearch
- EventStore
- Docker
- Redis

## Como executar

1. Iniciar containers:
```bash
docker-compose up -d
```

2. Executar a aplicação:
```bash
dotnet run --project FCG.Game.API
```

3. Acessar Swagger:
```
http://localhost:5002/swagger
```

4. Acessar Kibana:
```
http://localhost:5601
```

## Estrutura

- **FCG.Game.API**: Controllers e configuração da API
- **FCG.Game.Application**: Serviços e lógica de aplicação
- **FCG.Game.Domain**: Entidades e eventos de domínio
- **FCG.Game.Infrastructure**: Repositórios e integrações
- **FCG.Game.Tests**: Testes unitários e de integração
