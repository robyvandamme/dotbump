// Copyright © Roby Van Damme.

using System.Text;
using DotBump.Commands.BumpDocker;
using DotBump.Commands.BumpDocker.DataModel;
using DotBump.Tests.TestHelpers;
using Moq;
using Serilog;
using Shouldly;

namespace DotBump.Tests.Commands.BumpDocker;

public class DockerfileServiceTests
{
    public class GetDockerfileManifest
    {
        [Fact]
        public void With_Single_From_Instruction_Returns_Entry()
        {
            ResetTempDirectory();
            const string content = "FROM mcr.microsoft.com/dotnet/sdk:11.0.100-rc.3 AS build\n";
            File.WriteAllText(TempPath("Dockerfile"), content);
            var service = CreateService();

            var manifest = service.GetDockerfileManifest(TempDirectory.AbsolutePath);

            var entry = manifest.Entries.ShouldHaveSingleItem();
            manifest.Warnings.ShouldBeEmpty();
            entry.ShouldSatisfyAllConditions(
                () => entry.Image.ShouldBe("mcr.microsoft.com/dotnet/sdk"),
                () => entry.RelativePath.ShouldBe("Dockerfile"),
                () => entry.LineNumber.ShouldBe(1),
                () => entry.OriginalTag.ShouldBe("11.0.100-rc.3"),
                () => entry.ParsedVersion.Major.ShouldBe(11),
                () => content.Substring(entry.TagStart, entry.TagLength).ShouldBe("11.0.100-rc.3"));
        }

        [Fact]
        public void With_Multi_Stage_Dockerfile_Returns_Supported_Image_Entries()
        {
            ResetTempDirectory();
            const string content =
                "# syntax=docker/dockerfile:1\n" +
                "FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0.101 AS build\n" +
                "FROM mcr.microsoft.com/dotnet/aspnet:10.0.3 AS runtime\n" +
                "FROM nginx:1.27.3\n";
            File.WriteAllText(TempPath("Dockerfile"), content);
            var service = CreateService();

            var manifest = service.GetDockerfileManifest(TempDirectory.AbsolutePath);

            manifest.Warnings.ShouldBeEmpty();
            manifest.Entries.Count.ShouldBe(2);
            manifest.Entries.ShouldContain(entry =>
                entry.LineNumber == 2
                && entry.OriginalTag == "10.0.101"
                && entry.VersionSource == DotnetImageVersionSource.Sdk);
            manifest.Entries.ShouldContain(entry =>
                entry.LineNumber == 3
                && entry.OriginalTag == "10.0.3"
                && entry.VersionSource == DotnetImageVersionSource.AspNetCore);
        }

        [Fact]
        public void With_AspNetCore_Image_Returns_AspNetCore_Source_Entry()
        {
            ResetTempDirectory();
            File.WriteAllText(
                TempPath("Dockerfile"),
                "FROM mcr.microsoft.com/dotnet/aspnet:10.0.15 AS base\n");
            var service = CreateService();

            var manifest = service.GetDockerfileManifest(TempDirectory.AbsolutePath);

            manifest.Entries.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
                entry => entry.Image.ShouldBe("mcr.microsoft.com/dotnet/aspnet"),
                entry => entry.VersionSource.ShouldBe(DotnetImageVersionSource.AspNetCore));
        }

        [Fact]
        public void With_Runtime_Image_Returns_Runtime_Source_Entry()
        {
            ResetTempDirectory();
            File.WriteAllText(
                TempPath("Dockerfile"),
                "FROM mcr.microsoft.com/dotnet/runtime:10.0.15 AS base\n");
            var service = CreateService();

            var manifest = service.GetDockerfileManifest(TempDirectory.AbsolutePath);

            manifest.Entries.ShouldHaveSingleItem().VersionSource.ShouldBe(DotnetImageVersionSource.Runtime);
        }

        [Fact]
        public void With_RuntimeDeps_Image_Returns_Runtime_Source_Entry()
        {
            ResetTempDirectory();
            File.WriteAllText(
                TempPath("Dockerfile"),
                "FROM mcr.microsoft.com/dotnet/runtime-deps:10.0.15 AS base\n");
            var service = CreateService();

            var manifest = service.GetDockerfileManifest(TempDirectory.AbsolutePath);

            manifest.Entries.ShouldHaveSingleItem().VersionSource.ShouldBe(DotnetImageVersionSource.Runtime);
        }

