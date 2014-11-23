namespace Fake.Multitargeting

module FsFs =
    open System.IO

    type Dir = DirectoryInfo Option
    type File = FileInfo Option

    module Dirs =
        let ofDirectoryInfo (di: DirectoryInfo) : Dir = 
            option { if di.Exists then return di }            
            
        let ofString (name: string): Dir = ofDirectoryInfo (new DirectoryInfo(name))

        let child (name: string) (dir: Dir): Dir = option {
            let! d = dir 

            let di = new DirectoryInfo(Path.Combine [|d.FullName; name|])

            return! ofDirectoryInfo di }

        let childDI (name: string) (dir: DirectoryInfo): Dir = option {
            let di = new DirectoryInfo(Path.Combine [|dir.FullName; name|])

            if di.Exists then return di }

        let fileNamed (fileName: string) (dir: Dir): File = option {
            let! d = dir

            let fi = new FileInfo(Path.Combine [|d.FullName; fileName|])

            if fi.Exists then return fi }

        let fileNamedInDirectoryInfo (fileName: string) (dir: DirectoryInfo): File = option {
            let! d = ofDirectoryInfo dir
            
            let fi = new FileInfo(Path.Combine [|dir.FullName; fileName|])
            
            if fi.Exists then return fi }

        let getChildrenWithPatternAndOption (pattern: string) (searchOption: SearchOption) (dir: Dir) = 
            option {
                let! d = dir
                return d.EnumerateDirectories(pattern, searchOption) |> Seq.map (ofDirectoryInfo) }
            |> Option.fold (flip konst) Seq.empty

        let getChildrenWithPattern (pattern: string) = 
            getChildrenWithPatternAndOption pattern SearchOption.TopDirectoryOnly

        let getChildren = getChildrenWithPattern "*"
        
    module Files =
        let asString (fi: File) = option {
            let! file = fi
            return file.OpenText().ReadToEnd() }
        
    let (</>)  = flip Dirs.child
    let (</)   = flip Dirs.fileNamed
    let (<//>) = flip Dirs.childDI
    let (<//)  = flip Dirs.fileNamedInDirectoryInfo
    let (<**>) (d: Dir) (f: Dir -> 'a) = 
        d |> Dirs.getChildren 
        |> onlySome |> Seq.map Some 
        |> Seq.map f 