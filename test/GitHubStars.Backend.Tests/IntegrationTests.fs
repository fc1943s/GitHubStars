namespace GitHubStars.Backend.Tests

open FsUnit
open FsUnit.Xunit
open GitHubStars.Backend
open FSharpPlus.Data
open GitHubStars.Shared.Model
open FSharpPlus.Builders
open FSharp.Data.GraphQL
open Xunit
open Xunit.Abstractions

module GitHub =
    type GitHubProvider = GraphQLProvider<"../../src/GitHubStars.Backend/github_schema.json">
    
    [<Literal>]
    let starredRepositoriesCountQuery = """
        query starredRepositoriesCount($user: String!) {
            user(login: $user) {
                starredRepositories {
                    totalCount
                }
            }
        }
    """
    
    let queryStarredRepositoriesCountAsync user token = async {
        let operation = GitHubProvider.Operation<starredRepositoriesCountQuery> ()
        use runtimeContext = GitHub.getGitHubRuntimeContext token
        
        let! operationResult = operation.AsyncRun (runtimeContext, user)
        return defaultArg (monad {
            let! data = operationResult.Data
            let! user = data.User
            user.StarredRepositories.TotalCount
        }) 0
    }

module IntegrationTests =
    let unwrapGitHubQuery fn =
        GitHub.getToken ()
        |> Core.resultOrException
        |> fn
        |> Async.RunSynchronously
        
    type IntegrationTests (output: ITestOutputHelper) =
        
        [<Fact>]
        member _.InvalidUserTest () =
            let user = "!!!"
            
            GitHub.queryStarredRepositoriesAsync user
            |> unwrapGitHubQuery
            |> function
                | Error error -> error
                | Ok _ -> ""
            |> should equal (sprintf "Could not resolve to a User with the login of '%s'." user)
                
            
        [<Fact>]
        member _.RepositoryCountTest () =
            let user = "fc1943s"
            
            let starredRepositories =
                GitHub.queryStarredRepositoriesAsync user
                |> unwrapGitHubQuery
                |> Core.resultOrException
                
            let starredRepositoriesCount =
                GitHub.queryStarredRepositoriesCountAsync user
                |> unwrapGitHubQuery
                
            starredRepositories
            |> List.length
            |> should equal starredRepositoriesCount

