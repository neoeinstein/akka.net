//-----------------------------------------------------------------------
// <copyright file="FaultHandling.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------
namespace Akka.FSharp

open Akka.Actor
open System
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Linq.QuotationEvaluation


[<AutoOpen>]
module Logging = 
    open Akka.Event
    
    /// Logs a message using configured Akka logger.
    let log (level : LogLevel) (mailbox : Actor<'Message>) (msg : string) : unit = 
        let logger = mailbox.Log.Force()
        logger.Log(level, msg)
    
    /// Logs a message at Debug level using configured Akka logger.
    let inline logDebug mailbox msg = log LogLevel.DebugLevel mailbox msg
    
    /// Logs a message at Info level using configured Akka logger.
    let inline logInfo mailbox msg = log LogLevel.InfoLevel mailbox msg
    
    /// Logs a message at Warning level using configured Akka logger.
    let inline logWarning mailbox msg = log LogLevel.WarningLevel mailbox msg
    
    /// Logs a message at Error level using configured Akka logger. 
    let inline logError mailbox msg = log LogLevel.ErrorLevel mailbox msg
    
    /// Logs an exception message at Error level using configured Akka logger.
    let inline logException mailbox (e : exn) = log LogLevel.ErrorLevel mailbox (e.Message)
    
    open Printf
    
    let inline private doLogf level (mailbox : Actor<'Message>) msg = mailbox.Log.Value.Log(level, msg) |> ignore
    
    /// Logs a message using configured Akka logger.
    let inline logf (level : LogLevel) (mailbox : Actor<'Message>) = kprintf (doLogf level mailbox)
    
    /// Logs a message at Debug level using configured Akka logger.
    let inline logDebugf mailbox = kprintf (doLogf LogLevel.DebugLevel mailbox)
    
    /// Logs a message at Info level using configured Akka logger.
    let inline logInfof mailbox = kprintf (doLogf LogLevel.InfoLevel mailbox)
    
    /// Logs a message at Warning level using configured Akka logger.
    let inline logWarningf mailbox = kprintf (doLogf LogLevel.WarningLevel mailbox)
    
    /// Logs a message at Error level using configured Akka logger. 
    let inline logErrorf mailbox = kprintf (doLogf LogLevel.ErrorLevel mailbox)

open Akka.Util

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

type Strategy = 
    
    /// <summary>
    /// Returns a supervisor strategy appliable only to child actor which faulted during execution.
    /// </summary>
    /// <param name="decider">Used to determine a actor behavior response depending on exception occurred.</param>
    static member OneForOne(decider : exn -> Directive) : SupervisorStrategy = 
        upcast OneForOneStrategy(System.Func<_, _>(decider))
    
    /// <summary>
    /// Returns a supervisor strategy appliable only to child actor which faulted during execution.
    /// </summary>
    /// <param name="retries">Defines a number of times, an actor could be restarted. If it's a negative value, there is not limit.</param>
    /// <param name="timeout">Defines time window for number of retries to occur.</param>
    /// <param name="decider">Used to determine a actor behavior response depending on exception occurred.</param>
    static member OneForOne(decider : exn -> Directive, ?retries : int, ?timeout : TimeSpan) : SupervisorStrategy = 
        upcast OneForOneStrategy
                   (OptionHelper.optToNullable retries, OptionHelper.optToNullable timeout, System.Func<_, _>(decider))
    
    /// <summary>
    /// Returns a supervisor strategy appliable only to child actor which faulted during execution.
    /// </summary>
    /// <param name="retries">Defines a number of times, an actor could be restarted. If it's a negative value, there is not limit.</param>
    /// <param name="timeout">Defines time window for number of retries to occur.</param>
    /// <param name="decider">Used to determine a actor behavior response depending on exception occurred.</param>
    static member OneForOne(decider : Expr<exn -> Directive>, ?retries : int, ?timeout : TimeSpan) : SupervisorStrategy = 
        upcast OneForOneStrategy
                   (OptionHelper.optToNullable retries, OptionHelper.optToNullable timeout, ExprDecider decider)
    
    /// <summary>
    /// Returns a supervisor strategy appliable to each supervised actor when any of them had faulted during execution.
    /// </summary>
    /// <param name="decider">Used to determine a actor behavior response depending on exception occurred.</param>
    static member AllForOne(decider : exn -> Directive) : SupervisorStrategy = 
        upcast AllForOneStrategy(System.Func<_, _>(decider))
    
    /// <summary>
    /// Returns a supervisor strategy appliable to each supervised actor when any of them had faulted during execution.
    /// </summary>
    /// <param name="retries">Defines a number of times, an actor could be restarted. If it's a negative value, there is not limit.</param>
    /// <param name="timeout">Defines time window for number of retries to occur.</param>
    /// <param name="decider">Used to determine a actor behavior response depending on exception occurred.</param>
    static member AllForOne(decider : exn -> Directive, ?retries : int, ?timeout : TimeSpan) : SupervisorStrategy = 
        upcast AllForOneStrategy
                   (OptionHelper.optToNullable retries, OptionHelper.optToNullable timeout, System.Func<_, _>(decider))
    
    /// <summary>
    /// Returns a supervisor strategy appliable to each supervised actor when any of them had faulted during execution.
    /// </summary>
    /// <param name="retries">Defines a number of times, an actor could be restarted. If it's a negative value, there is not limit.</param>
    /// <param name="timeout">Defines time window for number of retries to occur.</param>
    /// <param name="decider">Used to determine a actor behavior response depending on exception occurred.</param>
    static member AllForOne(decider : Expr<exn -> Directive>, ?retries : int, ?timeout : TimeSpan) : SupervisorStrategy = 
        upcast AllForOneStrategy
                   (OptionHelper.optToNullable retries, OptionHelper.optToNullable timeout, ExprDecider decider)
