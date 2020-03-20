FROM mcr.microsoft.com/dotnet/core/sdk:3.1.200-alpine3.11 AS build-env
WORKDIR /app

COPY .config ./
RUN dotnet tool restore

COPY paket.* ./
RUN dotnet paket restore

COPY . ./
RUN dotnet publish -c Release -o dist ./test/GitHubStars.Backend.Tests/GitHubStars.Backend.Tests.fsproj



FROM mcr.microsoft.com/dotnet/core/aspnet:3.1.2-alpine3.11
WORKDIR /app

RUN echo -e "#!/bin/sh" > /usr/bin/docker && chmod +x /usr/bin/docker

COPY --from=build-env /app/dist .

ENTRYPOINT ["dotnet", "GitHubStars.Backend.Tests.dll"]
