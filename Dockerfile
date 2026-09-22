# syntax=docker/dockerfile:1.7
# Multi-stage build: the SDK image restores and publishes, the runtime image ships only the published output.
# Runtime base is the chiseled "extra" variant: distroless-style (no shell, no package manager), runs as the
# non-root `app` user, and includes ICU + tzdata. ICU is required: identifier normalisation relies on NFKC.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so the layer is cached until a project file or package version changes.
COPY .editorconfig Directory.Build.props Directory.Packages.props ./
COPY DFDS.SmartGate.Api/DFDS.SmartGate.Api.csproj DFDS.SmartGate.Api/
COPY src/DFDS.SmartGate.Domain/DFDS.SmartGate.Domain.csproj src/DFDS.SmartGate.Domain/
COPY src/DFDS.SmartGate.Application/DFDS.SmartGate.Application.csproj src/DFDS.SmartGate.Application/
COPY src/DFDS.SmartGate.Infrastructure/DFDS.SmartGate.Infrastructure.csproj src/DFDS.SmartGate.Infrastructure/
RUN dotnet restore DFDS.SmartGate.Api/DFDS.SmartGate.Api.csproj

COPY DFDS.SmartGate.Api/ DFDS.SmartGate.Api/
COPY src/ src/
RUN dotnet publish DFDS.SmartGate.Api/DFDS.SmartGate.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "DFDS.SmartGate.Api.dll"]
