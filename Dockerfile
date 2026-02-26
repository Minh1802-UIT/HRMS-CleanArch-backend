FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["Employee.API/Employee.API.csproj", "Employee.API/"]
COPY ["Employee.Application/Employee.Application.csproj", "Employee.Application/"]
COPY ["Employee.Domain/Employee.Domain.csproj", "Employee.Domain/"]
COPY ["Employee.Infrastructure/Employee.Infrastructure.csproj", "Employee.Infrastructure/"]

RUN dotnet restore "Employee.API/Employee.API.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/Employee.API"
RUN dotnet build "Employee.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Employee.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=publish /app/publish .

# Expose port 80 for Render default routing
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Employee.API.dll"]
