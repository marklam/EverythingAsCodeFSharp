## Second tidy-up
- Some of the Deployment stuff was named "Deployment" or "Deployment.Aws", so I renamed "Deployment" to "Deployment.Azure".
- Moved the Pulumi.Extras.fs file into its own library used from the Deployment.* projects, I used `dotnet new classlib --language F# --framework netcoreapp3.1` because the Pulumi libraries target netcoreapp3.1

