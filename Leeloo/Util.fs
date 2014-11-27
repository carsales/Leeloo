namespace Leeloo

[<AutoOpen>]
module Util =    
    type OptionBuilder() =
        member x.Bind(v,f) = Option.bind f v
        member x.Return v = Some v
        member x.ReturnFrom o = o
        member x.Zero() = Option.None

    let option = OptionBuilder()

    let otherwise<'a> (a: 'a) (ma: Option<'a>): 'a = 
        match ma with
        | None    -> a
        | Some(a) -> a

    let onlySome<'a> (xs: seq<Option<'a>>): seq<'a> = seq {
        for x in xs do
        if Option.isSome x 
        then yield Option.get x  }
        
    let flip f b a = f a b 
    let konst a _ = a
                    
    let (->>) f g a =
        f a |> ignore
        g a