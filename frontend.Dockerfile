FROM mcr.microsoft.com/dotnet/core/sdk:3.1.200-alpine3.11 AS build-env
WORKDIR /app

COPY .config ./
RUN dotnet tool restore

COPY paket.* ./
RUN dotnet paket restore

RUN apk add yarn

WORKDIR ./src/GitHubStars.Frontend

COPY ./src/GitHubStars.Frontend/package.json ./
COPY ./src/GitHubStars.Frontend/yarn.lock ./
RUN yarn install

COPY . /app/

ARG API_URL
RUN yarn run webpack -p


FROM node:13.10.1-alpine3.11
WORKDIR /app

RUN npm install -g serve

COPY --from=build-env /app/src/GitHubStars.Frontend/dist .

ENTRYPOINT ["serve", "-l", "8087"]

