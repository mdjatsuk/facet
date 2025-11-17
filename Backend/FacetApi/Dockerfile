# Define base image
# Allow overriding the registry from CI (e.g. set REGISTRY in the pipeline).
# Provide a safe default so the build doesn't produce an empty/invalid reference.
ARG REGISTRY=mcr.microsoft.com
FROM ${REGISTRY}/dotnet/sdk:9.0-alpine3.22 AS builder

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
FROM ${REGISTRY}/dotnet/aspnet:9.0-alpine3.22
WORKDIR /app
COPY --from=builder /publish .
RUN mkdir -p ./storage \
    && chmod 777 ./storage

EXPOSE 8080

RUN addgroup -S runner && adduser -S runner -G runner
USER runner
ENTRYPOINT ["dotnet", "FacetApi.dll"]
