﻿namespace FSharp.Data.Adaptive

open FSharp.Data.Traceable

/// Changeable adaptive list that allows mutation by user-code and implements alist.
[<Sealed>]
type ChangeableIndexList<'T>(initial: IndexList<'T>) =
    let history = 
        let h = History(IndexList.trace)
        h.Perform(IndexList.computeDelta IndexList.empty initial) |> ignore
        h

    override x.ToString() =
        history.State |> Seq.map (sprintf "%A") |> String.concat "; " |> sprintf "clist [%s]"

    /// is the list currently empty?
    member x.IsEmpty = history.State.IsEmpty

    /// the number of elements currently in the list.
    member x.Count = history.State.Count

    /// Gets or sets the value for the list.
    member x.Value
        with get() = 
            history.State
        and set (state: IndexList<'T>) = 
            x.UpdateTo state
                
    /// Sets the current state as List applying the init function to new elements and the update function to
    /// existing ones.
    member x.UpdateTo(other : IndexList<'T2>, init : 'T2 -> 'T, update : 'T -> 'T2 -> 'T) =
        let current = history.State.Content
        let target = other.Content

        let store = 
            (current, target) ||> MapExt.choose2V (fun i l r ->
                match l with
                | ValueNone -> 
                    match r with
                    | ValueSome r -> ValueSome (Set (init r))
                    | ValueNone -> ValueNone
                | ValueSome l ->
                    match r with
                    | ValueSome r -> 
                        let nl = update l r
                        if cheapEqual l nl then 
                            ValueNone
                        else 
                            ValueSome (Set nl)
                    | ValueNone ->
                        ValueSome Remove
            )

        let ops = IndexListDelta(store)
        history.Perform ops |> ignore
        
    /// Sets the current state as List.
    member x.UpdateTo(other : IndexList<'T>) =
        if not (cheapEqual history.State other) then
            let delta = IndexList.computeDelta history.State other
            history.PerformUnsafe(other, delta) |> ignore

    /// Appends an element to the list and returns its Index.
    member x.Append (element: 'T) =
        let index = 
            if history.State.IsEmpty then Index.after Index.zero
            else Index.after history.State.MaxIndex

        history.Perform(IndexListDelta.single index (Set element)) |> ignore
        index
       
    /// Appends an element to the list and returns its Index.
    member x.Add (element: 'T) = x.Append(element)
    
    /// Appends an element to the list and returns its Index.
    member x.AddRange (elements: seq<'T>) =
        let mutable i = Index.after history.State.MaxIndex
        let mutable delta = IndexListDelta.empty
        for e in elements do
            delta <- delta.Add(i, Set e)
            i <- Index.after i
            
        history.Perform(delta) |> ignore

    /// Prepends an element to the list and returns its Index.
    member x.Prepend (element: 'T) =
        let index = 
            if history.State.IsEmpty then Index.after Index.zero
            else Index.before history.State.MinIndex

        history.Perform(IndexListDelta.single index (Set element)) |> ignore
        index

       
    /// Inserts an element at the given position in the list and returns its Index.
    /// Note that the position can be equal to the count of the list.
    member x.InsertAt(index : int, element : 'T) =
        if index < 0 || index > history.State.Count then raise <| System.IndexOutOfRangeException()
        let l, s, r = MapExt.neighboursAt index history.State.Content
        let r = 
            match s with
                | Some s -> Some s
                | None -> r

        let index = 
            match l, r with
            | Some (before,_), Some (after,_) -> Index.between before after
            | None,            Some (after,_) -> Index.before after
            | Some (before,_), None           -> Index.after before
            | None,            None           -> Index.after Index.zero

        history.Perform (IndexListDelta.single index (Set element)) |> ignore
        index

    /// Inserts an element directly after the given index and returns the new index for the element.
    member x.InsertAfter(index : Index, element : 'T) =
        let _l, s, r = MapExt.neighbours index history.State.Content
        match s with
        | None ->
            history.Perform (IndexListDelta.single index (Set element)) |> ignore
            index
        | Some (s, _) ->
            let index =
                match r with
                | Some (r,_) -> Index.between s r
                | None -> Index.after index
            history.Perform (IndexListDelta.single index (Set element)) |> ignore
            index
            
    /// Inserts an element directly before the given index and returns the new index for the element.
    member x.InsertBefore(index : Index, element : 'T) =
        let l, s, _r = MapExt.neighbours index history.State.Content
        match s with
        | None ->
            history.Perform (IndexListDelta.single index (Set element)) |> ignore
            index
        | Some (s, _) ->
            let index =
                match l with
                | Some (l,_) -> Index.between l s
                | None -> Index.before index
            history.Perform (IndexListDelta.single index (Set element)) |> ignore
            index

    /// Gets or sets an element in the list at the given index.
    member x.Item
        with get (index: Index) = 
            history.State.[index]
        and set (index: Index) (value: 'T) = 
            history.Perform (IndexListDelta.single index (Set value)) |> ignore
        
    /// Gets or sets the element associated to index.
    member x.Item
        with get (index: int) = 
            if index < 0 || index >= history.State.Count then raise <| System.IndexOutOfRangeException()
            history.State.[index]

        and set (index: int) (value: 'T) = 
            if index < 0 || index > history.State.Content.Count then raise <| System.IndexOutOfRangeException()
            match history.State.TryGetIndex index with
            | Some index ->
                history.Perform (IndexListDelta.single index (Set value)) |> ignore
            | None ->
                raise <| System.IndexOutOfRangeException()
            
    /// Clears the list.
    member x.Clear() =  
        if not history.State.IsEmpty then
            let ops = IndexList.computeDelta history.State IndexList.empty
            history.Perform ops |> ignore        

    /// Removes the given index from the list and returns true if the element was deleted.
    member x.Remove (index: Index) =
        history.Perform(IndexListDelta.single index Remove)
        
    /// Removes the element at the given position and returns its Index.
    member x.RemoveAt (index: int) =
        if index < 0 || index >= history.State.Count then raise <| System.IndexOutOfRangeException()
        let (index, _) = history.State.Content |> MapExt.item index
        history.Perform(IndexListDelta.single index Remove) |> ignore
        index

    /// Tries to get the Index associated to the given position.
    member x.TryGetIndex(index : int) =
        history.State.TryGetIndex index

    /// Gets the (optional) element associated to the given Index.
    member x.TryGet(index : Index) =
        IndexList.tryGet index history.State

    /// Tries to get the element at the given position.
    member x.TryAt(index : int) =
        IndexList.tryAt index history.State
        
    /// Returns a new (currently unused) index directly after the given one.
    member x.NewIndexAfter(index : Index) =
        history.State.NewIndexAfter index
        
    /// Returns a new (currently unused) index directly before the given one.
    member x.NewIndexBefore(index : Index) =
        history.State.NewIndexBefore index
        
    /// Gets the neigbour elements and self (if existing) and returns (previous, self, next) as a triple.
    member x.Neighbours(index : Index) =
        IndexList.neighbours index history.State
        
    /// Tries to get the (index, value) for element directly after the given ref.
    member x.TryGetNext(index : Index) =
        IndexList.tryGetNext index history.State
        
    /// Tries to get the (index, value) for element directly before the given ref.
    member x.TryGetPrev(index : Index) =
        IndexList.tryGetPrev index history.State


    /// The smallest index contained in the list (or Index.zero if empty)
    member x.MinIndex = history.State.MinIndex

    /// The largest index contained in the list (or Index.zero if empty)
    member x.MaxIndex = history.State.MaxIndex

    /// Creates a new list initially holding the given elements.
    new(elements : seq<'T>) = ChangeableIndexList (IndexList.ofSeq elements)

    /// Creates a new empty list.
    new() = ChangeableIndexList IndexList.empty

    interface alist<'T> with
        member x.IsConstant = false
        member x.Content = history :> _
        member x.GetReader() = history.NewReader()
        member x.History = Some history

/// Changeable adaptive list that allows mutation by user-code and implements alist.
type clist<'T> = ChangeableIndexList<'T>