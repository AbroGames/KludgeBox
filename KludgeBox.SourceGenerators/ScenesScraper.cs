using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis;

namespace KludgeBox.SourceGenerators;

internal static class ScenesScraper
{
    public static IEnumerable<ProjectFile> GetFilesIn(Compilation compilation, string gdProjectRoot, string[] include, string[] exclude)
    {
        throw new NotImplementedException();
    }
}