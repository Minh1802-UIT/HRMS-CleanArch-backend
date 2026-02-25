# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies (Docker Cache layer)
COPY ["Employee.API/Employee.API.csproj", "Employee.API/"]
COPY ["Employee.Application/Employee.Application.csproj", "Employee.Application/"]
COPY ["Employee.Domain/Employee.Domain.csproj", "Employee.Domain/"]
COPY ["Employee.Infrastructure/Employee.Infrastructure.csproj", "Employee.Infrastructure/"]
COPY ["Employee.UnitTests/Employee.UnitTests.csproj", "Employee.UnitTests/"]

RUN dotnet restore "Employee.API/Employee.API.csproj"
RUN dotnet restore "Employee.UnitTests/Employee.UnitTests.csproj"

# Copy all source code
COPY . .

# Stage 2: Test (quality gate — fails build if tests fail)
FROM build AS test
WORKDIR /src
RUN dotnet test "Employee.UnitTests/Employee.UnitTests.csproj" \
  --configuration Release \
  --no-restore \
  --verbosity normal

# Stage 3: Publish (only runs if tests pass)
FROM build AS publish
WORKDIR "/src/Employee.API"
RUN dotnet publish "Employee.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 4: Runtime (Final Image — minimal footprint)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Security: Run as non-root user; create logs directory with correct ownership
RUN adduser --disabled-password --gecos "" appuser && \
  mkdir -p /app/logs && \
  chown -R appuser:appuser /app
USER appuser

COPY --from=publish /app/publish .

# Configure runtime
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Employee.API.dll"]
