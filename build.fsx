#I @"packages/FAKE/tools/"
#r "FakeLib.dll"

open Fake

let outDir = __SOURCE_DIRECTORY__ </> "output"

Target "Clean" (fun _ ->
    CleanDirs [outDir]
)

Target "Build" (fun _ ->
    !! "**/*.fsproj"
      |> MSBuildRelease outDir "Build"
      |> Log "AppBuild-Output: "
)

"Clean"
  ==> "Build"
  
RunTargetOrDefault "Build"
