using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace YoFi.SampleGen;

public class SampleDataRunner
{
    public class Definition
    {
        public string Name { get; set; }
        public string Path { get; set; }
    }

    public class Project
    {
        public string Name { get; set; }

        public string Path { get; set; }

        public Output[] Outputs { get; set; }
    }

    public class Output
    {
        public string Load { get; set; }
        public SampleDataGenerator.SaveOptions Save { get; set; }
    }

    public Definition[] Definitions { get; set; }
    public Project[] Projects { get; set; }

    private Dictionary<string,SampleDataGenerator> Generators;

    public void Load()
    {
        Generators = Definitions.ToDictionary(
            x => x.Name,
            x => 
            {
                using var stream = File.Open(x.Path,FileMode.Open);
                var generator = new SampleDataGenerator();
                generator.LoadDefinitions(stream);
                generator.GenerateTransactions();
                generator.GeneratePayees();
                generator.GenerateBudget();
                return generator;
            });
    }

    public IEnumerable<string> Save(Project project, string directory)
    {
        var result = new List<string>();
        Directory.CreateDirectory(directory);
        foreach(var output in project.Outputs)
        {
            var filenamecomponents = new List<string>()
            {
                "SampleData",
                SampleDataPattern.Year.ToString(),
                output.Load
            };
            if (output.Save.TxOnly)
                filenamecomponents.Add("Tx");
            if (output.Save.Month > 0)
                filenamecomponents.Add("Month" + output.Save.Month.ToString("D2"));

            var filename = directory + "/" + string.Join('-',filenamecomponents) + $".{output.Save.Type.ToString().ToLowerInvariant()}";
            result.Add(filename);

            File.Delete(filename);
            using var stream = File.Open(filename,FileMode.Create);

            Generators[output.Load].Save(stream,output.Save);
        }

        return result;
    }
}
