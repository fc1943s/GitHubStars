namespace GitHubStars.Backend

open System
open FSharpPlus.Data
open GitHubStars.Shared.Model
open Microsoft.AspNetCore.Http
open Npgsql.FSharp
open Saturn
open Giraffe
open FSharp.Control.Tasks.V2.ContextInsensitive

module Api =
    let connectionString =
        Sql.host "githubstars-postgres"
        |> Sql.port 5432
        |> Sql.username (Environment.GetEnvironmentVariable "POSTGRES_USER")
        |> Sql.password (Environment.GetEnvironmentVariable "POSTGRES_PASSWORD")
        |> Sql.database "postgres"
        |> Sql.formatConnectionString
        
    let gitHubToken =
        GitHub.getToken ()
        |> Core.resultOrException
        
    let handleRequest fn = fun next ctx -> task {
        let! result = fn ctx |> Async.StartAsTask
        return! result next ctx
    }
        
    let tryBindJsonAsync<'T> (ctx: HttpContext) = async {
        try
            let! model = ctx.BindJsonAsync<'T> () |> Async.AwaitTask
            return Ok model
        with ex ->
            return Error ex.Message
    }
    
    let apiRouter connectionString = choose [
        
        subRoutef "/api/users/%s" (fun user -> choose [
            
            subRoute "/repositories" (choose [
                
                GET >=> handleRequest (fun ctx -> async {
                    let tagPartialSearch = ctx.TryGetQueryStringValue "tag"
                    
                    use connection = new Npgsql.NpgsqlConnection (connectionString)
                    
                    return!
                        connection
                        |> Database.getRepositoriesWithTag user tagPartialSearch
                        |> Result.map (fun tags ->
                            match tagPartialSearch with
                            | None | Some "" ->
                                gitHubToken
                                |> GitHub.queryStarredRepositoriesAsync user
                                    
                            | Some _ -> 
                                let repositoryIds =
                                    tags
                                    |> List.map (fun x -> x.RepositoryId)
                                    |> List.distinct
                                    |> List.toArray
                                
                                gitHubToken
                                |> GitHub.queryRepositoriesByIdAsync repositoryIds
                            |> fun repositories -> async {
                                match! repositories with
                                | Error error -> return error |> RequestErrors.BAD_REQUEST
                                | Ok repositories ->
                                    return
                                        repositories
                                        |> List.map (fun repository ->
                                            { Repository = repository
                                              Tags =
                                                  tags
                                                  |> List.filter (fun x -> x.RepositoryId = repository.Id) 
                                                  |> List.map (fun x -> x.Tag)
                                                  |> List.toArray }
                                        )
                                        |> List.toArray
                                        |> Successful.OK
                            }
                        )
                        |> function
                            | Error error -> error |> RequestErrors.BAD_REQUEST |> Async.wrap
                            | Ok repositories -> repositories
                })
                
                subRoutef "/%s/tags" (fun repositoryId -> choose [
                    
                    POST >=> handleRequest (fun ctx -> async {
                        let! tag = tryBindJsonAsync<string> ctx
                        return
                            match tag |> Result.map cleanTag with
                            | Error _ | Ok "" | Ok null -> "Error parsing tag from request body" |> RequestErrors.BAD_REQUEST
                            | Ok tag ->
                                use connection = new Npgsql.NpgsqlConnection (connectionString)
                                
                                connection
                                |> Database.addTag user repositoryId tag
                                |> function
                                    | Ok () -> Successful.CREATED ""
                                    | Error error -> error |> RequestErrors.BAD_REQUEST
                    })
                    
                    PUT >=> handleRequest (fun ctx -> async {
                        let! tags = tryBindJsonAsync<string[]> ctx
                        
                        return
                            match tags |> Result.map (Option.ofObj >> Option.map cleanTags) with
                            | Error _ | Ok None -> "Error parsing tags from request body" |> RequestErrors.BAD_REQUEST
                            | Ok (Some tags) when not (validateTags tags) -> "Invalid tags" |> RequestErrors.BAD_REQUEST
                            | Ok (Some tags) ->
                                use connection = new Npgsql.NpgsqlConnection (connectionString)
                                
                                connection
                                |> Database.setTags user repositoryId tags
                                |> function
                                    | Ok results when List.sum results = 0 -> RequestErrors.NOT_FOUND "No operation was performed"
                                    | Ok _ -> Successful.NO_CONTENT
                                    | Error error -> error |> RequestErrors.BAD_REQUEST
                    })
                    
                    DELETE >=> routef "/%s" (fun tag -> handleRequest (fun _ -> async {
                        use connection = new Npgsql.NpgsqlConnection (connectionString)
                        
                        return
                            connection
                            |> Database.deleteTag user repositoryId tag
                            |> function
                                | Ok 0 -> RequestErrors.NOT_FOUND "Tag not found"
                                | Ok _ -> Successful.NO_CONTENT
                                | Error error -> error |> RequestErrors.BAD_REQUEST
                    }))
                    
                ])
            ])
        ])
        
        RequestErrors.NOT_FOUND "Endpoint not found"
    ]
        
    [<EntryPoint>]
    let main _ =
        do
            use connection = new Npgsql.NpgsqlConnection (connectionString)
            connection
            |> Database.createTables
            |> ResultOrException.Result
            |> ignore
        
        application {
            url "http://0.0.0.0:8086/"
            use_router (apiRouter connectionString)
            use_cors "" (fun builder -> builder.AllowAnyOrigin().AllowAnyMethod () |> ignore)
        } |> run
        0
