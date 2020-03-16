namespace GitHubStars.Backend

open System
open GitHubStars.Shared.Model
open FSharpPlus.Builders
open FSharpPlus.Data
open FSharp.Data.GraphQL

module GitHub =
    
    // Since F# is a statically typed language, the GitHub API JSON Schema is stored in the repository
    // where the GraphQL query will be parsed at compilation time by the provider
    type GitHubProvider = GraphQLProvider<"github_schema.json">
    
    [<Literal>]
    let starredRepositoriesQuery = """
        query starredRepositories($user: String!, $after: String) {
            user(login: $user) {
                starredRepositories(orderBy: {field: STARRED_AT, direction: DESC}, first: 100, after: $after) {
                    nodes {
                        id
                        name
                        description
                        url
                        languages(first: 1, orderBy: {field: SIZE, direction: DESC}) {
                            nodes {
                                name
                            }
                        }
                    }
                    pageInfo {
                        endCursor
                        hasNextPage
                    }
                }
            }
        }
    """
    
    let getGitHubRuntimeContext token =
        let headers = [ "Authorization", "bearer " + token
                        "User-Agent", "GitHubStars" ]
        GitHubProvider.GetContext ("https://api.github.com/graphql", headers)
    
    let handleOperationResultErrors<'T when 'T :> OperationResultBase> (operationResult: 'T) =
        operationResult.Errors
        |> Array.map (fun x -> x.Message)
        |> function
            | [| error |] -> Error error
            | _ -> Ok operationResult
    
    let queryStarredRepositoriesAsync user token = async {
        let operation = GitHubProvider.Operation<starredRepositoriesQuery> ()
        use runtimeContext = getGitHubRuntimeContext token
        
        let convertPage (operationResult: GitHubProvider.Operations.StarredRepositories.OperationResult) = monad {
            let! data = operationResult.Data
            let! user = data.User
            let! repositoryNodes = user.StarredRepositories.Nodes
            let! endCursor = user.StarredRepositories.PageInfo.EndCursor
            
            let repos =
                repositoryNodes
                |> Array.choose (fun repo -> monad {
                    let! repo = repo
                    let! languages = repo.Languages
                    let! languageNodes = languages.Nodes
                    let language =
                        languageNodes
                        |> Array.tryHead
                        |> function
                            | Some (Some language) -> language.Name
                            | _ -> ""
                        
                    { Id = repo.Id
                      Name = repo.Name
                      Description = defaultArg repo.Description ""
                      Url = repo.Url.ToString ()
                      Language = language }
                })
                |> Array.toList
                
            {| Repos = repos
               EndCursor = if user.StarredRepositories.PageInfo.HasNextPage then Some endCursor else None |}
        }
        
        let rec fetchPageAsync endCursor: Async<Result<Repository list, string>> = async {
            try
                let! operationResult = operation.AsyncRun (runtimeContext, user, endCursor)
                
                return!
                    operationResult
                    |> handleOperationResultErrors
                    |> Result.map convertPage
                    |> function
                        | Error externalError -> externalError |> Error |> Async.wrap
                        | Ok None -> "Error converting page" |> Error |> Async.wrap
                        | Ok (Some page) ->
                            match page.EndCursor with
                            | None -> page.Repos |> Ok |> Async.wrap
                            | Some endCursor -> async {
                                let! nextPage = fetchPageAsync endCursor
                                return nextPage |> Result.map ((@) page.Repos)
                            }
                    
            with ex ->
                return sprintf "Error fetching starred repositories: %s" ex.Message |> Error
        }
        
        return! fetchPageAsync null
    }
        
    let getToken () =
        let envVarName = "GITHUB_API_TOKEN"
        match Environment.GetEnvironmentVariable envVarName with
        | null | "" -> sprintf "Invalid GitHub API Token (%s Environment Variable)" envVarName |> Error
        | token -> Ok token

