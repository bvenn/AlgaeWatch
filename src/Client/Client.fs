module Client

open Elmish
open Elmish.React

open Fable.React
open Fable.React.Props
open Fable.FontAwesome
open Fable.Core.JsInterop
open Fulma

open Shared



// The model holds data that you want to keep track of while the application is running
// in this case, we are keeping track of a counter
// we mark it as optional, because initially it will not be available from the client
// the initial value will be requested from server
type Model = { 
    ArbState : ArbVal option
    PlotHTML : string
    Loading : bool
    ClickedOnChunk : bool
    LoadingChunk : bool
    ClickedOnWavelet : bool
    TraceSelectionMdl : bool
    LoadingWavelet : bool
    SearchBarChunkMdl : string
    SearchBarWaveletMdl : string
    WaveletTrace : string
    }

// The Msg type defines what events/actions can occur while the application is running
// the state of the application changes *only* in reaction to these events
type Msg =
| GetPlotRequest
| GetPlotResponse of Result<string, exn>
| GetPlotWaveletRequest
| GetPlotWaveletResponse of Result<string, exn>
| GetPlotChunkRequest
| GetPlotChunkResponse of Result<string, exn>
| FetchRainResponse of Result<unit, exn>
| PlotLoadingYear
| PlotLoadingChunk
| FetchRain
| PlotLoadingWavelet1
| PlotLoadingWavelet2
| PlotLoadingWavelet3
| PlotLoadingWavelet4
| PlotLoadingWavelet5
| PlotLoadingWavelet6
| PlotLoadingWaveletLight
| PlotLoadingWaveletRain
| ChunkClick
| WaveletClick
| SearchBarChunk
| SearchBarWavelet
| TraceSelectionWavelet
| UpdateChunkSearchBar of string
| UpdateWaveletSearchBar of string



module Server =

    open Shared
    open Fable.Remoting.Client
    
    /// A proxy you can use to talk to server directly
    let api : ILoggingAPI =
      Remoting.createApi()
      |> Remoting.withRouteBuilder Route.builder
      |> Remoting.buildProxy<ILoggingAPI>


module RequestHelpers =
    let createLoadingCmdPlot =
        Cmd.ofMsg GetPlotRequest

    let createLoadingCmdChunk =
        Cmd.ofMsg GetPlotChunkRequest   

    let createLoadingCmdWavelet =
        Cmd.ofMsg GetPlotWaveletRequest        

    let createGetPlotCmd =      
        Cmd.OfAsync.either
            Server.api.GetPlot
            ()
            (Ok >> GetPlotResponse)
            (Error >> GetPlotResponse)

    let openSearchBarChunkCmd =      
        Cmd.ofMsg ChunkClick     

    let openSearchBarWaveletCmd =      
        Cmd.ofMsg WaveletClick            

    let openTraceSelectionCmd =      
        Cmd.ofMsg TraceSelectionWavelet            

    let createGetPlotChunkCmd input=
        Cmd.OfAsync.either
            Server.api.GetPlotChunk 
            input
            (Ok >> GetPlotChunkResponse)
            (Error >> GetPlotChunkResponse)        

    let createGetPlotWaveletCmd date trace=
        Cmd.OfAsync.either
            Server.api.GetPlotWavelet 
            (date,trace)
            (Ok >> GetPlotWaveletResponse)
            (Error >> GetPlotWaveletResponse)

    let fetchRainCmd =
        Cmd.OfAsync.either
            Server.api.FetchRain
            ()
            (Ok >> FetchRainResponse)
            (Error >> FetchRainResponse)   

                



// defines the initial state and initial command (= side-effect) of the application
let init () : Model * Cmd<Msg> =
    let initialModel = { 
        ArbState = None 
        PlotHTML = ""
        Loading = false 
        ClickedOnChunk = false
        LoadingChunk = false
        ClickedOnWavelet = false
        LoadingWavelet = false
        SearchBarChunkMdl = ""
        SearchBarWaveletMdl = ""
        TraceSelectionMdl = false
        WaveletTrace = ""}
    initialModel, Cmd.none



