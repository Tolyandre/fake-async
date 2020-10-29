using FakeAsyncs;
using System;
using System.Threading.Tasks;

namespace ConsoleDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            bool flag = false;

            async Task MethodUnderTest()
            {
                await Task.Delay(TimeSpan.FromSeconds(10));

                flag = true;

                _ = Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(10));

                    Console.WriteLine("Hello from faked thread pool");
                });
            }

            var fakeAsync = new FakeAsync
            {
                UtcNow = new DateTime(2020, 10, 20),
            };
          
            fakeAsync.Isolate(async () =>
            {
                var testing = MethodUnderTest();

                // Current time is changed
                Console.WriteLine("{0}, flag={1}", DateTime.UtcNow, flag); // 2020-10-20 00:00:00, false

                // Skip 9s synchronously, no real delay
                fakeAsync.Tick(TimeSpan.FromSeconds(9));

                Console.WriteLine("{0}, flag={1}", DateTime.UtcNow, flag); // 2020-10-20 00:00:09, false

                // Skip 1s more
                fakeAsync.Tick(TimeSpan.FromSeconds(1));

                // Flag changed
                Console.WriteLine("{0}, flag={1}", DateTime.UtcNow, flag); // 2020-10-20 00:00:10, true

                // Now MethodUnderTest() is completed, await it to propagate any exceptions
                await testing;

                // Skip remaining time to display the message
                fakeAsync.Tick(TimeSpan.FromSeconds(10)); // Hello from faked thread pool
            });

            // Print real system time
            Console.WriteLine(DateTime.UtcNow);
        }
    }
}
