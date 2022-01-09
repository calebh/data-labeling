using System;
using System.Collections.Generic;
using System.Linq;

namespace DataLabeling
{
    public class Program
    {
        public static void Main(string[] args) {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            string exampleFile = "images/guitarist.json";
            List<IOExample> examples = Json.JsonMethods.Read(exampleFile);

            Console.WriteLine("Is color important in these images? (y/n)");
            string isColorImportantAnswer = Console.ReadLine();
            bool enableColorSynthesis = isColorImportantAnswer == "y" || isColorImportantAnswer == "yes";

            Console.WriteLine("Is the relative placement of objects important in these images? (y/n)");
            string isPlacementImportantAnswer = Console.ReadLine();
            bool enablePlacementSynthesis = isPlacementImportantAnswer == "y" || isPlacementImportantAnswer == "yes";

            List<List<MapApply>> ast = Synthesize.DoSynthesis(examples, enableColorSynthesis, enablePlacementSynthesis);

            foreach (List<MapApply> mapApplyEquivalenceClass in ast) {
                Console.WriteLine("Equivalence class for " + mapApplyEquivalenceClass[0].Action.LabelName);
                foreach (MapApply program in mapApplyEquivalenceClass) {
                    Console.WriteLine(program);
                }
            }

            Console.ReadLine();
        }
    }
}