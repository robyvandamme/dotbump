// Copyright © Roby Van Damme.

using DotBump.Commands.BumpDocker;
using Shouldly;

namespace DotBump.Tests.Commands.BumpDocker;

public class DotnetImagesTests
{
    public class TryGetVersionSource
    {
        [Theory]
        [InlineData("mcr.microsoft.com/dotnet/sdk", "Sdk")]
        [InlineData("mcr.microsoft.com/dotnet/aspnet", "AspNetCore")]
        [InlineData("mcr.microsoft.com/dotnet/runtime", "Runtime")]
        [InlineData("mcr.microsoft.com/dotnet/runtime-deps", "Runtime")]
        [InlineData("MCR.MICROSOFT.COM/dotnet/ASPNET", "AspNetCore")]
        public void With_Supported_Image_Returns_Version_Source(
            string image,
            string expected)
        {
            var result = DotnetImages.TryGetVersionSource(image, out var versionSource);

            result.ShouldSatisfyAllConditions(
                () => result.ShouldBeTrue(),
                () => versionSource.ShouldBe(Enum.Parse<DotnetImageVersionSource>(expected)));
        }

        [Theory]
        [InlineData("nginx")]
        [InlineData("mcr.microsoft.com/dotnet/sdk-nightly")]
        [InlineData("ubuntu")]
        [InlineData("")]
        [InlineData(" ")]
        public void With_Unsupported_Image_Returns_False(string image)
        {
            var result = DotnetImages.TryGetVersionSource(image, out _);

            result.ShouldBeFalse();
        }
    }
}
