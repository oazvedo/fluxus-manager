# Imagem da API. Compila na arquitetura da máquina de build e gera binário para a arquitetura alvo
# (ex.: build em amd64 gerando imagem arm64), sem emulação.
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /src

# Restore em camada própria: só refaz quando os .csproj mudam.
COPY FluxusManager.sln ./
COPY src/FluxusManager.Domain/FluxusManager.Domain.csproj src/FluxusManager.Domain/
COPY src/FluxusManager.Application/FluxusManager.Application.csproj src/FluxusManager.Application/
COPY src/FluxusManager.Infrastructure/FluxusManager.Infrastructure.csproj src/FluxusManager.Infrastructure/
COPY src/FluxusManager.API/FluxusManager.API.csproj src/FluxusManager.API/
RUN dotnet restore src/FluxusManager.API/FluxusManager.API.csproj -a $TARGETARCH

COPY src/ src/
RUN dotnet publish src/FluxusManager.API/FluxusManager.API.csproj -c Release -a $TARGETARCH --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID

ENTRYPOINT ["dotnet", "FluxusManager.API.dll"]
