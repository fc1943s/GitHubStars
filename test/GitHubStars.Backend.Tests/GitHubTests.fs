namespace GitHubStars.Backend

open FSharpPlus.Data
open FSharpPlus.Builders
open FSharp.Data.GraphQL
open Expecto
open Expecto.Flip

module GitHub =
    type GitHubProvider = GraphQLProvider<"../../src/GitHubStars.Backend/github_schema.json">
    
    [<Literal>]
    let StarredRepositoriesCountQuery = """
        query starredRepositoriesCount($user: String!) {
            user(login: $user) {
                starredRepositories {
                    totalCount
                }
            }
        }
    """
    
    let queryStarredRepositoriesCountAsync user token = async {
        let operation = GitHubProvider.Operation<StarredRepositoriesCountQuery> ()
        use runtimeContext = GitHub.getGitHubRuntimeContext token
        
        let! operationResult = operation.AsyncRun (runtimeContext, user)
        return defaultArg (monad {
            let! data = operationResult.Data
            let! user = data.User
            user.StarredRepositories.TotalCount
        }) 0
    }

module GitHubTests =
    let unwrapGitHubQuery fn =
        GitHub.getToken ()
        |> Core.resultOrException
        |> fn
        |> Async.RunSynchronously
        
    let tests = testList "GitHub" [
        test "Invalid user" {
            let user = "!!!"
            
            GitHub.queryStarredRepositoriesAsync user
            |> unwrapGitHubQuery
            |> function
                | Error error -> error
                | Ok _ -> ""
            |> Expect.equal "" (sprintf "Could not resolve to a User with the login of '%s'." user)
        }
        
        test "Empty repositories" {
            GitHub.queryStarredRepositoriesAsync "21"
            |> unwrapGitHubQuery
            |> Core.resultOrException
            |> Expect.isEmpty ""
            
            GitHub.queryRepositoriesByIdAsync [||]
            |> unwrapGitHubQuery
            |> Core.resultOrException
            |> Expect.isEmpty ""
        }
        
        test "Pagination queries" {
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
            |> Expect.equal "" starredRepositoriesCount
            
            let repositoriesById =
                starredRepositories
                |> List.map (fun x -> x.Id)
                |> List.toArray
                |> GitHub.queryRepositoriesByIdAsync
                |> unwrapGitHubQuery
                |> Core.resultOrException
                
            repositoriesById
            |> Expect.equal "" starredRepositories
        }
    ]