// The update function computes the next state of the application based on the current state and the incoming events/messages
// It can also run side-effects (encoded as commands) like calling the server via Http.
// these commands in turn, can dispatch messages to which the update function will react.
let update (msg : Msg) (currentModel : Model) : Model * Cmd<Msg> =
    match  msg with
    | PlotLoadingYear -> 
        let nextModel =
            { currentModel with Loading = true; ClickedOnChunk = false;ClickedOnWavelet = false;PlotHTML=""}
        nextModel,RequestHelpers.createLoadingCmdPlot     
    | GetPlotRequest -> currentModel,RequestHelpers.createGetPlotCmd 
    | GetPlotResponse (Ok res) ->    
        let nextModel = 
            {
                currentModel with PlotHTML = res; Loading=false
            }
        nextModel, Cmd.none    
    | GetPlotResponse (Error ex)  -> 
        let nextModel =
                {
                    currentModel with PlotHTML = ex.Message; Loading=false
                }
        nextModel,Cmd.none

    //Chunk
    | SearchBarChunk ->
        let nextModel = 
            {
                currentModel with PlotHTML="";ClickedOnChunk = (not currentModel.ClickedOnChunk);ClickedOnWavelet = false//currentModel with ClickedOnChunk = true
            }
        nextModel,Cmd.none    
    | PlotLoadingChunk -> 
        let nextModel =
            { currentModel with PlotHTML="";LoadingChunk = true}
        nextModel,RequestHelpers.createLoadingCmdChunk     
    | GetPlotChunkRequest -> 
        currentModel,RequestHelpers.createGetPlotChunkCmd currentModel.SearchBarChunkMdl
    | GetPlotChunkResponse (Ok res) ->    
        let nextModel = 
            {
                currentModel with PlotHTML = res; LoadingChunk=false
            }
        nextModel, Cmd.none    
    | GetPlotChunkResponse (Error ex)  -> 
        let nextModel =
                {
                    currentModel with PlotHTML = ex.Message; LoadingChunk=false
                }
        nextModel,Cmd.none  

    //FetchRainResponse
    | FetchRainResponse (Ok res) ->    
        currentModel, Cmd.none    
    | FetchRainResponse (Error ex)  -> 
        currentModel,Cmd.none  
    | FetchRain -> 
        currentModel,RequestHelpers.fetchRainCmd


    //Wavelet
    | SearchBarWavelet ->
        let nextModel = 
            {
                currentModel with PlotHTML="";ClickedOnWavelet = (not currentModel.ClickedOnWavelet);ClickedOnChunk = false;TraceSelectionMdl = false//currentModel with ClickedOnChunk = true
            }
        nextModel,Cmd.none    
    | TraceSelectionWavelet -> 
        let nextModel =
            {
                currentModel with TraceSelectionMdl = true
            }
        nextModel,Cmd.none
    | PlotLoadingWavelet1 -> 
        let nextModel =
            { currentModel with PlotHTML="";LoadingWavelet = true;WaveletTrace = "T1"}
        nextModel,RequestHelpers.createLoadingCmdWavelet     
    | PlotLoadingWavelet2 -> 
        let nextModel =
            { currentModel with PlotHTML="";LoadingWavelet = true;WaveletTrace = "T2"}
        nextModel,RequestHelpers.createLoadingCmdWavelet         
    | PlotLoadingWavelet3 -> 
        let nextModel =
            { currentModel with PlotHTML="";LoadingWavelet = true;WaveletTrace = "T3"}
        nextModel,RequestHelpers.createLoadingCmdWavelet     
    | PlotLoadingWavelet4 -> 
        let nextModel =
            { currentModel with PlotHTML="";LoadingWavelet = true;WaveletTrace = "T4"}
        nextModel,RequestHelpers.createLoadingCmdWavelet         
    | PlotLoadingWavelet5 -> 
        let nextModel =
            { currentModel with PlotHTML="";LoadingWavelet = true;WaveletTrace = "T5"}
        nextModel,RequestHelpers.createLoadingCmdWavelet     
    | PlotLoadingWavelet6 -> 
        let nextModel =
            { currentModel with PlotHTML="";LoadingWavelet = true;WaveletTrace = "T6"}
        nextModel,RequestHelpers.createLoadingCmdWavelet         
    | PlotLoadingWaveletLight -> 
        let nextModel =
            { currentModel with PlotHTML="";LoadingWavelet = true;WaveletTrace = "Light"}
        nextModel,RequestHelpers.createLoadingCmdWavelet     
    | PlotLoadingWaveletRain -> 
        let nextModel =
            { currentModel with PlotHTML="";LoadingWavelet = true;WaveletTrace = "Rain"}
        nextModel,RequestHelpers.createLoadingCmdWavelet         
    | GetPlotWaveletRequest -> 
        currentModel,RequestHelpers.createGetPlotWaveletCmd currentModel.SearchBarWaveletMdl currentModel.WaveletTrace
    | GetPlotWaveletResponse (Ok res) ->    
        let nextModel = 
            {
                currentModel with PlotHTML = res; LoadingWavelet=false
            }
        nextModel, Cmd.none    
    | GetPlotWaveletResponse (Error ex)  -> 
        let nextModel =
                {
                    currentModel with PlotHTML = ex.Message; LoadingWavelet=false
                }
        nextModel,Cmd.none  
    | UpdateChunkSearchBar (input)  -> 
        let nextModel =
                {
                    currentModel with PlotHTML="";SearchBarChunkMdl = input
                }
        nextModel,Cmd.none
    | UpdateWaveletSearchBar (input)  -> 
        let nextModel =
                {
                    currentModel with PlotHTML="";SearchBarWaveletMdl = input
                }
        nextModel,Cmd.none//RequestHelpers.createGetPlotWaveletCmd     
    | _ -> currentModel, Cmd.none