        [Fact]
        public void With_Mixed_Case_Image_Name_Returns_Entry()
        {
            ResetTempDirectory();
            File.WriteAllText(
                TempPath("Dockerfile"),
                "FROM MCR.MICROSOFT.COM/dotnet/ASPNET:10.0.15 AS base\n");
            var service = CreateService();

            var manifest = service.GetDockerfileManifest(TempDirectory.AbsolutePath);

            manifest.Entries.ShouldHaveSingleItem().VersionSource.ShouldBe(DotnetImageVersionSource.AspNetCore);
        }

        [Fact]
        public void With_Lowercase_From_Returns_Entry()
        {
            ResetTempDirectory();
            File.WriteAllText(
                TempPath("Dockerfile"),
                "from mcr.microsoft.com/dotnet/sdk:10.0.101 as build\n");
            var service = CreateService();

            var manifest = service.GetDockerfileManifest(TempDirectory.AbsolutePath);

            manifest.Entries.ShouldHaveSingleItem().OriginalTag.ShouldBe("10.0.101");
        }

        [Fact]
        public void With_Unsupported_Images_Returns_No_Entries_And_No_Warnings()
        {
            ResetTempDirectory();
            const string content =
                "FROM ubuntu:24.04 AS runtime\n" +
                "FROM nginx:1.27.3\n";
            File.WriteAllText(TempPath("Dockerfile"), content);
            var service = CreateService();

            var manifest = service.GetDockerfileManifest(TempDirectory.AbsolutePath);

            manifest.ShouldSatisfyAllConditions(
                () => manifest.Entries.ShouldBeEmpty(),
                () => manifest.Warnings.ShouldBeEmpty());
        }

        [Fact]
        public void With_Floating_AspNetCore_Tag_Adds_Warning()
        {
            ResetTempDirectory();
            File.WriteAllText(
                TempPath("Dockerfile"),
                "FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base\n");
            var service = CreateService();

            var manifest = service.GetDockerfileManifest(TempDirectory.AbsolutePath);

            manifest.ShouldSatisfyAllConditions(
                () => manifest.Entries.ShouldBeEmpty(),
                () => manifest.Warnings.ShouldHaveSingleItem().ShouldContain("fully versioned"));
        }

        [Fact]
        public void With_AspNetCore_Variant_Tag_Adds_Warning()
        {
            ResetTempDirectory();
            File.WriteAllText(
                TempPath("Dockerfile"),
                "FROM mcr.microsoft.com/dotnet/aspnet:10.0.15-noble AS base\n");
            var service = CreateService();

            var manifest = service.GetDockerfileManifest(TempDirectory.AbsolutePath);

            manifest.ShouldSatisfyAllConditions(
                () => manifest.Entries.ShouldBeEmpty(),
                () => manifest.Warnings.ShouldHaveSingleItem().ShouldContain("fully versioned"));
        }

        [Fact]
        public void With_Digest_Pinned_AspNetCore_Image_Adds_Warning()
        {
            ResetTempDirectory();
            File.WriteAllText(
                TempPath("Dockerfile"),
                "FROM mcr.microsoft.com/dotnet/aspnet:10.0.15@sha256:abcdef AS base\n");
            var service = CreateService();

            var manifest = service.GetDockerfileManifest(TempDirectory.AbsolutePath);

            manifest.ShouldSatisfyAllConditions(
                () => manifest.Entries.ShouldBeEmpty(),
                () => manifest.Warnings.ShouldHaveSingleItem().ShouldContain("digests are not supported"));
        }

        [Fact]
        public void With_Floating_Tag_Adds_Warning()
        {
            AssertUnsupportedTag("latest");
        }

        [Fact]
        public void With_Variant_Tag_Adds_Warning()
        {
            AssertUnsupportedTag("10.0.101-noble");
        }

        [Fact]
        public void With_Build_Argument_Tag_Adds_Warning()
        {
            AssertUnsupportedTag("${SDK_VERSION}");
        }

        [Fact]
        public void With_Missing_Tag_Adds_Warning()
        {
            ResetTempDirectory();
            File.WriteAllText(TempPath("Dockerfile"), "FROM mcr.microsoft.com/dotnet/sdk\n");
            var service = CreateService();

            var manifest = service.GetDockerfileManifest(TempDirectory.AbsolutePath);

            manifest.ShouldSatisfyAllConditions(
                () => manifest.Entries.ShouldBeEmpty(),
                () => manifest.Warnings.ShouldHaveSingleItem().ShouldContain("because it has no tag"));
        }

