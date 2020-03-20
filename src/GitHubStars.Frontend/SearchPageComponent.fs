namespace GitHubStars.Frontend

open Fable.React.Props
open Fable.React
open Fulma
open Fable.FontAwesome
open Browser.Types

module SearchPageComponent =
    let ``default`` = FunctionComponent.Of (fun (props: {| QueryRepositories: string -> unit |}) ->
        
        let state = Hooks.useState ""
    
        let events = {|
            OnInputKeyDown = fun (e: KeyboardEvent) ->
                if e.key = "Enter" then
                    props.QueryRepositories state.current
                    
            OnInputChange = fun (e: Event) ->
                state.update e.Value
                
            OnButtonClick = fun _ ->
                props.QueryRepositories state.current
        |}
        
        div [ Class "valign" ][
            
            div [ Style [ Display DisplayOptions.Flex
                          MarginBottom 10 ] ] [
        
                div [ Style [ AlignSelf AlignSelfOptions.Center
                              PaddingRight 5 ] ][
                    
                    str "https://github.com/"
                ]
                
                Input.text [ Input.Value state.current
                             Input.Props [ AutoFocus true
                                           OnChange events.OnInputChange
                                           OnKeyDown events.OnInputKeyDown
                                           Style [ Width 200 ] ] ]
            ]
                
            Button.button [ Button.OnClick events.OnButtonClick
                            Button.Color IsDark
                            Button.Props [ Disabled (state.current = "") ] ][
                
                str "get repositories"
                
                Icon.icon [ Icon.Props [ Style [ MarginLeft 0 ] ] ][
                    Fa.i [ Fa.Solid.ChevronRight ][]
                ]
            ]
        ]
    , memoizeWith = equalsButFunctions)

    let loading =
        div [ Class "valign" ][
            Progress.progress [ Progress.Color IsDark ][]
            div [][
                str "Getting the repository list from GitHub..."
            ]
        ]

