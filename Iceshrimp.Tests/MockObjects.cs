using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Iceshrimp.Utils.DependencyInjection;

namespace Iceshrimp.Tests;

public static class MockObjects
{
	public static readonly ASActor ASActor = new()
	{
		Id               = $"https://example.org/users/{IdHelpers.GenerateSnowflakeId()}",
		Type             = ASActor.Types.Person,
		Url              = new ASLink("https://example.org/@test"),
		Username         = "test",
		DisplayName      = "Test account",
		IsCat            = false,
		IsDiscoverable   = true,
		IsLocked         = true,
		WebfingerAddress = "test@example.org"
	};

	public static readonly User User = new() { Id = IdHelpers.GenerateSnowflakeId() };

	public static readonly RSA Keypair = RSA.Create(4096);

	public static readonly UserKeypair UserKeypair = new()
	{
		UserId     = User.Id,
		PrivateKey = Keypair.ExportPkcs8PrivateKeyPem(),
		PublicKey  = Keypair.ExportSubjectPublicKeyInfoPem()
	};

	private static readonly ServiceProvider  DefaultServiceProvider = GetServiceProvider();
	public static           IServiceProvider ServiceProvider => DefaultServiceProvider.CreateScope().ServiceProvider;

	[SuppressMessage("ReSharper", "RedundantSuppressNullableWarningExpression")]
	private static ServiceProvider GetServiceProvider()
	{
		var config = new ConfigurationManager();
		config.AddIniStream(AssemblyHelpers.GetEmbeddedResourceStream("configuration.ini"));

		IServiceCollection collection = new ServiceCollection();
		collection.AddServicesFromAssembly<Config>(config, typeof(Config).Assembly!);
		collection.ConfigureServicesWithValidation<Config>(config);
		collection.ConfigureServices();

		return collection.BuildServiceProvider();
	}
}