        [Fact]
        public void With_Digest_Adds_Warning()
        {
            ResetTempDirectory();
            File.WriteAllText(
                TempPath("Dockerfile"),
                "FROM mcr.microsoft.com/dotnet/sdk:10.0.101@sha256:abcdef AS build\n");
            var service = CreateService();

            var manifest = service.GetDockerfileManifest(TempDirectory.AbsolutePath);

            manifest.ShouldSatisfyAllConditions(
                () => manifest.Entries.ShouldBeEmpty(),
                () => manifest.Warnings.ShouldHaveSingleItem().ShouldContain("digests are not supported"));
        }

        [Fact]
        public void With_Trailing_At_Sign_Adds_Digest_Warning()
        {
            ResetTempDirectory();
            File.WriteAllText(
                TempPath("Dockerfile"),
                "FROM mcr.microsoft.com/dotnet/sdk:10.0.101@ AS build\n");
            var service = CreateService();

            var manifest = service.GetDockerfileManifest(TempDirectory.AbsolutePath);

            manifest.ShouldSatisfyAllConditions(
                () => manifest.Entries.ShouldBeEmpty(),
                () => manifest.Warnings.ShouldHaveSingleItem().ShouldContain("digests are not supported"));
        }

        [Fact]
        public void With_Multi_Line_From_Instruction_Skips_It()
        {
            ResetTempDirectory();
            File.WriteAllText(
                TempPath("Dockerfile"),
                "FROM mcr.microsoft.com/dotnet/sdk:10.0.101 \\\n    AS build\n");
            var service = CreateService();

            var manifest = service.GetDockerfileManifest(TempDirectory.AbsolutePath);

            manifest.ShouldSatisfyAllConditions(
                () => manifest.Entries.ShouldBeEmpty(),
                () => manifest.Warnings.ShouldHaveSingleItem().ShouldContain("multi-line FROM"));
        }

        [Fact]
        public void With_Comment_Inside_Continuation_Does_Not_Parse_Following_From()
        {
            ResetTempDirectory();
            const string content =
                "RUN apt-get update \\\n" +
                "# note about the install\n" +
                "FROM mcr.microsoft.com/dotnet/sdk:8.0.404\n";
            File.WriteAllText(TempPath("Dockerfile"), content);
            var service = CreateService();

            var manifest = service.GetDockerfileManifest(TempDirectory.AbsolutePath);

            manifest.ShouldSatisfyAllConditions(
                () => manifest.Entries.ShouldBeEmpty(),
                () => manifest.Warnings.ShouldBeEmpty());
        }

        [Fact]
        public void With_Blank_Line_Inside_Continuation_Does_Not_Parse_Following_From()
        {
            ResetTempDirectory();
            const string content =
                "RUN apt-get update \\\n" +
                "\n" +
                "FROM mcr.microsoft.com/dotnet/sdk:8.0.404\n";
            File.WriteAllText(TempPath("Dockerfile"), content);
            var service = CreateService();

            var manifest = service.GetDockerfileManifest(TempDirectory.AbsolutePath);

            manifest.ShouldSatisfyAllConditions(
                () => manifest.Entries.ShouldBeEmpty(),
                () => manifest.Warnings.ShouldBeEmpty());
        }

        [Fact]
        public void With_Comment_Inside_Multi_Line_From_Adds_Warning()
        {
            ResetTempDirectory();
            const string content =
                "FROM mcr.microsoft.com/dotnet/sdk:10.0.101 \\\n" +
                "# note about the stage\n" +
                "AS build\n";
            File.WriteAllText(TempPath("Dockerfile"), content);
            var service = CreateService();

            var manifest = service.GetDockerfileManifest(TempDirectory.AbsolutePath);

            manifest.ShouldSatisfyAllConditions(
                () => manifest.Entries.ShouldBeEmpty(),
                () => manifest.Warnings.ShouldHaveSingleItem().ShouldContain("multi-line FROM"));
        }

