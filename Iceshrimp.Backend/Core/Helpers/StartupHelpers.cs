using System.Reflection;

namespace Iceshrimp.Backend.Core.Helpers;

public static class StartupHelpers
{
	public static void ParseCliArguments(string[] args)
	{
		if (args.Contains("-h") || args.Contains("--help") || args.Contains("-?"))
		{
			Console.WriteLine($"""
			                   Usage: ./{typeof(Program).Assembly.GetName().Name} [options...]
			                    --migrate             Apply pending migrations, then exit
			                    --migrate-and-start   Apply pending migrations, then start the application
			                    --printconfig         Print the example config, then exit
			                    --help                Print information on available command line arguments
			                   """);
			Environment.Exit(0);
		}

		if (args.Contains("--printconfig"))
		{
			var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			var config      = File.ReadAllText(Path.Join(assemblyDir, "configuration.ini"));
			Console.WriteLine(config);
			Environment.Exit(0);
		}
	}
}