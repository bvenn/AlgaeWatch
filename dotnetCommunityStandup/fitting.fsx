#r "nuget: FSharp.Stats"
#r "nuget: Plotly.NET, 2.0.0-alpha5"

open FSharp.Stats
open Plotly.NET
open Fitting.LinearRegression

let xData = vector [|1. .. 10.|]
let yData = vector [|4.;7.;9.;12.;15.;17.;16.;23.;5.;30.|]


Seq.median xData

Seq.stDev yData

Correlation.Seq.pearson xData yData

ML.DistanceMetrics.cityblock xData yData

Testing.TTest.twoSample false xData yData


// get coefficients of interpolating polynomial
let interpolatingCoefficients = 
    Interpolation.Polynomial.coefficients xData yData

// get fitting function of interpolating polynomial
let interpolFitFunc = 
    Interpolation.Polynomial.fit interpolatingCoefficients

// get coefficients of 3rd order regression polynomial
let regressionCoefficients = 
    OrdinaryLeastSquares.Polynomial.coefficient 3 xData yData
    
// get fitting function of 3rd order regression polynomial
let regressionFitFunc = 
    OrdinaryLeastSquares.Polynomial.fit 3 regressionCoefficients
    

let rawChart = Chart.Point(xData,yData)

let interpolChart = 
    [1. .. 0.1 .. 10.] 
    |> List.map (fun x -> x,interpolFitFunc x)
    |> Chart.Line

let regressionChart = 
    [1. .. 0.1 .. 10.] 
    |> List.map (fun x -> x,regressionFitFunc x)
    |> Chart.Line


[rawChart;interpolChart;regressionChart]
|> Chart.Combine
|> Chart.Show




let cSpline = Interpolation.CubicSpline.Simple.coefficients Interpolation.CubicSpline.Simple.BoundaryCondition.Natural xData yData
Interpolation.CubicSpline.Simple.fit cSpline xData 
|> fun fu -> [1. .. 0.01 .. 10.] |> List.map (fun x -> x,fu x) |> Chart.Line |> Chart.Show


Fitting.Spline.smoothingSpline (Seq.zip xData yData |> Array.ofSeq) (Array.ofSeq xData) 0.1
|> fun fu -> 
    [1. .. 0.1 .. 10.]
    |> List.map (fun x -> x,fu x)
    |> Chart.Line 
    |> Chart.Show


