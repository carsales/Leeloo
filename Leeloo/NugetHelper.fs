namespace Leeloo        

module NugetHelper =
    open NuGet
    open Fake    
    open CsProjHelper
                
    let versionsAvailable (package: IPackage) = 
        let mkVersionString (reference: IPackageAssemblyReference) =
            "v" + reference.TargetFramework.Version.ToString() 
            |> FrameworkVersion.ParseTargetFramework                 
                    
        let tuple2 a b = a,b   

        package.AssemblyReferences 
        |> Seq.map (fun ref -> mkVersionString ref |> Option.map (tuple2 ref))
        |> Seq.collect Option.toList

    let runNugetUpdate (packageDir: string) (packagesConfig: string) (projectFile: string)  =
        let localRepo = new LocalPackageRepository(packageDir)
        
        try                
            let doc = CsProjHelper.loadProj projectFile

            let dependencies = 
                getDependencies packagesConfig
                |> List.map (fun (name, version) -> localRepo.FindPackage(name, new SemanticVersion(version)))

            let projectVersion = frameworkVersion doc

            dependencies |> List.iter (fun package ->
                let packageVersions = versionsAvailable package

                let lowestVersion = 
                    packageVersions 
                    |> Seq.filter (fun (_, version) -> version <= projectVersion) 
                    |> Seq.last

                let reference, hintPath = findReference doc package.Id

                let packageVersionString = package.Version.ToString()
                let packageRef = fst lowestVersion
                let updatedPath = localRepo.Source @@ package.Id + "." + packageVersionString @@ packageRef.Path
                
                sprintf "Updating reference to %s to use %s" reference updatedPath |> Log <| []

                hintPath.SetValue updatedPath)

            doc.Save projectFile
        with 
        | e -> printfn "Error %A" e

        ()

