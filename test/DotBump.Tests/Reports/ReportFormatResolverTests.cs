// Copyright © Roby Van Damme.

using DotBump.Reports;
using Shouldly;

namespace DotBump.Tests.Reports;

public class ReportFormatResolverTests
{
    public class Resolve
    {
        [Theory]
        [InlineData("report.md")]
        [InlineData("report.MD")]
        [InlineData("report.markdown")]
        [InlineData("./artifacts/bump-tools.Markdown")]
        public void With_Markdown_Extension_Returns_Markdown(string path)
        {
            ReportFormatResolver.Resolve(path).ShouldBe(ReportFormat.Markdown);
        }

        [Theory]
        [InlineData("report.json")]
        [InlineData("report.JSON")]
        [InlineData("report.txt")]
        [InlineData("report")]
        [InlineData("./artifacts/bump-sdk.result")]
        public void With_Non_Markdown_Extension_Returns_Json(string path)
        {
            ReportFormatResolver.Resolve(path).ShouldBe(ReportFormat.Json);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void With_Empty_Path_Throws_ArgumentException(string path)
        {
            Should.Throw<ArgumentException>(() => ReportFormatResolver.Resolve(path));
        }
    }
}
