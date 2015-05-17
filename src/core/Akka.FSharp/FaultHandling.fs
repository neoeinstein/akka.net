//-----------------------------------------------------------------------
// <copyright file="FaultHandling.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------
module Akka.FSharp.Supervision

open Akka.Actor
open Akka.Util
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Linq.QuotationEvaluation

type ExprDeciderSurrogate(serializedExpr : byte array) = 
    member __.SerializedExpr = serializedExpr
    interface ISurrogate with
        member this.FromSurrogate _ = 
            let fsp = Nessos.FsPickler.FsPickler.CreateBinary()
            let expr = 
                (Serialization.deserializeFromBinary fsp (this.SerializedExpr) typeof<Expr<exn -> Directive>>) :?> Expr<exn -> Directive>
            ExprDecider(expr) :> ISurrogated

and ExprDecider(expr : Expr<exn -> Directive>) = 
    member __.Expr = expr
    member private this.Compiled = lazy this.Expr.Compile () ()
    
    interface IDecider with
        member this.Decide(e : exn) : Directive = this.Compiled.Value(e)
    
    interface ISurrogated with
        member this.ToSurrogate _ = 
            let fsp = Nessos.FsPickler.FsPickler.CreateBinary()
            ExprDeciderSurrogate(Serialization.serializeToBinary fsp this.Expr) :> ISurrogate

type Retry =
    | Unlimited
    | MaxRetries of retries : int
    static member toCount = function
        | Unlimited -> -1
        | MaxRetries retries -> retries

type Timeout =
    | Infinite
    | MaxTime of timeout : System.TimeSpan
    static member toMilliseconds = function
        | Infinite -> System.Threading.Timeout.Infinite
        | MaxTime ts -> int ts.TotalMilliseconds

[<RequireQualifiedAccess>]
module OneForOne =
    /// <summary>
    /// Returns a supervisor strategy appliable only to child actor which faulted during execution.
    /// </summary>
    /// <param name="retries">Defines a number of times, an actor could be restarted.</param>
    /// <param name="timeout">Defines time window for number of retries to occur.</param>
    /// <param name="decider">Used to determine a actor behavior response depending on exception occurred.</param>
    let create' retries timeout decider : SupervisorStrategy =
        upcast OneForOneStrategy (Retry.toCount retries, Timeout.toMilliseconds timeout, System.Func<_, _>(decider))

    /// <summary>
    /// Returns a supervisor strategy appliable only to child actor which faulted during execution.
    /// </summary>
    /// <param name="decider">Used to determine a actor behavior response depending on exception occurred.</param>
    let create decider =
        create' Unlimited Infinite decider

    /// <summary>
    /// Returns a supervisor strategy appliable only to child actor which faulted during execution.
    /// </summary>
    /// <param name="retries">Defines a number of times, an actor could be restarted.</param>
    /// <param name="timeout">Defines time window for number of retries to occur.</param>
    /// <param name="decider">Used to determine a actor behavior response depending on exception occurred.</param>
    let ofExpr' retries timeout decider : SupervisorStrategy =
        upcast OneForOneStrategy (Retry.toCount retries, Timeout.toMilliseconds timeout, ExprDecider decider)

    /// <summary>
    /// Returns a supervisor strategy appliable only to child actor which faulted during execution.
    /// </summary>
    /// <param name="decider">Used to determine a actor behavior response depending on exception occurred.</param>
    let ofExpr decider =
        ofExpr' Unlimited Infinite decider

[<RequireQualifiedAccess>]
module AllForOne =
    /// <summary>
    /// Returns a supervisor strategy appliable to each supervised actor when any of them had faulted during execution.
    /// </summary>
    /// <param name="retries">Defines a number of times, an actor could be restarted. If it's a negative value, there is not limit.</param>
    /// <param name="timeout">Defines time window for number of retries to occur.</param>
    /// <param name="decider">Used to determine a actor behavior response depending on exception occurred.</param>
    let create' retries timeout decider : SupervisorStrategy =
        upcast AllForOneStrategy (Retry.toCount retries, Timeout.toMilliseconds timeout, System.Func<_, _>(decider))

    /// <summary>
    /// Returns a supervisor strategy appliable to each supervised actor when any of them had faulted during execution.
    /// </summary>
    /// <param name="decider">Used to determine a actor behavior response depending on exception occurred.</param>
    let create decider =
        create' Unlimited Infinite decider

    /// <summary>
    /// Returns a supervisor strategy appliable to each supervised actor when any of them had faulted during execution.
    /// </summary>
    /// <param name="retries">Defines a number of times, an actor could be restarted. If it's a negative value, there is not limit.</param>
    /// <param name="timeout">Defines time window for number of retries to occur.</param>
    /// <param name="decider">Used to determine a actor behavior response depending on exception occurred.</param>
    let ofExpr' retries timeout decider : SupervisorStrategy =
        upcast AllForOneStrategy (Retry.toCount retries, Timeout.toMilliseconds timeout, ExprDecider decider)

    /// <summary>
    /// Returns a supervisor strategy appliable to each supervised actor when any of them had faulted during execution.
    /// </summary>
    /// <param name="decider">Used to determine a actor behavior response depending on exception occurred.</param>
    let ofExpr decider =
        ofExpr' Unlimited Infinite decider

