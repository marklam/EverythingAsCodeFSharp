## Using Paket
Because `paket` is very good at keeping all the NuGet package versions in the solution up-to-date and consistent, we'll switch this repo over to use paket.

Create the dotnet tool manifest, install paket, and convert the repo to use paket.
```cmd
cd EverythingAsCodeFSharp
dotnet new tool-manifest
dotnet tool install paket
dotnet paket convert-from-nuget
```
This will change a few things in the folder.
- There will ba a dotnet-tools.json which specifies `paket` as a tool for this repo. After cloning a repo, you can use `dotnet tool restore` to install all the tools used in the repo.
- There's a `paket.dependencies` file that lists all the NuGet packages in the repo, and their versions.
- The `paket.lock` file is used to fix all the NuGet packages to a certain versions. This will mean that a `dotnet restore` on a fresh clone will download the versions from your commit.
- The `.fsproj` files have had their `<PackageReference>` nodes removed, and the package names are now listed in a `paket.references` file in each folder.
- An msbuild file `.paket\Paket.Restore.targets` has been added to help in the build process.