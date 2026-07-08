# Base image for running the app
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5234
ENV ASPNETCORE_URLS=http://+:5234

# SDK image for building the app
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["MyBackend.csproj", "./"]
RUN dotnet restore "./MyBackend.csproj"
COPY . .
RUN dotnet build "MyBackend.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MyBackend.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final production stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyBackend.dll"]
