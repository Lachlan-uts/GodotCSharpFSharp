namespace GodotFs

open Godot
open PathProvider
open GodotNodeUtils
open NeedSystem
open NeedSystem.TaskSystem

// So I can use the adaptive package but not in the interactive
open FSharp.Data.Adaptive


// Porting enums from C# to F# to enable F# calling interopt
type NeedNames = Rest = 0 | Boredom = 1
type NeedTraits = OptimumValue = 0 | WorstValue = 1 | DecayDirection = 2 | DecayRate = 3 | CurrentDirection = 4

type PawnFs() as self =
    inherit Area2D()

    let startingNeeds = NeedSystem.defaultMapOfNeeds
    let startingTasks = TaskSystem.defaultMapOfTasks

    let mutable mutNeedMap = startingNeeds
    let mutable mutTaskMap = startingTasks //no use yet probably.
    let mutable mutCurrentMission:TaskNames Option = None

    let mutable mutCurrentPathA:Vector2 [] = Array.empty
    let mutable mutCurrentPoint = 1
    let mutable mutCurrentPathL:Vector2 list = List.empty

    let pathProviderNode = self.getNode "../Navigation2D"
    let visAid = self.getNode "visualAid"

    let pathProviderCS =
        lazy(
            self.GetParent().GetNode(new NodePath("Navigation2D"))
            :?> Navigation2D
    )

    //let testVec = new Vector2(0.0f, 0.0f)

    //let wot = pathProviderNode.Value.saySomething

    // the signal system
    let mutable currentTask = 0
    let handleActionEndingSignal word =
        GD.Print(word)
        currentTask <- currentTask+1

    // Functions/actions that don't require iter in the main loop
    let getPath destination =
        //visAid.Value
        PathProvider.calcPath pathProviderNode self.Position destination // Get the array of vector2 points
        |> Array.toList // Turn it into a list
        |> List.tail // Remove the first element because it's where this entity is and therefore is not needed.

    // The missions/actions/task functions, probably want to pull them out to a distinct module
    let walk (dir : Vector2) (speed : float32) (delta : float32) =
        self.Translate(dir * speed * delta)

    let chopTree () =
        ()

    let nap () =
        ()

    // Currently only being called once every 60 frames or once per second.
    // Due to being called from within the C# script currently.
    override this._PhysicsProcess(delta) =
        let needList = (Map.map (fun k v -> v.CurrentValue) mutNeedMap) |> Map.toList |> (List.sortBy (fun (_,y) -> y))
        let chooseMission currentMission (needL : (NeedName * float) list) =
            match currentMission with
            | None -> "get new task"
            | Some x when (snd needL.Head) < 2.0 -> "get new task"
            | _ -> "Stick with it"

        //do tasky stuff here.
        let action taskName =
            match taskName with
            | "task1" -> walk (new Vector2(0.0f,0.0f)) 10.0f delta
            | "task2" -> chopTree ()
            | _ -> nap ()

        // I need to choose a need,
        //let needList = (Map.map (fun k v -> v.CurrentValue) mutNeedMap) |> Map.toList |> (List.sortBy (fun (_,y) -> y))
        // then look at the list of tasks
        let newTask = getTaskFromPriorityNeed startingTasks (fst needList.Head)
        // then choose a task and swap to it or maintain current task
        mutNeedMap <- updateNeedsWithTask newTask mutNeedMap
        // then iterate needs.
        mutNeedMap <- updateNeedMap mutNeedMap // Make this take a task and time?
        //mutNeedMap <- mapBounce mutNeedMap
        Map.iter (fun (name : NeedName) (need : NeedSystem.Need ) -> GD.Print(need.CurrentValue.ToString())) mutNeedMap

    // Abstract (C# callable) Methods/functions

    //abstract member UpdateNeedTest: string -> unit
    //default this.UpdateNeedTest _ =
        ////mutRest <- updateNeed mutRest
        //GD.Print(mutRest.CurrentValue.ToString())

    //abstract member ChangeTaskAdjustNeeds: Need


        
    abstract member ChooseWanderDest: Rect2 -> int[] -> Vector2
    default this.ChooseWanderDest (pArea : Rect2) (validTiles : int[]) =
        PathProvider.getClosestValidTile pathProviderNode (PathProvider.getRandomPointFromArea pArea validTiles)