using System;
using System.Collections.Generic;
using System.Linq;

namespace DataLabeling
{
    public class Program
    {
        public static void Main(string[] args) {
            BoundingBox foo = new BoundingBox(0.0, 0.0, 0.0, 0.0);
            string exampleFile = "images/example.json";
            List<IOExample> examples = Json.JsonMethods.Read(exampleFile);
            ProgramAst ast = Synthesize.DoSynthesis(examples);

            Console.WriteLine("Done");
        }
    }
}