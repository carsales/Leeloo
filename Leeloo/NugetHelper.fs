namespace Leeloo

module NugetHelper =
    open NuGet
    open Fake
    open System.Linq
    open System.Xml.Linq
    
    let ns = XNamespace.op_Implicit "http://schemas.microsoft.com/developer/msbuild/2003"
    let xn = XName.op_Implicit

    let frameworkVersion (projectDoc: XDocument) =
        projectDoc.Descendants(ns + "TargetFrameworkVersion").First().Value 
        |> FrameworkVersion.ParseTargetFramework
        |> Option.get

    let findReference (doc: XDocument) packageName =
        let includesPackageName (name: string) (elem: XElement) =
            elem.Attribute(xn "Include").Value.Contains(name) 
        doc.Descendants(ns + "Reference").First(fun elem -> includesPackageName packageName elem)
                
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
            let doc = XDocument.Load(projectFile)

            let dependencies = 
                getDependencies packagesConfig
                |> List.map (fun (name, version) -> localRepo.FindPackage(name, new SemanticVersion(version)))

            let projectVersion = frameworkVersion doc

            dependencies |> List.iter (fun package ->
                printfn "Updating %A" package

                let packageVersions = versionsAvailable package
                
                printfn "Available versions: %A" packageVersions

                let lowestVersion = 
                    packageVersions 
                    |> Seq.filter (fun (_, version) -> version <= projectVersion) 
                    |> Seq.last

                printfn "Will use %A as project is %A" lowestVersion projectVersion
                    
                let reference = findReference doc package.Id

                let hintPath = reference.Element(ns + "HintPath")
                printfn "Path %A" hintPath.Value

                let packageVersionString = package.Version.ToString()
                let packageRef = fst lowestVersion
                let updatedPath = localRepo.Source @@ package.Id + "." + packageVersionString @@ packageRef.Path

                printfn "Will update reference to %A" updatedPath

                printfn "Updating %A" reference
                hintPath.SetValue updatedPath)

            doc.Save projectFile
        with 
        | e -> printfn "Error %A" e

        ()

