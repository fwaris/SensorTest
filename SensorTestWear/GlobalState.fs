module GlobalState 
open System

let runningEvent = Event<bool>()

let isRunning  = runningEvent.Publish

let mutable filenameprefix = ""