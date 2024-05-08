# Use the official Microsoft .NET SDK image to create a build environment.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Copy the csproj file and restore any dependencies (via NuGet)
COPY *.csproj ./
RUN dotnet restore --verbosity detailed

# Copy the remaining files and build the application
COPY . ./
RUN dotnet publish -c Release -o out

# Generate the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "getting-started-jaeger.dll"]
