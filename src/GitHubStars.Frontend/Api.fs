namespace GitHubStars.Frontend

open System
open System.Text.RegularExpressions
open Fable.SimpleJson
open GitHubStars.Shared.Model
open Fable.SimpleHttp

module Api =
    let apiUrl =
        match "|API_URL|" with
        | x when x.StartsWith "|" -> ""
        | x -> Regex.Replace (x, @"\/*$", "")
        
    let queryRepositoriesAsync user tagPartialSearch = async {
        let url = sprintf "%s/api/users/%s/repositories?tag=%s" apiUrl user tagPartialSearch
        try
            let! statusCode, responseText = Http.get url
            
            return
                match statusCode with
                | 200 ->
                    responseText
                    |> SimpleJson.parse
                    |> SimpleJson.mapKeys (fun key -> string (Char.ToUpper key.[0]) + key.Substring 1) // Not cool
                    |> Json.convertFromJsonAs<UserRepository list>
                    |> Ok
                | 404 -> Error "API Endpoint not found"
                | _ -> Error responseText
        with ex ->
            return Error ex.Message
    }
        
    let setTagsAsync user repositoryId tags = async {
        let url = sprintf "%s/api/users/%s/repositories/%s/tags" apiUrl user repositoryId
        try
            let! statusCode, responseText = Http.put url (Json.stringify tags)
            
            return
                match statusCode with
                | 200 | 204 -> Ok ()
                | 404 -> Error "API Endpoint not found"
                | _ -> Error responseText
        with ex ->
            return Error ex.Message
    }

