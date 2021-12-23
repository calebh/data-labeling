using System;
using System.Collections.Generic;
using System.Linq;

namespace DataLabeling
{
    public class Program
    {
        public static void Main(string[] args) {
            string exampleFile = "images/all.json";
            List<IOExample> examples = Json.JsonMethods.Read(exampleFile);
            List<List<MapApply>> ast = Synthesize.DoSynthesis(examples);

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