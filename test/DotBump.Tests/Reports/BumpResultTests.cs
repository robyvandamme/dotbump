// Copyright © Roby Van Damme.

using DotBump.Reports;
using Shouldly;

namespace DotBump.Tests.Reports;

public class BumpResultTests
{
    public class DisplayId
    {
        [Fact]
        public void With_Condition_Appends_Condition()
        {
            var result = new BumpResult("MyPackage", "1.0.0")
            {
                Condition = "'$(TargetFramework)' == 'net10.0'",
            };

            result.DisplayId.ShouldBe("MyPackage (when '$(TargetFramework)' == 'net10.0')");
        }

        [Fact]
        public void Without_Condition_Returns_Id()
        {
            var result = new BumpResult("MyPackage", "1.0.0");

            result.DisplayId.ShouldBe("MyPackage");
        }
    }

    public class ToString_
    {
        [Fact]
        public void With_Condition_Includes_Condition()
        {
            var result = new BumpResult("MyPackage", "1.0.0")
            {
                NewVersion = "1.1.0",
                Condition = "'$(TargetFramework)' == 'net10.0'",
            };

            result.ToString().ShouldBe("MyPackage (when '$(TargetFramework)' == 'net10.0'): 1.0.0 > 1.1.0");
        }

        [Fact]
        public void Without_Condition_Omits_Condition()
        {
            var result = new BumpResult("MyPackage", "1.0.0")
            {
                NewVersion = "1.1.0",
            };

            result.ToString().ShouldBe("MyPackage: 1.0.0 > 1.1.0");
        }
    }
}
