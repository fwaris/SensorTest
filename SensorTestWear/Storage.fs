module Storage
open System.IO
open System
open System.Collections.Generic
open Extensions

let personalDir = Environment.GetFolderPath(Environment.SpecialFolder.Personal)
let data_directory = Path.Combine(personalDir,"data")
let temp_ext = "_.csv"
let csv_ext = ".csv"

let ensureDir dir =
    if Directory.Exists dir |> not then
        Directory.CreateDirectory dir |> ignore

type FMsg = Data of (int64*int*int*IList<float32>) | FRoll | FClose of AsyncReplyChannel<unit> | FOpen of string

let dateStr() = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss")

type OpenFile = {Stream:StreamWriter; Name:string; Prefix:string}

let createDataFile nameprefix = 
    //open data files have names data_trip_<timestamp>.tmp
    //when done they will be renamed to .txt, compressed to .gzip and encrypted to .encr 
    ensureDir data_directory
    let ts =   dateStr()
    let fileName = sprintf "%s/%s_%s%s" data_directory nameprefix ts temp_ext
    logI fileName
    let stream = File.Create(fileName)
    stream.Flush()
    let str = new StreamWriter(stream)
    {Stream=str; Name=fileName; Prefix=nameprefix}

let writeFile (strw:StreamWriter) (ticks,name,count,values:IList<float32>) =
    strw.Write(ticks:int64)
    strw.Write(',')
    strw.Write(name:int)
    strw.Write(',')
    strw.Write(count:int)
    strw.Write(',')
    for i in 0..2 do
        strw.Write(values.[i])
        strw.Write(',')
    if values.Count > 3 then
        strw.Write(values.[3])
    strw.Write("\r\n")

let closeFile f =
    match !f with
    | Some {Name=file; Stream=str} -> 
        str.Close()
        let path = Path.GetDirectoryName(file)
        let fn = Path.GetFileNameWithoutExtension(file)
        let file2 = Path.Combine(path,fn,csv_ext)
        try
            File.Move(file,file2)
        with ex ->
            logE ex.Message
    | None -> ()
    f := None

let writeLoop (inbox:MailboxProcessor<_>) =
    let file : OpenFile option ref = ref None
    async {
        while true do
            try
                let! msg = inbox.Receive()
                match msg with
                | Data d -> 
                    match !file with
                    | Some s -> writeFile s.Stream d 
                    | _ -> ()
                | FRoll ->
                    match !file with
                    | Some f -> 
                        closeFile file
                        file := createDataFile f.Prefix |> Some
                    | _ -> ()
                | FOpen nameprefix ->
                    closeFile file
                    file := createDataFile nameprefix |> Some
                | FClose rc ->
                    closeFile file
                    rc.Reply()
            with ex ->
                logE ex.Message
    }

let rollover timeSpanMS (inbox:MailboxProcessor<_>) =
    async {
        while true do
            try
                do! Async.Sleep timeSpanMS
                inbox.Post FRoll
            with ex ->
               logE ex.Message
    }
