using System;
using System.Collections.Generic;
using System.Linq;

namespace DataLabeling
{
    public class Program
    {
        public static void Main(string[] args) {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            string exampleFile = "images/lifeguard.json";
            List<IOExample> examples = Json.JsonMethods.Read(exampleFile);

            Console.WriteLine("Is color important in these images? (y/n)");
            string isColorImportantAnswer = Console.ReadLine();
            bool enableColorSynthesis = isColorImportantAnswer == "y" || isColorImportantAnswer == "yes";

            Console.WriteLine("Is the relative placement of objects important in these images? (y/n)");
            string isPlacementImportantAnswer = Console.ReadLine();
            bool enablePlacementSynthesis = isPlacementImportantAnswer == "y" || isPlacementImportantAnswer == "yes";

            var asts = Synthesize.DoSynthesis(examples, enableColorSynthesis, enablePlacementSynthesis);

            foreach (List<LabelApply> labelApplyEqClass in asts.Item1) {
                Console.WriteLine("Equivalence class for " + labelApplyEqClass[0].Action.LabelName);
                foreach (LabelApply program in labelApplyEqClass) {
                    Console.WriteLine(program);
                }
            }

            foreach (List<GroupApply> groupApplyEqClass in asts.Item2) {
                Console.WriteLine("Equivalence class for " + groupApplyEqClass[0].Action.LabelName);
                foreach (GroupApply program in groupApplyEqClass) {
                    Console.WriteLine(program);
                }
            }

            Console.ReadLine();
        }
    }
}