let safeComponents =
    let components =
        span [ ]
           [
             a [ Href "http://suave.io" ] [ str "Suave" ]
             str ", "
             a [ Href "http://fable.io" ] [ str "Fable" ]
             str ", "
             a [ Href "https://elmish.github.io/elmish/" ] [ str "Elmish" ]
             str ", "
             a [ Href "https://fulma.github.io/Fulma" ] [ str "Fulma" ]
             str ", "
             a [ Href "https://zaid-ajaj.github.io/Fable.Remoting/" ] [ str "Fable.Remoting" ]
           ]

    span [ ]
        [ str " powered by: "
          components ]


let showArb = function
| { ArbState = Some arbstate } -> "\n\r" + (sprintf "%.2f" arbstate.ValueA)
| { ArbState = None   } -> "Loading..."


let button (model:Model) txt onClick =
    //let buttonCol = if model.ClickedOnChunk then (Button.CustomClass "button") else ( Button.CustomClass "redbutton")
    Button.button
        [ Button.IsFullWidth
          //Button.Color IsPrimary
          Button.CustomClass "button"
          Button.OnClick onClick ]
        [ str txt ]

let fetchbutton txt onClick =
    Button.button
        [ Button.IsFullWidth
          //Button.Color IsPrimary
          Button.CustomClass "redbutton"
          Button.OnClick onClick ]
        [ str txt ]

let fromToButton (model:Model) txt onClick =
    let buttonCol = if model.ClickedOnChunk then (Button.CustomClass "redbutton") else ( Button.CustomClass "button")
    Button.button
        [ Button.IsFullWidth
          //Button.Color IsPrimary
          buttonCol
          Button.OnClick onClick ]
        [ str txt ]

