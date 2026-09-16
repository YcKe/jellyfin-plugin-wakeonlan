using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Plugin.WakeOnLan.Tests
{
    public class WolServiceTests
    {
        private static readonly byte[] ExpectedMac = [0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF];

        [Theory]
        [InlineData("AA:BB:CC:DD:EE:FF")]
        [InlineData("aa:bb:cc:dd:ee:ff")]
        [InlineData("AA-BB-CC-DD-EE-FF")]
        [InlineData("AABB.CCDD.EEFF")]
        [InlineData("AABBCCDDEEFF")]
        [InlineData("  aa bb cc dd ee ff  ")]
        public void TryParseMacAddress_AcceptsCommonNotations(string input)
        {
            var ok = WolService.TryParseMacAddress(input, out var bytes);

            Assert.True(ok);
            Assert.Equal(ExpectedMac, bytes);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("AA:BB")]
        [InlineData("AA:BB:CC:DD:EE")]
        [InlineData("AA:BB:CC:DD:EE:FF:00")]
        [InlineData("ZZ:BB:CC:DD:EE:FF")]
        [InlineData("AABBCCDDEEF")]
        [InlineData("192.168.1.10")]
        public void TryParseMacAddress_RejectsInvalidInput(string? input)
        {
            var ok = WolService.TryParseMacAddress(input, out var bytes);

            Assert.False(ok);
            Assert.Empty(bytes);
        }

        [Fact]
        public void BuildMagicPacket_HasStandardLayout()
        {
            var packet = WolService.BuildMagicPacket(ExpectedMac);

            Assert.Equal(102, packet.Length);
            Assert.All(packet.Take(6), b => Assert.Equal(0xFF, b));

            for (var repetition = 0; repetition < 16; repetition++)
            {
                var offset = 6 + (repetition * 6);
                Assert.Equal(ExpectedMac, packet.Skip(offset).Take(6));
            }
        }

        [Theory]
        [InlineData("not-a-mac", 9)]
        [InlineData("AA:BB:CC:DD:EE:FF", 0)]
        [InlineData("AA:BB:CC:DD:EE:FF", 65536)]
        public void SendWolPacket_ReturnsFalseWithoutSendingOnInvalidInput(string mac, int port)
        {
            var sent = WolService.SendWolPacket(mac, port, NullLogger.Instance);

            Assert.False(sent);
        }
    }
}
