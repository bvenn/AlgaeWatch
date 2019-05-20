namespace Shared

type ArbVal = { ValueA : float }

module Route =
    /// Defines how routes are generated on server and mapped from client
    let builder typeName methodName =
        sprintf "/api/%s/%s" typeName methodName

type TransmitJSON = {
      D     : string // "dd.MM.yy HH:mm:ss"
      T1    : float
      T2    : float
      T3    : float
      T4    : float
      T5    : float
      T6    : float
      L     : float
    }


/// A type that specifies the communication protocol between client and server
/// to learn more, read the docs at https://zaid-ajaj.github.io/Fable.Remoting/src/basics.html
type ILoggingAPI =
    { GetPlot: unit -> Async<string>
      GetPlotChunk: string -> Async<string>
      GetPlotWavelet: (string * string) -> Async<string> //date and trace
      ArduinoPost: TransmitJSON -> Async<unit>
      FetchRain: unit -> Async<unit>
    }
