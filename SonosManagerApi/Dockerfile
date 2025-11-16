# Use the official .NET 8 SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy the csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build the application
COPY . ./
RUN dotnet publish -c Release -o out

# Use the official .NET 8 runtime image to run the application
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Install iproute2 and iputils-ping in the runtime stage
RUN apt-get update && apt-get install -y iproute2 iputils-ping

COPY --from=build /app/out .

# Set the environment variable to specify the port
ENV ASPNETCORE_URLS=http://+:8080

# Expose port 8080
EXPOSE 8080

# Set the entry point for the container
ENTRYPOINT ["dotnet", "SonosManagerApi.dll"]
