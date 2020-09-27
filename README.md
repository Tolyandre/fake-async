# fake-time
Run tests with fake time


```c#

await FakeTime.Isolate(async time =>
{
	Task delayTask = Task.Delay(TimeSpan.FromSeconds(10));
	
	time.Tick(TimeSpan.FromSeconds(10));
	
	await delayTask; // returns immediately
});

```


## Road map
- Suitable API to test time-dependent code
- Implement tick() (like Angular's [FakeAsync](https://angular.io/api/core/testing/fakeAsync))
- Allow tests with Polly [timeout policies](https://github.com/App-vNext/Polly#timeout)
- Make a nuget package

# Credits
- [Harmony](https://github.com/pardeike/Harmony) - a library for patching, replacing and decorating
.NET and Mono methods during runtime
