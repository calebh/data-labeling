using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLabeling
{
    public static class Synthesize
    {
        private static List<ObjectLiteral> GenerateBaseLibrary(List<IOExample> examples) {
            HashSet<ObjectLiteral> result = new HashSet<ObjectLiteral>();
            foreach (IOExample example in examples) {
                foreach (BoundingBox b in example.GetBoxes()) {
                    result.Add(example.GetBase(b));
                }
            }
            return result.ToList();
        }

        private static List<ObjectLiteral> GeneratePreciseLibrary(List<IOExample> examples) {
            HashSet<ObjectLiteral> result = new HashSet<ObjectLiteral>();
            foreach (IOExample example in examples) {
                foreach (BoundingBox b in example.GetBoxes()) {
                    result.UnionWith(example.GetPrecise(b));
                }
            }
            return result.ToList();
        }

        public static ProgramAst DoSynthesis(List<IOExample> examples) {
            using (Context ctx = new Context(new Dictionary<string, string>() { { "model", "true" } })) {
                int varIndex = 0;
                Func<BoolExpr> getFreshToggleVar = () => {
                    BoolExpr ret = ctx.MkBoolConst("v" + varIndex.ToString());
                    varIndex++;
                    return ret;
                };

                Func<RealExpr> getFreshRealVar = () => {
                    RealExpr ret = ctx.MkRealConst("r" + varIndex.ToString());
                    varIndex++;
                    return ret;
                };

                List<ObjectLiteral> baseLibrary = GenerateBaseLibrary(examples);
                List<ObjectLiteral> preciseLibrary = GeneratePreciseLibrary(examples);

                List<MapApply> synthesizedMaps = new List<MapApply>();

                foreach (ObjectLiteral preciseLabel in preciseLibrary) {
                    int numClauses = 5;
                    int quantifierNestedLevel = 0;
                    bool synthesisSucceeded = false;
                    bool incrementQuantifierLevel = true;

                    while (!synthesisSucceeded) {

                        Func<int, List<ObjectVariable>, Tuple<Ir, Ir>> recursivelyGenerate = null;
                        recursivelyGenerate = (int level, List<ObjectVariable> accumLevels) => {
                            ObjectVariable levelVar = new ObjectVariable("x" + level.ToString());
                            ObjectVariable nextLevelVar = new ObjectVariable("x" + (level - 1).ToString());
                            List<ObjectVariable> nextAccumLevel = new List<ObjectVariable>(accumLevels);
                            nextAccumLevel.Add(nextLevelVar);

                            List<Ir> clausesDnf = new List<Ir>();
                            List<Ir> clausesCnf = new List<Ir>();

                            for (int clauseI = 0; clauseI < numClauses; clauseI++) {
                                List<Ir> clauseDnf = new List<Ir>();
                                List<Ir> clauseCnf = new List<Ir>();

                                foreach (ObjectLiteral label in baseLibrary) {
                                    clauseDnf.Add(new MatchIr(levelVar, label, false, getFreshToggleVar()));
                                    clauseCnf.Add(new MatchIr(levelVar, label, false, getFreshToggleVar()));
                                    clauseDnf.Add(new MatchIr(levelVar, label, true, getFreshToggleVar()));
                                    clauseCnf.Add(new MatchIr(levelVar, label, true, getFreshToggleVar()));
                                }

                                for (int i = 0; i < accumLevels.Count; i++) {
                                    for (int j = i + 1; j < accumLevels.Count; j++) {
                                        clauseDnf.Add(new MatchIr(accumLevels[i], accumLevels[j], false, getFreshToggleVar()));
                                        clauseCnf.Add(new MatchIr(accumLevels[i], accumLevels[j], false, getFreshToggleVar()));
                                        clauseDnf.Add(new MatchIr(accumLevels[i], accumLevels[j], true, getFreshToggleVar()));
                                        clauseCnf.Add(new MatchIr(accumLevels[i], accumLevels[j], true, getFreshToggleVar()));
                                    }
                                }

                                if (level > 0) {
                                    Tuple<Ir, Ir> recResult = recursivelyGenerate(level - 1, nextAccumLevel);
                                    Ir dnfRes = recResult.Item1;
                                    Ir cnfRes = recResult.Item2;

                                    clauseDnf.Add(new AnyIr(nextLevelVar, dnfRes, getFreshToggleVar()));
                                    clauseDnf.Add(new AllIr(nextLevelVar, dnfRes, getFreshToggleVar()));
                                    clauseCnf.Add(new AnyIr(nextLevelVar, cnfRes, getFreshToggleVar()));
                                    clauseCnf.Add(new AllIr(nextLevelVar, cnfRes, getFreshToggleVar()));
                                }

                                clausesDnf.Add(new AndIr(clauseDnf, null));
                                clausesCnf.Add(new OrIr(clauseCnf, null));
                            }

                            Ir dnf = new OrIr(clausesDnf, null);
                            Ir cnf = new AndIr(clausesCnf, null);

                            return Tuple.Create(dnf, cnf);
                        };

                        ObjectVariable outermostVariable = new ObjectVariable("x" + quantifierNestedLevel.ToString());

                        Tuple<Ir, Ir> irResult = recursivelyGenerate(quantifierNestedLevel, new List<ObjectVariable>() { outermostVariable });
                        Ir dnf = irResult.Item1;
                        Ir cnf = irResult.Item2;

                        Optimize s_dnf = ctx.MkOptimize();
                        Optimize s_cnf = ctx.MkOptimize();

                        foreach (IOExample example in examples) {
                            foreach (BoundingBox box in example.GetBoxes()) {
                                var initEnv = ImmutableDictionary<ObjectVariable, Tuple<BoundingBox, ObjectLiteral>>.Empty.Add(outermostVariable, Tuple.Create(box, example.GetBase(box)));
                                var compiled_dnf_0 = dnf.Apply(initEnv, example);
                                var compiled_dnf = compiled_dnf_0.ToZ3(ctx, Form.DNF);
                                var compiled_cnf_0 = cnf.Apply(initEnv, example);
                                var compiled_cnf = compiled_cnf_0.ToZ3(ctx, Form.CNF);
                                bool actionApplied = example.GetPrecise(box).Contains(preciseLabel);
                                BoolExpr actionAppliedZ3 = ctx.MkBool(actionApplied);
                                s_dnf.Add(ctx.MkIff(compiled_dnf, actionAppliedZ3));
                                s_cnf.Add(ctx.MkIff(compiled_cnf, actionAppliedZ3));
                            }
                        }

                        int smallestObjective = int.MaxValue;

                        Optimize.Handle minimizationDnf = s_dnf.MkMinimize(dnf.ToggleVarSum(ctx));
                        Optimize.Handle minimizationCnf = s_cnf.MkMinimize(cnf.ToggleVarSum(ctx));

                        MapApply? bestProgram = null;

                        Action<Optimize, Ir, Optimize.Handle> runZ3 = (Optimize s, Ir nf, Optimize.Handle minimization) => {
                            if (s.Check() == Status.SATISFIABLE) {
                                Model m = s.Model;
                                int objectiveValue = ((IntNum)minimization.Lower).Int;
                                if (objectiveValue < smallestObjective) {
                                    smallestObjective = objectiveValue;
                                    BooleanAst? synthesizedPred = nf.Compile(m);
                                    bestProgram = new MapApply(preciseLabel, new Filter(new PredicateLambda(outermostVariable, synthesizedPred), new AllObjects()));
                                }
                            }
                        };

                        runZ3(s_dnf, dnf, minimizationDnf);
                        runZ3(s_cnf, cnf, minimizationCnf);

                        if (bestProgram != null) {
                            synthesizedMaps.Add(bestProgram);
                            synthesisSucceeded = true;
                        } else {
                            if (incrementQuantifierLevel) {
                                quantifierNestedLevel++;
                            } else {
                                numClauses += 5;
                            }
                            incrementQuantifierLevel = !incrementQuantifierLevel;
                        }
                    }
                }

                return new ProgramAst(synthesizedMaps);
            }
        }
    }
}
