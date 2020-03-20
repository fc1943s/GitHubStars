namespace GitHubStars.Frontend

open Fable.React.Props
open Fable.React

module HeaderComponent =
    let ``default`` = FunctionComponent.Of (fun (props: {| User: string
                                                           ResetState: unit -> unit |}) ->
        div [ Id "header" ][
            
            div [][
                str "githubstars"
            ]
            
            if props.User <> "" then
                a [ OnClick (fun _ -> props.ResetState ()) ][
                    str "home"
                ]
        ]
    , memoizeWith = equalsButFunctions)