let waveletButton (model:Model) txt onClick =
    let buttonCol = if model.ClickedOnWavelet then (Button.CustomClass "redbutton") else ( Button.CustomClass "button")
    Button.button
        [ Button.IsFullWidth
          //Button.Color IsPrimary
          buttonCol
          Button.OnClick onClick ]
        [ str txt ]    

let loading = 
    Columns.columns []
        [   
            Column.column [ Column.Width (Screen.Desktop,Column.Is4)] []
            Column.column [Column.Width (Screen.Desktop,Column.Is4)] [
                                                        
                Image.image [Image.Option.CustomClass "centr" ]                                   
                    [
                        img 
                            [   
                                Props.Src "https://media.giphy.com/media/4ZgLPakqTajjVFOVqw/giphy.gif"
                            ]
                    ]                                                         
                ]
            Column.column [Column.Width (Screen.Desktop,Column.Is4)] []
                              
        ]

let loadingWavelet = 
    Columns.columns []
        [   
            Column.column [ Column.Width (Screen.Desktop,Column.Is4)] []
            Column.column [Column.Width (Screen.Desktop,Column.Is4)] [
                
                Notification.notification [ Notification.Color IsWarning;Notification.Modifiers [Modifier.TextAlignment (Screen.All,TextAlignment.Centered) ] ] [str "This may take some time!"]                                        
                Image.image [Image.Option.CustomClass "centr" ]                                   
                    [
                        img 
                            [   
                                Props.Src "https://media.giphy.com/media/4ZgLPakqTajjVFOVqw/giphy.gif"
                            ]
                    ]                                                         
                ]
            Column.column [Column.Width (Screen.Desktop,Column.Is4)] []
                              
        ]

