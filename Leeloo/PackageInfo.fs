namespace Leeloo
    
type PackageInfo = {
    Name: string
    Version: Fake.SemVerHelper.SemVerInfo Option
    Directory: FsFs.Dir
    Frameworks: FrameworkVersion list }
    with 
        static member FromTuple(name, ver, di, frameworks) 
            = { Name = name
                Version = ver
                Directory = di
                Frameworks = frameworks}

        static member FromDirectory (dir: FsFs.Dir) = 
            option {
                let! di = dir
                let foldr f s xs = List.foldBack f xs s
                let isNumeric = fst << System.Int32.TryParse
      
                let partitionVersionNumbers (a: string) = 
                    function
                    | false, version, name when isNumeric a -> 
                        false, a::version, name
                    | _, version, name -> 
                        true, version, a::name
      
                let empty = (false, [], [])

                let produce (_, version: string list, name: string list) =
                    let name = String.concat "." name
          
                    let version = 
                        match version with
                        | [] -> None
                        | version -> Some << Fake.SemVerHelper.parse << String.concat "." <| version 
            
                    let dirs = di.EnumerateDirectories("*", System.IO.SearchOption.AllDirectories)
                            |> Seq.map (fun di -> di.Name |> FrameworkVersion.ParseNugetPath)
                            |> onlySome
                            |> Seq.toList

                    PackageInfo.FromTuple(name, version, dir, dirs)
      
                return di.Name.Split([|'.'|])
                       |> Array.toList
                       |> foldr partitionVersionNumbers empty
                       |> produce 
            }
    end

