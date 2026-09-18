using Microsoft.Extensions.Hosting;
using Promissio.BatchProcessor;

using IHost host = BatchProcessorService.BuildHost(args);
await host.RunAsync();
