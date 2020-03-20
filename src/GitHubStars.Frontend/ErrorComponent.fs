namespace GitHubStars.Frontend

open Fable.React.Props
open Fable.React
open Fulma

module ErrorComponent =
    let ``default`` = FunctionComponent.Of (fun (props: {| Error: string
                                                           HideError: unit -> unit |}) ->
        div [ Id "error" ][
            
            Message.message [ Message.Color IsDanger ] [
                
                Message.header [] [
                    str "Error"
                    Delete.delete [ Delete.OnClick (fun _ -> props.HideError ()) ][]
                ]
                
                Message.body [][
                    str props.Error
                ]
            ]
        ]
    , memoizeWith = equalsButFunctions)

