## Make a Visual Studio solution (.sln) file
As we've now got more than one project, I'll create a solution file to keep them together and make managing their inter-dependencies easier.

Just using the "Open Folder" feature in Visual Studio, or the Ionide extension in Visual Studio Code could work here too, 
but I find Visual Studio's build tooling more straightforward to use.

### Create the solution file
```cmd
dotnet new sln --name EverythingAsCodeFSharp
```

### Set up the projects
This can be done in Visual Studio by opening the .sln file and using the Solution Explorer.
Alternatively, the projects can be arranged from the command-line.
```cmd
dotnet sln add WordValues.Azure WordValues.Azure.Tests
dotnet add WordValues.Azure.Tests reference WordValues.Azure
```

If I wasn't making a solution file at all, the `dotnet add ... reference ...` command would still be important,
to ensure that the function project gets built before the tests run under `dotnet test`.