let view (model : Model) (dispatch : Msg -> unit) =
    
    div []
        [ 
          Hero.hero [Hero.IsMedium; Hero.CustomClass "csbHero"] [
               Hero.body [] [
                   Container.container [] [
                       Heading.h1 [Heading.CustomClass "csbHead"] [
                           str "AlgaeWatch"
                       ]
                       Heading.h3 [Heading.CustomClass "csbHead";Heading.IsSubtitle] [
                           str "Automated logging and analysis of water temperature"
                       ]
                   ]
               ]
           ]
          
          
          Container.container []
              [ Content.content [ Content.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered) ] ]
                    []
                Columns.columns []
                    [ 
                      Column.column [] [ button model "show last year" (fun _ -> dispatch PlotLoadingYear) ] 
                      Column.column [] [ fromToButton model "show from-to" (fun _ -> dispatch SearchBarChunk) ] 
                      Column.column [] [ waveletButton model "show wavelet from to" (fun _ -> dispatch SearchBarWavelet)] 
                      Column.column [] [ fetchbutton "Fetch rain data" (fun _ -> dispatch FetchRain) ] 
                      ]



                
                Columns.columns [Columns.CustomClass "outerFrame"; Columns.IsCentered ] [
                    Column.column [Column.Width (Screen.Desktop,Column.Is12)] [
                        if model.ClickedOnChunk then
                            yield 
                                    Columns.columns [ ]

                                        [Column.column [] []  
                                         Column.column [ Column.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered) ] ]
                                            [ 
                                            Level.item [ ]
                                                [ Field.div [ Field.HasAddons ]
                                                    [ Control.div [ ]
                                                        [ Input.text [  Input.Placeholder ("yyMMdd - yyMMdd" )
                                                                        Input.OnChange (fun e ->    let x = !!e.target?value
                                                                                                    dispatch (UpdateChunkSearchBar x)
                                                                                                    
                                                                                    )
                                                                   ]
                                                                  ]
                                                      Control.div [ ] [
                                                                   button model "from-to" (fun _ -> dispatch PlotLoadingChunk)
                                                                  ]
                                                    ]
                                                ]
                                           ]
                                         Column.column [] []
                                         Column.column [] []                                       
                                        ]
                        elif model.ClickedOnWavelet then                
                            yield 
                                    Columns.columns [ ]

                                        [Column.column [] []  
                                         Column.column [] []
                                         Column.column [ Column.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered) ] ]
                                            [ 
                                            Level.item [ ]
                                                [ Field.div [ Field.HasAddons ]
                                                    [ Control.div [ ]
                                                        [ Input.text [  Input.Placeholder ("yyMMdd - yyMMdd" )
                                                                        Input.OnChange (fun e ->    let x = !!e.target?value
                                                                                                    dispatch (UpdateWaveletSearchBar x)
                                                                                                    
                                                                                    )
                                                                   ]
                                                                  ]
                                                      Control.div [ ] [
                                                                   button model "from-to" (fun _ -> dispatch TraceSelectionWavelet)
                                                                  ]
                                                    ]
                                                ]
                                           ]
                                         Column.column [] []                                       
                                        ]    
                            if model.TraceSelectionMdl then                  
                                              
                                yield 
                                        Columns.columns [ ]

                                          [
                                            Column.column [] []  
                                            Column.column [] []
                                            Column.column [ Column.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered) ] ]
                                               [ 
                                                Columns.columns [ Columns.IsGap (Screen.All,Columns.ISize.Is8) ]
                                                                [ 
                                                                  Column.column [] [button model "T1" (fun _ -> dispatch    PlotLoadingWavelet1 )]
                                                                  Column.column [] [button model "T2" (fun _ -> dispatch    PlotLoadingWavelet2 )]
                                                                  Column.column [] [button model "T2" (fun _ -> dispatch    PlotLoadingWavelet3 )]
                                                                  Column.column [] [button model "T4" (fun _ -> dispatch    PlotLoadingWavelet4 )]
                                                                  Column.column [] [button model "T5" (fun _ -> dispatch    PlotLoadingWavelet5 )]
                                                                  Column.column [] [button model "T6" (fun _ -> dispatch    PlotLoadingWavelet6 )]
                                                                  Column.column [] [button model "Light" (fun _ -> dispatch PlotLoadingWaveletLight)] 
                                                                  Column.column [] [button model "Rain" (fun _ -> dispatch  PlotLoadingWaveletRain )] 

                                                                ]
                                                
                                               ]
                                            Column.column [] []                                       
                                            ]
                                                                                                                        
                        
                        if model.Loading then
                            yield 
                                loading
                        elif model.LoadingChunk then
                            yield 
                                loading
                        elif model.LoadingWavelet then
                            yield 
                                loadingWavelet                                                 
                        else
                            yield iframe 
                                    [
                                      SrcDoc model.PlotHTML
                                      Id "frame"
                                    ] []                                          

                    
                ] ]
                Content.content [ Content.Modifiers [ Modifier.IModifier.Display (Screen.All, Display.Block) ] ] 
                            [ ] ]

          Footer.footer [ ]
                [   Columns.columns []
                        [
                            Column.column [] [
                                Content.content [ Content.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered) ] ] 
                                    [   span [] [str "created by: "]
                                        a [ Href "https://github.com/bvenn" ] [ str "Benedikt Venn" ] ] ] 
                        
                            Column.column [] [
                            Content.content [ Content.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered) ] ] 
                                    [   //span [] [str "Documentation: "]
                                        a [ Href "https://github.com/bvenn/AlgaeWatch" ] [ str "Documentation" ] ] ] 
                        
                            Column.column [] [
                            Content.content [ Content.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered) ] ] [ safeComponents ] ]                                 
                        ]
                ]
            ]


#if DEBUG
open Elmish.Debug
open Elmish.HMR
#endif

Program.mkProgram init update view
#if DEBUG
|> Program.withConsoleTrace
#endif
|> Program.withReactBatched "elmish-app"
#if DEBUG
|> Program.withDebugger
#endif
|> Program.run

