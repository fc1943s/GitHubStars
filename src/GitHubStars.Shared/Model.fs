namespace GitHubStars.Shared

open System.Text.RegularExpressions

module Model =
    
    [<CLIMutable>]
    type Repository =
        { Id: string
          Name: string
          Description: string
          Url: string
          Language: string }

    [<CLIMutable>]
    type RepositoryTag =
        { User: string
          RepositoryId: string
          Tag: string }

    [<CLIMutable>]
    type UserRepository =
        { Repository: Repository
          Tags: string[] }

    
    let cleanTag (tag: string) =
        tag.Trim().ToLower().Replace(" ", "_")
        
    let cleanTags (tags: seq<string>) =
        tags
        |> Seq.map cleanTag
        |> Seq.except [ "" ]
        |> Seq.distinct
        |> Seq.toList

    let validateTags =
        List.forall (fun x -> Regex.IsMatch (x, @"^[^-_][\w\-]*[^-_]$"))

