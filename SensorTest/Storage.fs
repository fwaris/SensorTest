module Storage
open System.IO
open System
open Extensions

let personalDir = Environment.GetFolderPath(Environment.SpecialFolder.Personal)
let data_directory = Path.Combine(personalDir,"data")

let ensureDir dir =
    if Directory.Exists dir |> not then
        Directory.CreateDirectory dir |> ignore
(*
type FMsg = Data of (int64*String*int*float32 array) | FClose of AsyncReplyChannel<unit> | FOpen of string

let dateStr() = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss")

type OpenFile = {Stream:StreamWriter; Name:string}

let createDataFile nameprefix = 
    //open data files have names data_trip_<timestamp>.tmp
    //when done they will be renamed to .txt, compressed to .gzip and encrypted to .encr 
    ensureDir data_directory
    let ts =   dateStr()
    let fileName = sprintf "%s/%s_%s.csv" data_directory nameprefix ts
    logI fileName
    let stream = File.Create(fileName)
    stream.Flush()
    let str = new StreamWriter(stream)
    {Stream=str; Name=fileName}

let writeFile (strw:StreamWriter) (ticks,name,count,values:float32 array) =
    strw.Write(ticks:int64)
    strw.Write(',')
    strw.Write(name:string)
    strw.Write(',')
    strw.Write(count:int)
    strw.Write(',')
    for i in 0..2 do
        strw.Write(values.[i])
        strw.Write(',')
    if values.Length > 3 then
        strw.Write(values.[3])
    strw.Write("\r\n")
        
                    
let agent = MailboxProcessor.Start(fun inbox ->
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
                | FOpen nameprefix ->
                    match !file with 
                    | Some f -> f.Stream.Close() 
                    | _-> ()
                    file := createDataFile nameprefix |> Some
                    let i = 1
                    ()
                | FClose rc ->
                    match !file with
                    | Some f -> f.Stream.Close()
                    | None -> ()
                    rc.Reply()
            with ex ->
                logE ex.Message
    })
*)

let copyFile ctx uictx f =
    try
        let files = Directory.GetFiles(data_directory)
        if files |> Array.isEmpty then
            AndroidExtensions.notifyAsync ctx uictx "" "No files to copy" f
        else
            let extPath = "/sdcard/Download"
            ensureDir extPath
            files
            |> Array.map (fun f ->
                let toPath = Path.Combine(extPath,Path.GetFileName(f))
                try
                    logI (sprintf "copying %s to %s" f toPath)
                    File.Copy(f,toPath)
                    File.Delete f
                    1
                with ex ->
                    logE ex.Message
                    0
                )
            |> Array.sum
            |> fun count ->
                AndroidExtensions.notifyAsync ctx uictx "" (sprintf "%d files copied out of %d" count files.Length) f
    with ex ->
        logE ex.Message

