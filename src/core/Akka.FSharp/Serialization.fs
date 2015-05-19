//-----------------------------------------------------------------------
// <copyright file="Serialization.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------
namespace Akka.FSharp

open Akka.Actor
open System
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Linq.QuotationEvaluation

module internal LinqExpression = 
    open System.Linq.Expressions
    open Microsoft.FSharp.Linq
    
    let inline internal (|Lambda|_|) (expr : Expression) = 
        match expr with
        | :? LambdaExpression as l -> Some(l.Parameters, l.Body)
        | _ -> None
    
    let inline internal (|Call|_|) (e : Expression) = 
        match e with
        | :? MethodCallExpression as c -> Some(c.Object, c.Method, c.Arguments)
        | _ -> None
    
    let inline internal (|Method|) (e : System.Reflection.MethodInfo) = e.Name
    
    let inline internal (|Invoke|_|) e = 
        match e with
        | Call(o, Method("Invoke"), _) -> Some o
        | _ -> None
    
    let inline internal (|Ar|) (p : System.Collections.ObjectModel.ReadOnlyCollection<Expression>) = Array.ofSeq p
    
    let inline internal toExpression<'Actor> expr = 
        match expr with
        | Lambda(_, Invoke(Call(null, Method "ToFSharpFunc", Ar [| Lambda(_, p) |])))
        | Call(null, Method "ToFSharpFunc", Ar [| Lambda(_, p) |]) -> 
            Expression.Lambda(p, [||]) :?> System.Linq.Expressions.Expression<System.Func<'Actor>>
        | _ -> failwith "Doesn't match"

    let ofQuotation<'Actor> =
        QuotationEvaluator.ToLinqExpression
        >> toExpression<'Actor>

module internal Serialization = 
    open Nessos.FsPickler
    open Akka.Serialization
    
    let internal serializeToBinary (fsp : BinarySerializer) o = 
        use stream = new System.IO.MemoryStream()
        fsp.Serialize(o.GetType(), stream, o)
        stream.ToArray()
    
    let internal deserializeFromBinary (fsp : BinarySerializer) (bytes : byte array) (t : Type) = 
        use stream = new System.IO.MemoryStream(bytes)
        fsp.Deserialize(t, stream)
    
    // used for top level serialization
    type internal ExprSerializer(system) = 
        inherit Serializer(system)
        let fsp = FsPickler.CreateBinary()
        override __.Identifier = 9
        override __.IncludeManifest = true
        override __.ToBinary(o) = serializeToBinary fsp o
        override __.FromBinary(bytes, t) = deserializeFromBinary fsp bytes t
    
    let internal exprSerializationSupport (system : ActorSystem) = 
        let serializer = ExprSerializer(system :?> ExtendedActorSystem)
        system.Serialization.AddSerializer(serializer)
        system.Serialization.AddSerializationMap(typeof<Expr>, serializer)
