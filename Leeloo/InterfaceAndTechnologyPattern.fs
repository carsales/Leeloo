namespace Leeloo

module InterfaceAndTechnologyPattern =
    open MultipassTypes
    open Fake

    /// Filters projects in paths.SourceDirectory, excludes *.Tests by default
    let projectsByConvention (paths: LeelooPaths) (callback: NugetableCallback) (interfaceProjectName: string) = 
        let testProj (name: string) = name.Contains "Tests"
    
        let config = callback <| defaultNugetableProjectsArg

        let isChild (name: string) 
            = [testProj; config.IsExcludedProject] 
           |> Seq.map    (fun f -> not << f)
           |> Seq.forall (fun f -> f name)

        let projectPattern = paths.SourcesPath @@ interfaceProjectName + ".*/"

        let projects = !! projectPattern
                       |> Seq.filter isChild
                       |> Seq.map (fun dir -> (directoryInfo dir).Name)
                       |> Seq.toList

        trace << (+) "Will build packages: " 
              << String.concat "; " 
              <| projects

        projects 