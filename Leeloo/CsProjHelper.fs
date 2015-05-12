namespace Leeloo

module CsProjHelper =
    open System
    open System.Linq
    open System.Xml.Linq
    open System.IO

    let xn = XName.op_Implicit
    let ns = let ns = XNamespace.op_Implicit "http://schemas.microsoft.com/developer/msbuild/2003"
             fun s -> ns + s

    type CsProjRepresentation(fileName: string, xdoc: XDocument) = 
        member val Doc      = xdoc     with get
        member val FileName = fileName with get

        member x.HintPaths with get() = lazy (xdoc.Descendants(ns "HintPath").Select(fun hp -> hp))
                

    let loadProj (fileName: string): CsProjRepresentation = CsProjRepresentation(fileName, XDocument.Load fileName)
    
    let frameworkVersion (projectDoc: CsProjRepresentation) =
        projectDoc.Doc.Descendants(ns "TargetFrameworkVersion").First().Value 
        |> FrameworkVersion.ParseTargetFramework
        |> Option.get

    let extractPackageName (hintPath: string) = 
        let firstDirAfterPackages = 
            hintPath.Split('\\')
            |> Seq.skipWhile ((<>) "packages")
            |> Seq.skip 1
            |> Seq.head

        let isDigit = fst << Int32.TryParse

        let piecesNotAllDigits =
            firstDirAfterPackages.Split('.')
            |> Array.rev
            |> Seq.skipWhile isDigit

        piecesNotAllDigits
        |> Seq.toArray
        |> Array.rev
        |> String.concat "."

    let nugetReferences (doc: CsProjRepresentation) =
        let hintPathSmellsLikeNuget (hintPath: XElement) =
            hintPath.Value.Contains(@"..\packages")

        doc.HintPaths.Force()
     |> Seq.filter hintPathSmellsLikeNuget
     |> Seq.map (fun hintPath -> extractPackageName hintPath.Value, hintPath)
    
    let packageNameMatchesReference (packageName: string) ((refName,elem): string * XElement): bool =
        let hintPath = elem.Value

        let packageNameMatchesWithIncludeName() = refName.Equals(packageName)
        let pathMatches() = hintPath.Contains(@"\packages\"+ packageName + ".") // Should be good enough?

        packageNameMatchesWithIncludeName() || pathMatches()

    let findReference (doc: CsProjRepresentation) packageName =
        let references = nugetReferences doc |> Seq.toList

        references
     |> List.tryFind (packageNameMatchesReference packageName)
     |> Option.otherwise (fun () -> raise(new Exception(sprintf "Couldn't find a reference for package named %s in %s" packageName doc.FileName)))           
     
    let projectReferences (doc: CsProjRepresentation) =
        let projRefNode         = ns "ProjectReference"
        let nameNode            = ns "Name"
        let value (n: XElement) = n.Value

        doc.Doc.Descendants(projRefNode).Descendants(nameNode).Select(value)

