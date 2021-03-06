﻿namespace Leeloo

module LeelooDefaults =
    let sourcesPath           = "src"
    let installedPackagesPath = "packages"
    
    let buildPath             = "leeloo/build"

    let testPath              = "leeloo/test"

    let packagingWorkPath     = "leeloo/work"
    let packageOutputPath     = "leeloo/nupkgs"

    let copiedPackagesPath    = "leeloo/packages"

    let nugetExePath          = "leeloo/Nuget.exe"

    let frameworksToBuild = [V451]

    let isExcludedProject (s:string) = false
    let shouldBuildForProject _ _    = true

