# GitHubStars

The project is written in F# but it uses React internally for rendering (Functional components + Hooks), and an architecture similar to Redux for state management (Elmish). The API usage from the UI is minimal for the sake of simplicity (GET and PUT methods only).

## Execution

1. `EXPORT GITHUB_API_TOKEN=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx` to allow interaction with the public GitHub API from the integration tests and backend 
2. `EXPORT API_URL=http://xxx.xxx.xxx.xxx:8086` to compile the frontend container with the correct API URL hardcoded (some environments don't have "localhost" bound as the Docker host)
3. `docker-compose -f docker-compose.integration-tests.yml up --build` to execute the tests
4. `docker-compose -f docker-compose.yml up --build` to execute the application

#### Details

- Database: `DOCKER_HOST:5432`
- Backend: `http://DOCKER_HOST:8086`
- Frontend: `http://DOCKER_HOST:8087`

## Development environment configuration

1. Install [.NET Core SDK 3.1](https://dotnet.microsoft.com/download/dotnet-core) and [yarn](https://yarnpkg.com/lang/en/docs/install/)
3. Run `dotnet tool restore` to install all developer tools required to build the project
4. Run `dotnet paket restore` and `yarn --cwd ./src/GitHubStars.Frontend install` to download all package dependencies
5. `EXPORT GITHUB_API_TOKEN=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx` to allow interaction with the public GitHub API
6. `dotnet run -p ./test/GitHubStars.Backend.Tests/GitHubStars.Backend.Tests.fsproj` to run the tests
7. `dotnet fsharplint lint GitHubStars.sln --lint-config ./fsharplint.json` to run the linter (*Windows only*. it can also be configured into an IDE)



