# fake-async C#
[![test workflow](https://github.com/Tolyandre/fake-async/workflows/test/badge.svg)](https://github.com/Tolyandre/fake-async/actions?query=workflow%3Atest)

Simulates passage of time to test asynchronous long-running code in synchronous way.

This library is inspired from Angular's [FakeAsync](https://angular.io/api/core/testing/fakeAsync).

```c#
async Task MethodUnderTest()
{
	await Task.Delay(TimeSpan.FromSeconds(10));

	flag = true;
	Assert.Equal(new DateTime(2020, 10, 20, 0, 0, 10), DateTime.Now); // DateTime.Now mocked

	await Task.Delay(TimeSpan.FromSeconds(10));
}

var fakeAsync = new FakeAsync();

bool flag = false;
fakeAsync.Now = new DateTime(2020, 10, 20);

fakeAsync.Isolate(async () =>
{
	var testing = MethodUnderTest();
	
	fakeAsync.Tick(TimeSpan.FromSeconds(9)); // skip 9s synchronously, no real delay
	Assert.False(flag); // flag still false

	fakeAsync.Tick(TimeSpan.FromSeconds(1));
	Assert.True(flag); // skip 1s more and now flag==true

	fakeAsync.Tick(TimeSpan.FromSeconds(10)); // skip remaining time

	await testing; // propagate any exceptions
});
```

Supported calls:
- `DateTime.Now`
- `DateTime.UtcNow`
- `Task.Run()`
- `Task.Factory.StartNew()` when TaskScheduler is ThreadPoolTaskScheduler (default)
- `new Task().Start()`
- `Task.Delay()`
- `Thread.Sleep()` (TODO)

`Stopwatch` is not changed, so it is possible to measure time.

# Known issues
Tiered compilation in .NET Core 3.0 is enabled by default. It occasionally overrides FakeAsync patches, so mock is not reliable. Currently, there is an [open issue](https://github.com/pardeike/Harmony/issues/307) in Harmony.Lib.
Workaround is to [disable](https://docs.microsoft.com/en-us/dotnet/core/run-time-config/compilation#tiered-compilation) tiered compilation in tests project:
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TieredCompilation>false</TieredCompilation>
  </PropertyGroup>

</Project>
```
After changing this setting Clean and Rebuild solution to take effect.


# Road map
- Suitable API to test asynchronous long running code
- Implement tick() (like Angular's [FakeAsync](https://angular.io/api/core/testing/fakeAsync))
- Allow tests with Polly [timeout policies](https://github.com/App-vNext/Polly#timeout)
- Make a Nuget package

# Credits
- [Harmony](https://github.com/pardeike/Harmony) - a library for patching, replacing and decorating .NET and Mono methods during runtime
