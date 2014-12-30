module Serdes
open System
open System.IO

let intArrayToBytes (xs:int array) = 
   let data = Array.zeroCreate ( 4 + xs.Length * 4)  // count + data
   let ms = new MemoryStream(data)
   let strw = new BinaryWriter(ms)
   strw.Write(xs.Length)
   for x in xs do strw.Write(x)
   strw.Flush()
   strw.Close()
   ms.Close()
   data

let bytesToIntArray (bytes:byte array) =
    if bytes.Length = 0 then
        [||]
    else
        let ms = new MemoryStream(bytes)
        let strw = new BinaryReader(ms)
        let c = strw.ReadInt32()
        let xs = Array.zeroCreate c
        for i in 0..c-1 do
            xs.[i] <- strw.ReadInt32()
        xs

 (*
 let d = [|1; 2; 3|] |> intArrayToBytes 
 d |> bytesToIntArray
 *)