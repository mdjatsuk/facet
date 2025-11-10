# Define base image
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine3.22 AS builder

# Copy project files
WORKDIR /source
COPY ./*.csproj .

# Restore
RUN dotnet restore

# Copy all source code
COPY . .

# Publish
WORKDIR /source
RUN dotnet publish "FacetApi.csproj" -c Release -o /publish

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine3.22
WORKDIR /app
COPY --from=builder /publish .

EXPOSE 80

RUN addgroup -S runner && adduser -S runner -G runner
USER runner
ENTRYPOINT ["dotnet", "FacetApi.dll"]
