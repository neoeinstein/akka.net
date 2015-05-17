//-----------------------------------------------------------------------
// <copyright file="Prelude.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------
[<AutoOpen>]
module internal Akka.FSharp.Prelude

[<RequireQualifiedAccess>]
module internal Option = 
    let inline toNullable opt =
        match opt with
        | Some x -> System.Nullable x
        | None -> System.Nullable()
