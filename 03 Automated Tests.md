## Add an automated test
I'd already begun moving on to the next stages when I realised that I was failing at one of my objectives. 
I had tested the Azure Function locally with the `func` command, and a web browser - but of course I wouldn't remember to do that every time I made a change. I need to have checks like that done automatically (or at least automated).

So, time to add a test project.
```cmd
mkdir WordValues.Azure.Tests
cd WordValues.Azure.Tests
dotnet new xunit --language F#
```
That produces an F# project using [xUnit](https://xunit.net/) as the test framework.

### Testing under `func.exe`
Http-triggered functions can be tested by connecting to the endpoint that is created when `func start` is used in the Azure Function project's source folder.

To do this, we can use xUnit's [class fixtures](https://xunit.net/docs/shared-context#class-fixture) 
- Run `func start` in the class fixture with a known port.
- When a test needs to access the function, check the `func.exe` process has not exited, and try to connect to that port. If all is ok, return some connection context.
- When the fixture is disposed, kill the process we started.
### Testing the Http-Triggered function
In the test case, we can use the class fixture to get the base Url (which will check that the hosting process hasn't gone away)

[FsHttp](https://github.com/ronaldschlenker/FsHttp) provides helpers to make the request/response code simple, and then the status code and response body can be checked in the test.

It's a slow test to run compared to real unit-tests, but it's faster than running func manually and pasting urls into the web browser, and it can be done automatically by just running `dotnet test`