namespace Leeloo

[<AutoOpen>]
module MultipassTypes =
    (* Function argument types *)
    type NugetableProjectsArg     = 
        { IsExcludedProject : ExcludedProjectPredicate }    
       
    and CreateNugetForProjectArgs = 
        { Version: string
        ; NuspecTemplatePath: string
        ; IncludeOutputFromProjectsNamed: string seq
        ; FrameworksToBuild: FrameworkVersion seq
        ; ShouldBuildForFramework: BuildForFrameworkPredicate
        ; SpecialisedReferences: SpecialisedReferencesMap
        ; DependencyCallback: DependenciesCallback }

    and BuildForAllFrameworksArgs = 
        { ShouldBuildForFramework: BuildForFrameworkPredicate
        ; Frameworks: FrameworkVersion seq }

    and PackageName = string
    and PackageVersion = string
    and ProjectName = string

    (* Functions provided to argument types given name *)
    and ExcludedProjectPredicate   = string -> bool
    and BuildForFrameworkPredicate = FrameworkVersion -> string -> bool
    and SpecialisedReferencesMap   = string -> (string * string) list

    (* Update callbacks *)
    and NugetableCallback    = NugetableProjectsArg -> NugetableProjectsArg
    and CreateNugetCallback  = CreateNugetForProjectArgs -> CreateNugetForProjectArgs
    and BuildForAllCallback  = BuildForAllFrameworksArgs -> BuildForAllFrameworksArgs
    and DependenciesCallback = ProjectName -> PackageName -> PackageVersion -> PackageVersion
        
    (* Defaults *)
    let defaultNugetableProjectsArg = { IsExcludedProject = LeelooDefaults.isExcludedProject; }

    let defaultCreateNugetForProjectArgs =
        { Version = "0.0.1"; NuspecTemplatePath = ""
        ; IncludeOutputFromProjectsNamed = []
        ; FrameworksToBuild = [V451]
        ; ShouldBuildForFramework = LeelooDefaults.shouldBuildForProject
        ; SpecialisedReferences = konst [] 
        ; DependencyCallback = fun _ _ v -> v }

    let defaultBuildForAllFrameworksArgs = 
        { ShouldBuildForFramework = LeelooDefaults.shouldBuildForProject
        ; Frameworks = LeelooDefaults.frameworksToBuild } 
