FROM mcr.microsoft.com/dotnet/core/sdk:3.1.200-alpine3.11 AS build-env
WORKDIR /app

COPY .config ./
RUN dotnet tool restore

COPY paket.* ./
RUN dotnet paket restore

COPY . ./

RUN dotnet publish -c Release -o dist ./src/GitHubStars.Backend/GitHubStars.Backend.fsproj



FROM mcr.microsoft.com/dotnet/core/aspnet:3.1.2-alpine3.11
WORKDIR /app

COPY --from=build-env /app/dist .

ENTRYPOINT ["dotnet", "GitHubStars.Backend.dll"]
