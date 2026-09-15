# syntax=docker/dockerfile:1

ARG DOTNET_SDK_VERSION=10.0.201
ARG DOTNET_RUNTIME_VERSION=10.0.11
ARG BUILD_CONFIGURATION=Release

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_SDK_VERSION} AS restore
WORKDIR /src

COPY src/ECommerce.Domain/ECommerce.Domain.csproj src/ECommerce.Domain/
COPY src/ECommerce.Application/ECommerce.Application.csproj src/ECommerce.Application/
COPY src/ECommerce.Infrastructure/ECommerce.Infrastructure.csproj src/ECommerce.Infrastructure/
COPY src/ECommerce.Api/ECommerce.Api.csproj src/ECommerce.Api/

RUN dotnet restore src/ECommerce.Api/ECommerce.Api.csproj

FROM restore AS build
ARG BUILD_CONFIGURATION

COPY . .

RUN dotnet build src/ECommerce.Api/ECommerce.Api.csproj \
    --configuration ${BUILD_CONFIGURATION} \
    --no-restore

FROM build AS migration-bundle
ARG BUILD_CONFIGURATION

RUN dotnet tool install \
    --tool-path /tools \
    dotnet-ef \
    --version 10.0.11

RUN mkdir -p /app \
    && /tools/dotnet-ef migrations bundle \
    --project src/ECommerce.Infrastructure/ECommerce.Infrastructure.csproj \
    --startup-project src/ECommerce.Api/ECommerce.Api.csproj \
    --configuration ${BUILD_CONFIGURATION} \
    --no-build \
    --output /app/efbundle

FROM build AS publish
ARG BUILD_CONFIGURATION

RUN dotnet publish src/ECommerce.Api/ECommerce.Api.csproj \
    --configuration ${BUILD_CONFIGURATION} \
    --no-build \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_RUNTIME_VERSION} AS runtime-base
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /app/data \
    && chown -R "${APP_UID}" /app/data

FROM runtime-base AS migrations

COPY --from=migration-bundle /app/efbundle ./efbundle
COPY src/ECommerce.Api/appsettings.json ./appsettings.json

USER ${APP_UID}

ENTRYPOINT ["./efbundle"]

FROM runtime-base AS final

COPY --from=publish /app/publish .

USER ${APP_UID}

ENTRYPOINT ["dotnet", "ECommerce.Api.dll"]
