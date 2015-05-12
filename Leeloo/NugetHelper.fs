namespace Leeloo        

module NugetHelper =
    open NuGet
    open Fake    
    open CsProjHelper

    let debug = log

    let versionsAvailable (package: IPackage) = 
        let mkVersionString (reference: IPackageAssemblyReference) =
            option {
                "Testing reference" |> debug
                let! ref = Option.noneIfNull reference
                "Found ref" |> debug
                let! frameworkVersion = Option.noneIfNull ref.TargetFramework

                let version = "v" + frameworkVersion.Version.ToString()

                sprintf "Attempting to parse framework %s" version |> debug
                let! parsedVersion = FrameworkVersion.ParseTargetFramework version

                sprintf "Parsed version %A" parsedVersion |> debug
                return parsedVersion
            }
                    
        let tuple2 a b = a,b   
        
        sprintf "Assembly references %A for %A" package.Id package.AssemblyReferences |> debug

        package.AssemblyReferences 
        |> Seq.map (fun ref -> mkVersionString ref |> (fun ms -> debug (sprintf "Got version string %A" ms); ms) |> Option.map (tuple2 ref))
        |> Seq.collect Option.toList

    let dependenciesFromRepo (localRepo: LocalPackageRepository) (packagesConfig: string) = 
        getDependencies packagesConfig
     |> List.map (fun (name, version) -> localRepo.FindPackage(name, new SemanticVersion(version)))

    let referenceForVersion (version: FrameworkVersion) (package: IPackage) =
        let isDll (s: IPackageFile) = s.Path.EndsWith ".dll"
        let usableVersion (version: FrameworkVersion) (packageFile: IPackageFile) =
            match FrameworkVersion.FromTargetFramework packageFile.TargetFramework with
            | None -> false
            | Some(target) -> target <= version

        let files = package.GetLibFiles()
                 |> Seq.filter isDll
                 |> Seq.toList 

        let appropriateVersions = files 
                               |> List.filter (usableVersion version)
                               |> List.rev

        match appropriateVersions with
        | highest::_ -> Some(highest)
        | [] -> Option.ofSeq files 


    let updateDependenciesInProjectFile (packagesPath: string) (doc: CsProjRepresentation) (projectVersion: FrameworkVersion) (projectFile: string) (package: IPackage) = 
        sprintf "Loading package %A with version %A" package.Id projectVersion |> debug

        match referenceForVersion projectVersion package with
        | None -> sprintf "No package verison found for %A in project of %A, leaving" package.Id projectFile |> debug
        | Some(packageRef) ->                        
            let reference, elem = findReference doc package.Id

            let packageVersionString = package.Version.ToString()
            let updatedPath = packagesPath @@ package.Id + "." + packageVersionString @@ packageRef.Path
                
            sprintf "Updating reference to %s to use %s" reference updatedPath |> debug

            elem.SetValue updatedPath

    let runNugetUpdate (packageDir: string) (packagesConfig: string) (projectFile: string)  =

        try                
            let doc = CsProjHelper.loadProj projectFile

            let projectVersion = frameworkVersion doc
            
            let localRepo = new LocalPackageRepository(packageDir)

            dependenciesFromRepo localRepo packagesConfig
            |> Seq.iter (updateDependenciesInProjectFile localRepo.Source doc projectVersion projectFile)

            doc.Doc.Save projectFile
        with 
        | e -> printfn "Error %A" e

        ()

