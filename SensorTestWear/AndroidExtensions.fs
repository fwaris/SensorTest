﻿module AndroidExtensions
open System
open System.Collections.Generic
open System.Linq
open System.Text
open Extensions

open Android.App
open Android.Content
open Android.OS
open Android.Runtime
open Android.Util
open Android.Views
open Android.Widget
open Android.Support.Wearable.Views

let mapRunning serviceInfos =
    let ctx = Android.App.Application.Context
    let amgr = ctx.GetSystemService(Context.ActivityService) :?> ActivityManager
    let running = 
        amgr.GetRunningServices(1000)
        |> Seq.filter (fun ser -> ser.Started)
        |> Seq.map (fun ser -> ser.Service.ClassName)
        |> Seq.map lowercase
        |> Seq.toArray
    let runningSet = running |> Set.ofArray
    let areRunning = serviceInfos |> List.map(fun (name,_) -> name, runningSet |> Set.contains name)
    areRunning

let isServiceRunning serviceName = 
    mapRunning [lowercase serviceName, true]
    |> List.map snd
    |> Seq.nth 0