        [Fact]
        public void With_Completed_Continuation_Parses_Following_From()
        {
            ResetTempDirectory();
            const string content =
                "RUN echo \\\n" +
                "    hello\n" +
                "FROM mcr.microsoft.com/dotnet/sdk:10.0.101 AS build\n";
            File.WriteAllText(TempPath("Dockerfile"), content);
            var service = CreateService();

            var manifest = service.GetDockerfileManifest(TempDirectory.AbsolutePath);

            var entry = manifest.Entries.ShouldHaveSingleItem();
            manifest.Warnings.ShouldBeEmpty();
            entry.ShouldSatisfyAllConditions(
                () => entry.LineNumber.ShouldBe(3),
                () => entry.OriginalTag.ShouldBe("10.0.101"));
        }

        [Fact]
        public void With_Supported_File_Names_Returns_All_Entries()
        {
            ResetTempDirectory();
            const string content = "FROM mcr.microsoft.com/dotnet/sdk:10.0.101\n";
            File.WriteAllText(TempPath("Dockerfile"), content);
            File.WriteAllText(TempPath("Dockerfile.dev"), content);
            File.WriteAllText(TempPath("app.dockerfile"), content);
            File.WriteAllText(TempPath("Build.Dockerfile"), content);
            File.WriteAllText(TempPath("Containerfile"), content);
            File.WriteAllText(TempPath("notes.txt"), content);
            var service = CreateService();

            var manifest = service.GetDockerfileManifest(TempDirectory.AbsolutePath);

            manifest.Entries.Count.ShouldBe(5);
            manifest.Entries.Select(entry => entry.RelativePath).ShouldBe(
                ["Build.Dockerfile", "Containerfile", "Dockerfile", "Dockerfile.dev", "app.dockerfile"],
                ignoreOrder: true);
        }

        [Fact]
        public void With_Excluded_Directory_Ignores_Its_Files()
        {
            ResetTempDirectory();
            var excludedDirectory = Path.Combine(TempDirectory.AbsolutePath, "excluded");
            Directory.CreateDirectory(excludedDirectory);
            File.WriteAllText(
                Path.Combine(excludedDirectory, "Dockerfile"),
                "FROM mcr.microsoft.com/dotnet/sdk:10.0.101\n");
            File.WriteAllText(
                TempPath("Dockerfile"),
                "FROM mcr.microsoft.com/dotnet/sdk:10.0.101\n");
            var service = CreateService();

            var manifest = service.GetDockerfileManifest(
                TempDirectory.AbsolutePath,
                [excludedDirectory]);

            manifest.Entries.ShouldHaveSingleItem().RelativePath.ShouldBe("Dockerfile");
        }

