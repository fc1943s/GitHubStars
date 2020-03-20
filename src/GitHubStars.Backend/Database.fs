namespace GitHubStars.Backend

open Npgsql.FSharp
open GitHubStars.Shared.Model

module Database =
    let createTables connection =
        connection
        |> Sql.existingConnection
        |> Sql.query """ CREATE TABLE IF NOT EXISTS tags (
                             "id" SERIAL PRIMARY KEY,
                             "user" TEXT NOT NULL,
                             "repository_id" TEXT NOT NULL,
                             "tag" TEXT NOT NULL,
                             UNIQUE ("user", "repository_id", "tag")
                         ); """
        |> Sql.parameters []
        |> Sql.executeNonQuery
        
    let getRepositoriesWithTag user tagPartialSearch connection =
        connection
        |> Sql.existingConnection
        |> Sql.query """ SELECT * 
                           FROM tags
                          WHERE "user" = @u
                            AND "repository_id" IN (SELECT DISTINCT "repository_id"
                                                      FROM tags
                                                     WHERE "user" = @u
                                                       AND ("tag" LIKE '%'||@tag_search||'%' OR @tag_search = '') )
                          ORDER BY "id" """
        |> Sql.parameters [ "u", Sql.text user
                            "tag_search", Sql.text (defaultArg tagPartialSearch "") ]
        |> Sql.execute (fun rowReader ->
            { User = rowReader.text "user"
              RepositoryId = rowReader.text "repository_id"
              Tag = rowReader.text "tag" }
        )
        |> Result.mapError (fun ex ->
            sprintf "Error querying tags: %s" ex.Message
        )
        
    let addTag user repositoryId tag connection =
        connection
        |> Sql.existingConnection
        |> Sql.query """ INSERT INTO tags ("user", "repository_id", "tag") VALUES (@u, @rid, @t); """
        |> Sql.parameters [ "u", Sql.text user
                            "rid", Sql.text repositoryId
                            "t", Sql.text tag ]
        |> Sql.executeNonQuery
        |> Result.mapError (fun ex ->
            if ex.Message.Contains "23505"
            then "Tag already exists"
            else sprintf "Error adding tag %s: %s" tag ex.Message
        )
        |> Result.bind (function
            | 1 -> Ok ()
            | _ -> Error "Unknown error while trying to add tag"
        )
            
    let setTags user repositoryId newTags connection =
        connection
        |> Sql.existingConnection
        |> Sql.executeTransaction [
            """ DELETE FROM tags WHERE "user" = @u AND "repository_id" = @rid; """,
            [
                [ "u", Sql.text user
                  "rid", Sql.text repositoryId ]
            ]
            
            """ INSERT INTO tags ("user", "repository_id", "tag") VALUES (@u, @rid, @t); """, 
            newTags
            |> cleanTags
            |> List.map (fun tag ->
                [ "u", Sql.text user
                  "rid", Sql.text repositoryId
                  "t", Sql.text tag ]
            )
        ]
        |> Result.mapError (fun ex ->
            sprintf "Error updating tags: %s" ex.Message
        )
        
    let deleteTag user repositoryId tag connection =
        connection
        |> Sql.existingConnection
        |> Sql.query """ DELETE FROM tags WHERE "user" = @u AND "repository_id" = @rid AND "tag" = @t; """
        |> Sql.parameters [ "u", Sql.text user
                            "rid", Sql.text repositoryId
                            "t", Sql.text tag ]
        |> Sql.executeNonQuery
        |> Result.mapError (fun ex ->
            sprintf "Error deleting tag %s: %s" tag ex.Message
        )

