// See https://aka.ms/new-console-template for more information

using System.Text.Json;
using DotLurker.CodeGenerator;

var generator = new CodeGenerator();

var trainingDataFolder = "training_data";

if (!Directory.Exists(trainingDataFolder))
    Directory.CreateDirectory(trainingDataFolder);

var numberOfEntitiesToGenerate = 5000;
for (var i = 0; i < numberOfEntitiesToGenerate; i++)
{
    var tree = generator.GenerateTree(343569);
    var s = generator.Generate(tree);
    var json = JsonSerializer.Serialize(tree);
    File.WriteAllText(Path.Combine(trainingDataFolder, $"{i}.txt"), s);
    File.WriteAllText(Path.Combine(trainingDataFolder, $"{i}.json"), json);
    Console.WriteLine(i);
}