        private static void AssertUnsupportedTag(string tag)
        {
            ResetTempDirectory();
            File.WriteAllText(
                TempPath("Dockerfile"),
                $"FROM mcr.microsoft.com/dotnet/sdk:{tag}\n");
            var service = CreateService();

            var manifest = service.GetDockerfileManifest(TempDirectory.AbsolutePath);

            manifest.ShouldSatisfyAllConditions(
                () => manifest.Entries.ShouldBeEmpty(),
                () => manifest.Warnings.ShouldHaveSingleItem());
        }
    }

    public class SaveDockerfileManifest
    {
        [Fact]
        public void With_Changed_File_Without_Captured_Text_Adds_Warning()
        {
            var manifest = new DockerfileManifest();
            var entry = new DockerImageEntry(
                "mcr.microsoft.com/dotnet/sdk",
                DotnetImageVersionSource.Sdk,
                "Dockerfile",
                "/repo/Dockerfile",
                1,
                new DockerTagSpan("10.0.100", 0, 8));
            manifest.Add(entry);
            manifest.SetTag(entry, "10.0.101");
            var service = CreateService();

            service.SaveDockerfileManifest(manifest);

            manifest.ShouldSatisfyAllConditions(
                () => manifest.Warnings.ShouldHaveSingleItem().ShouldContain("file content was not available"),
                () => manifest.Entries.ShouldHaveSingleItem().Tag.ShouldBe("10.0.101"));
        }

        [Fact]
        public void With_Changed_Tag_Updates_Only_The_Tag()
        {
            ResetTempDirectory();
            const string content =
                "# syntax=docker/dockerfile:1\n" +
                "FROM mcr.microsoft.com/dotnet/sdk:10.0.100 AS build\n" +
                "RUN dotnet publish -c Release\n";
            var filePath = TempPath("Dockerfile");
            File.WriteAllText(filePath, content);
            var service = CreateService();
            var manifest = service.GetDockerfileManifest(TempDirectory.AbsolutePath);
            manifest.SetTag(manifest.Entries.ShouldHaveSingleItem(), "10.0.101");

            service.SaveDockerfileManifest(manifest);

            File.ReadAllText(filePath).ShouldBe(content.Replace(
                "sdk:10.0.100",
                "sdk:10.0.101",
                StringComparison.Ordinal));
        }

        [Fact]
        public void With_Containerfile_Updates_The_Tag()
        {
            ResetTempDirectory();
            const string content =
                "FROM mcr.microsoft.com/dotnet/sdk:10.0.100 AS build\n" +
                "RUN dotnet build -c Release\n";
            var filePath = TempPath("Containerfile");
            File.WriteAllText(filePath, content);
            var service = CreateService();
            var manifest = service.GetDockerfileManifest(TempDirectory.AbsolutePath);
            manifest.SetTag(manifest.Entries.ShouldHaveSingleItem(), "10.0.101");

            service.SaveDockerfileManifest(manifest);

            File.ReadAllText(filePath).ShouldBe(content.Replace(
                "sdk:10.0.100",
                "sdk:10.0.101",
                StringComparison.Ordinal));
        }

        [Fact]
        public void With_Multiple_Occurrences_Updates_All_Of_Them()
        {
            ResetTempDirectory();
            const string content =
                "FROM mcr.microsoft.com/dotnet/sdk:10.0.100 AS build\n" +
                "FROM mcr.microsoft.com/dotnet/sdk:10.0.100 AS publish\n";
            var filePath = TempPath("Dockerfile");
            File.WriteAllText(filePath, content);
            var service = CreateService();
            var manifest = service.GetDockerfileManifest(TempDirectory.AbsolutePath);

            foreach (var entry in manifest.Entries)
            {
                manifest.SetTag(entry, "10.0.101");
            }

            service.SaveDockerfileManifest(manifest);

            var updated = File.ReadAllText(filePath);
            updated.ShouldBe(
                "FROM mcr.microsoft.com/dotnet/sdk:10.0.101 AS build\n" +
                "FROM mcr.microsoft.com/dotnet/sdk:10.0.101 AS publish\n");
        }

        [Fact]
        public void With_Crlf_File_Preserves_Line_Endings()
        {
            ResetTempDirectory();
            const string content =
                "FROM mcr.microsoft.com/dotnet/sdk:10.0.100 AS build\r\n" +
                "RUN dotnet publish -c Release\r\n";
            var filePath = TempPath("Dockerfile");
            File.WriteAllText(filePath, content);
            var service = CreateService();
            var manifest = service.GetDockerfileManifest(TempDirectory.AbsolutePath);
            manifest.SetTag(manifest.Entries.ShouldHaveSingleItem(), "10.0.101");

            service.SaveDockerfileManifest(manifest);

            var updated = File.ReadAllText(filePath);
            updated.ShouldSatisfyAllConditions(
                () => updated.ShouldContain("sdk:10.0.101 AS build\r\n"),
                () => updated.ShouldNotContain("sdk:10.0.100"));
        }

        [Fact]
        public void With_Utf8_Bom_Preserves_The_Bom()
        {
            ResetTempDirectory();
            var filePath = TempPath("Dockerfile");
            File.WriteAllText(
                filePath,
                "FROM mcr.microsoft.com/dotnet/sdk:10.0.100 AS build\n",
                new UTF8Encoding(true));
            var service = CreateService();
            var manifest = service.GetDockerfileManifest(TempDirectory.AbsolutePath);
            manifest.SetTag(manifest.Entries.ShouldHaveSingleItem(), "10.0.101");

            service.SaveDockerfileManifest(manifest);

            var bytes = File.ReadAllBytes(filePath);
            bytes.Take(3).ShouldBe(new UTF8Encoding(true).GetPreamble());
        }
    }

    private static LocalDirectory TempDirectory => new("./temp/dockerfile-service");

    private static string TempPath(string fileName)
    {
        return Path.Combine(TempDirectory.AbsolutePath, fileName);
    }

    private static void ResetTempDirectory()
    {
        TempDirectory.EnsureDirectoryDeleted();
        TempDirectory.EnsureDirectoryCreated();
    }

    private static DockerfileService CreateService()
    {
        return new DockerfileService(Mock.Of<ILogger>());
    }
}
