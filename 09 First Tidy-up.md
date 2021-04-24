## First tidy-up

There are a few tasks that I should now probably do in this repo just to keep things tidy & up-to-date etc.

* Add missing file references to projects and solution file (documentation files, paket.reference etc) for easy access from the IDE.
* Set the test projects to be libraries by adding `<OutputType>Library</OutputType>` to the project files
* Update all the Nuget Packages
  * Remove the version numbers from paket.dependencies
  * Limit the packages to the frameworks used in the solution by adding `framework: auto-detect`
  * `dotnet paket update`