using System;
using NUnit.Framework;

namespace SGH.Tests
{
    public class LiveRegionConfigurationTests
    {
        [TestCase(" ", "asia")]
        [TestCase("sg", "asia")]
        [TestCase(" SG ", "asia")]
        [TestCase("ASIA", "asia")]
        [TestCase("eu", "eu")]
        public void RegionConfigurationUsesPhotonCodesAndMigratesLegacySingaporeAlias(string configured, string expected)
        {
            string previous = Environment.GetEnvironmentVariable("SGH_PHOTON_REGION");
            try
            {
                Environment.SetEnvironmentVariable("SGH_PHOTON_REGION", configured);
                Assert.That(LiveTestConfig.Region, Is.EqualTo(expected));
            }
            finally { Environment.SetEnvironmentVariable("SGH_PHOTON_REGION", previous); }
        }
    }
}
