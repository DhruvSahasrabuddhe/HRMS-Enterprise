# ─────────────────────────────────────────────────────────────────────────────
# Stage 1: SDK – restore and build
# ─────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_VERSION=local

WORKDIR /src

# Copy solution and project files first to maximise layer-cache reuse
COPY HRMS.sln ./
COPY src/HRMS.Shared/HRMS.Shared.csproj              src/HRMS.Shared/
COPY src/HRMS.Core/HRMS.Core.csproj                  src/HRMS.Core/
COPY src/HRMS.Infrastructure/HRMS.Infrastructure.csproj src/HRMS.Infrastructure/
COPY src/HRMS.Services/HRMS.Services.csproj          src/HRMS.Services/
COPY src/HRMS.Web/HRMS.Web.csproj                    src/HRMS.Web/
COPY tests/HRMS.UnitTests/HRMS.UnitTests.csproj      tests/HRMS.UnitTests/
COPY tests/HRMS.IntegrationTests/HRMS.IntegrationTests.csproj tests/HRMS.IntegrationTests/

# Restore packages (cached separately from source)
RUN dotnet restore HRMS.sln

# Copy the full source
COPY . .

# Publish the web application
RUN dotnet publish src/HRMS.Web/HRMS.Web.csproj \
        --configuration Release \
        --no-restore \
        --output /app/publish \
        -p:UseAppHost=false \
        -p:BuildVersion=${BUILD_VERSION}

# ─────────────────────────────────────────────────────────────────────────────
# Stage 2: Runtime – lean ASP.NET image
# ─────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# Install curl for health-check scripts and tini as PID-1 init
RUN apt-get update && apt-get install -y --no-install-recommends \
        curl \
        tini \
    && rm -rf /var/lib/apt/lists/*

# Create non-root user for the application
RUN groupadd --gid 1001 hrms \
 && useradd  --uid 1001 --gid hrms --shell /bin/bash --create-home hrms

WORKDIR /app

# Copy published artefacts from build stage
COPY --from=build --chown=hrms:hrms /app/publish .

# Copy entrypoint script
COPY --chown=hrms:hrms scripts/entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

# Create writable log directory
RUN mkdir -p /app/logs && chown hrms:hrms /app/logs

# Drop root privileges
USER hrms

# Expose HTTP port (HTTPS termination handled by the ingress/load-balancer)
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Use tini to handle signals correctly and avoid zombie processes
ENTRYPOINT ["/usr/bin/tini", "--", "/entrypoint.sh"]
