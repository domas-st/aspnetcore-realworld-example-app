var target = Argument("target", "Default");
var tag = Argument("tag", "cake");

var binaryDir = Directory("./src/Conduit/bin");
var objectDir = Directory("./src/Conduit/obj");
var publishDir = Directory("./publish");

Task("Clean")
   .Does(() =>
{
	CleanDirectory(binaryDir);
	CleanDirectory(objectDir);
	CleanDirectory(publishDir);
});

Task("Restore")
  .Does(() =>
{
    DotNetCoreRestore(".");
});

Task("Build")
  .Does(() =>
{
    DotNetCoreBuild(".");
});

Task("Test")
  .Does(() =>
{
    var files = GetFiles("tests/**/*.csproj");
    foreach(var file in files)
    {
        DotNetCoreTest(file.ToString());
    }
});

Task("Publish")
  .Does(() =>
{
    var settings = new DotNetCorePublishSettings
    {
        Framework = "netcoreapp2.0",
        Configuration = "Release",
        OutputDirectory = "./publish",
        Runtime = "win7-x64", //https://docs.microsoft.com/de-de/dotnet/core/rid-catalog
        VersionSuffix = tag
    };
                
    DotNetCorePublish("src/Conduit", settings);
});

Task("Default")
    .IsDependentOn("Restore")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .IsDependentOn("Publish");

 Task("Rebuild")
    .IsDependentOn("Restore")
    .IsDependentOn("Build");


RunTarget(target);