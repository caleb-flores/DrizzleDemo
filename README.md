# DrizzleDemo

Install the dependencies 
-----------------------
Install zmq:

``brew install zmq``

Install via paket, you need to be in the root directory:

``mono paket.exe install``

Description
------------------------

In this Demo I made a topology with one Faucet and two gears as follow:

``Faucet(RandoWordsFaucet) -> Gear(FilterEvenWordGear) -> Gear(WordPrinterGear)``

  * RandomWordsFaucet will send random words in tuples (word,length) from a file to the Gear(FilterEvenWord)
  * FilterEvenWord will receive random words but will send only the words when the length is even
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

The computation expresion `workflow` has the methods:
 * `name`: the name of the workflow
 * `addFaucet` : which recieves a DrizzleFaucet as parameter and  adds a faucet as node to the topology
 * `AddGear`: which recieves a DrizzleGear as parameter and adds a gear as node to the topology
 * `withConfig`: Receives a Map<string,string> with the configuration 
 * `validate` : this method validates that the workflow is ok
 
`faucet` has method to create a DrizzleFaucet:
 * `name` : the name of the Faucet
 * `create`: this method recieves a instance of a class that implements  `IFaucet`
 * `paralelism`: the number of faucet with this instance (Drizzle will create this number of copy)

`gear` has the methods:
 * `name` : the name of the Gear
 * `create`: this method recieves a instance of a class that implements `IGear`
 * `paralelism` : the number of gears with this instance (Drizzle will create this number of copy)
 * `steams` : this is  a `list` with the endPoint where the gear will connect 
 

```
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
```


Running the Workflow as LocalCluster
----------------------------------------------
At the moment the workflow only can run as Local cluster, because we will not know if a node is down.

```
    let localCluster = LocalCluster()
    localCluster.Start mywf

    Threading.Thread.Sleep(TimeSpan.FromMinutes 1.0)
    localCluster.Stop ()
```


