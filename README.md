# DrizzleDemo

Install the dependencies 
-----------------------

Install via paket, you need to be in the root directory:

``mono paket.exe install``

Description
------------------------

In this Demo I made a topology with one Faucet and two gears as follow:

``Faucet(RandoWordsFaucet) -> Gear(FilterEvenWordGear) -> Gear(WordPrinterGear)``

  * RandomWordsFaucet will send random words in tuples (word,length) from a file to the Gear(FilterEvenWord)
  * FilterEvenWord will receive random words but will only send the words when the length is even
  * WordPrinterGear this gear is going to receive a tuple (word,length) and will print a message
  
The equivalent in storm will be:
  * Spout - Faucet
  * Bolt - Gear


To create a Faucet
------------------------

You need to create a class that implement `IFaucet`.
This class will be the responsible to send data to 
the gears suscribed.

This class needs to be serializable.

```
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
```

To create a Gear
------------------------
The Gears need to be serializable as well and implement the `IGear` interface.
The gears could be conected to other gear or faucet to recieve data 


```
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
```

```
type WordPrinterGear() =
    interface IGear with
        member x.Init(): unit = 
            ()
        
        member x.Process(tuple: DrizzleTuple) (emitter: IEmitter): unit = 
            let word,length = tuple :?> string*int
            printfn "word = %s, Length = %i" word length
        
        member x.Stop(): unit = 
            ()
```

Creating the workflow
------------------------------

To create a Workflow we have 3 computation expressions named `workflow`,`gear` and `faucet`

the computation expresion 'workflow' has the methods to help to create a Workflow:
    * `name`: the name of the workflow
    * `addFaucet` : which adds a faucet as node to the topology
    * `AddGear`: which adds a gear as node to the topology
    * `withConfig`: Receives a Map<string,string> with the configuration 
    * `validate` : this method validates that the workflow is ok
    
