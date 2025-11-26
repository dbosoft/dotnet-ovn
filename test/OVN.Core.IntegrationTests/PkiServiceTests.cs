using AwesomeAssertions;
using Dbosoft.OVN.SimplePki;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dbosoft.OVN.Core.IntegrationTests;

public class PkiServiceTests
{
    private readonly ISystemEnvironment _systemEnvironment;
    private readonly IPkiService _pkiService;

    public PkiServiceTests()
    {
        _systemEnvironment = new TestSystemEnvironment(
            NullLoggerFactory.Instance,
            Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()).Replace(@"\", "/"));
        _pkiService = new PkiService(_systemEnvironment);
    }

    [Fact]
    public async Task Initialize_InitializesPki()
    {
        await _pkiService.InitializeAsync();

        _systemEnvironment.FileSystem.FileExists(
                new OvsFile("/var/lib/openvswitch/pki/dotnetovnca", "cacert.pem"))
            .Should().BeTrue();
        _systemEnvironment.FileSystem.FileExists(
                new OvsFile("/var/lib/openvswitch/pki/dotnetovnca/private", "cakey.pem"))
            .Should().BeTrue();
    }

    [Fact]
    public async Task GenerateChassisPki_GeneratesPkiForChassis()
    {
        await _pkiService.InitializeAsync();

        var result = await _pkiService.GenerateChassisPkiAsync("test-chassis");

        result.PrivateKey.Should().NotBeNullOrEmpty();
        result.Certificate.Should().NotBeNullOrEmpty();
        result.CaCertificate.Should().NotBeNullOrEmpty();
    }
}
