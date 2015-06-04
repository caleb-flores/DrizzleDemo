// Learn more about F# at http://fsharp.net
// See the 'F# Tutorial' project for more help.
open System
open Drizzle.Workflow
open Drizzle.Gear
open Drizzle.Faucet
open Drizzle.Tuple
open Drizzle.LocalCluster
open Drizzle.Stream

(* 

In this Demo I made a topology with one Faucet and two gears as follow:

Faucet(RandoWordsFaucet) -> Gear(FilterEvenWordGear) -> Gear(WordPrinterGear)

RandomWordsFaucet will send random words in tuples (word,length) from a file to the Gear(FilterEvenWord)

FilterEvenWord will receive random words but will only send the words when the length is even

WordPrinterGear this gear is going to receive a tuple (word,length) and will print a message


=============== Faucet ============
You need to create a class that implement IFaucet.
This class will be the responsible to send data to 
the gears suscribed.

This class needs to be serializable.

*)
type RandomWordsFaucet()=
    let mutable words = [||]
    let random = new Random()
    interface IFaucet with
        member x.Close(): unit = 
            ()
        
        member x.Emit(emitter:IEmitter): TimeSpan = 
            let n = random.Next(0,words.Length-1)
            let word : string = Array.get words n
            printfn  "sending %A" word
            emitter.Emit((word,word.Length))

            //The time that the worker will be waiting for send the next tuple, in this case this faucet will be emitting a word every 5 seconds 
            TimeSpan.FromSeconds 5.0 
        
        member x.Open(config: Map<string,string>): unit = 
           words <- System.IO.File.ReadLines "../../Resources.txt" |> Seq.toArray
           ()
(*
================ Gears==================

The Gears need to be serializable as well and implement the IGear interface 

the gears could be conected to other gear or faucet to recieve data 

*)
type FilterEvenWordGear() =
    interface IGear with
        member x.Init(): unit = 
            ()
        
        member x.Process(tuple:DrizzleTuple) (emitter: IEmitter): unit = 

            let word,length = tuple :?> string*int


            if length%2 = 0 then
                emitter.Emit(tuple)

            
        
        member x.Stop(): unit = 
            ()

type WordPrinterGear() =
    interface IGear with
        member x.Init(): unit = 
            ()
        
        member x.Process(tuple: DrizzleTuple) (emitter: IEmitter): unit = 
            let word,length = tuple :?> string*int
            printfn "word = %s, Length = %i" word length
        
        member x.Stop(): unit = 
            ()




[<EntryPoint>]
let main argv = 


    let mywf = workflow {
        name "mywf"
        addFaucet(faucet{
            name "random-word"
            create (RandomWordsFaucet())
            parallelism 1
             })
        addGear(gear{
            name "filter-gear"
            create (FilterEvenWordGear())
            parallelism 1
            streams [ Drizzle.Stream.shuffle "random-word" ]
        })
       
        addGear(gear{
            name "printer-gear"
            create (WordPrinterGear())
            parallelism 1
            streams [ Drizzle.Stream.shuffle "filter-gear" ]
        })
        validate
    }

    let localCluster = LocalCluster()
    localCluster.Start mywf

    Threading.Thread.Sleep(TimeSpan.FromMinutes 1.0)
    localCluster.Stop ()

    0 // return an integer exit code

