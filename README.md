# VSProjTypeExtractor

Visual Studio project type GUID extractor


## What is it all about?

**_VSProjTypeExtractor_** can be used in **[FASTBuild](http://fastbuild.org/docs/home.html)** together with the new _VSProjectExternal_ function to automatically determine the `ProjectTypeGuid` from the actual external visual studio project, instead of providing it in the BFF file. For this to happen, instead of providing a value, the reserved string '**_auto_**' should be used, like in the example below:
```
VSProjectExternal( 'SomeExternal-vsproj' )
{
	.ExternalProjectPath = 'path_to\ExternalProject.csproj'
	.ProjectTypeGuid = 'auto'
}
```


## How does it work?

**_VSProjTypeExtractor_** consists of 2 DLLs, `VSProjTypeExtractorManaged.dll` which is written in C# and is responsible of the actual work, by automating Visual Studio in the background for loading the project file just to be able to query the type GUID, and the `VSProjTypeExtractor.dll` **C** wrapper which can be used in any native apllication to call this functionality.


## Requirements, Usage

**_VSProjTypeExtractor_** targets .NET Framework v4.7.2, so the 2 DLLs which you can download from the [Releases](https://github.com/lucianm/VSProjTypeExtractor/releases) section will need this version of the .NET framework to be installed on your system. Just place them somewhere in the 'PATH' or along with FBuild.exe (a build supporting _VSProjectExternal_, post-v0.99 in any case) in the same directory.


© 2019 Lucian Muresan
