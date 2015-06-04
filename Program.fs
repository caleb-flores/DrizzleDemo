// Learn more about F# at http://fsharp.net
// See the 'F# Tutorial' project for more help.
open System
open Drizzle.Workflow
open Drizzle.Gear
open Drizzle.Faucet
open Drizzle.Tuple
open Drizzle.LocalCluster
open Drizzle.Stream


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

