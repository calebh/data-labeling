using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Z3;
using System.Collections.Immutable;

namespace DataLabeling
{
    public enum Form { DNF, CNF }

    public abstract class Ir
    {
        public BoolExpr ToggleVar;

        protected Ir(BoolExpr toggleVar) {
            ToggleVar = toggleVar;
        }

        public abstract Ir Apply(ImmutableDictionary<ObjectVariable, Tuple<BoundingBox, ObjectLiteral>> env, IOExample example);

        public abstract BoolExpr ToZ3(Context ctx, Form form);

        public abstract ArithExpr? ToggleVarSum(Context ctx);

        public abstract BooleanAst? Compile(Model z3Solution);

        protected bool ToggleSolution(Model z3Solution) {
            return z3Solution.Eval(ToggleVar).BoolValue == Z3_lbool.Z3_L_TRUE;
        }
    }

    public class OrIr : Ir
    {
        public List<Ir> Inner { get; private set; }

        public OrIr(List<Ir> inner, BoolExpr toggleVar) : base(toggleVar) {
            Inner = inner;
        }

        public override Ir Apply(ImmutableDictionary<ObjectVariable, Tuple<BoundingBox, ObjectLiteral>> env, IOExample example) {
            return new OrIr(Inner.Select(node => node.Apply(env, example)).ToList(), ToggleVar);
        }

        public override ArithExpr ToggleVarSum(Context ctx) {
            if (ToggleVar != null) {
                ArithExpr total = (ArithExpr)ctx.MkITE(ToggleVar, ctx.MkInt(1), ctx.MkInt(0));
                foreach (Ir r in Inner) {
                    total = total + r.ToggleVarSum(ctx);
                }
                return total;
            } else {
                ArithExpr? total = null;
                foreach (Ir r in Inner) {
                    ArithExpr? res = r.ToggleVarSum(ctx);
                    if (total == null) {
                        total = res;
                    } else if (res != null) {
                        total = total + r.ToggleVarSum(ctx);
                    }
                }
                return total;
            }
        }

        public override BoolExpr ToZ3(Context ctx, Form form) {
            if (form == Form.DNF) {
                if (ToggleVar == null) {
                    // This Or clause is in the outermost form, so we should ignore any toggle variable behaviour
                    return ctx.MkOr(Inner.Select(x => x.ToZ3(ctx, form)));
                } else {
                    // This Or clause is embedded within an And clause as part of an "Any"
                    return ctx.MkImplies(ToggleVar, ctx.MkOr(Inner.Select(x => x.ToZ3(ctx, form))));
                }
            } else if (form == Form.CNF) {
                if (ToggleVar == null) {
                    // This Or clause is embedded within an and clause, so we should compute an equivalent toggle variable
                    return ctx.MkOr(ctx.MkNot(ctx.MkOr(Inner.Select(x => x.ToggleVar))), ctx.MkOr(Inner.Select(x => x.ToZ3(ctx, form))));
                } else {
                    // This Or clause is embedded within an Or clause as part of an "Any"
                    return ctx.MkAnd(ToggleVar, ctx.MkOr(Inner.Select(x => x.ToZ3(ctx, form))));
                }
            }

            throw new NotImplementedException();
        }

        public override BooleanAst? Compile(Model z3Solution) {
            if (ToggleVar == null || ToggleSolution(z3Solution)) {
                List<BooleanAst> compiled = Inner.Select(x => x.Compile(z3Solution)).Where(x => x != null).ToList();
                if (compiled.Count > 0) {
                    return compiled.Aggregate((BooleanAst left, BooleanAst right) => new OrBool(left, right));
                } else {
                    return null;
                }
            } else {
                return null;
            }
        }
    }

    public class AndIr : Ir
    {
        public List<Ir> Inner { get; private set; }

        public AndIr(List<Ir> inner, BoolExpr toggleVar) : base(toggleVar) {
            Inner = inner;
        }

        public override Ir Apply(ImmutableDictionary<ObjectVariable, Tuple<BoundingBox, ObjectLiteral>> env, IOExample example) {
            return new AndIr(Inner.Select(node => node.Apply(env, example)).ToList(), ToggleVar);
        }

        public override ArithExpr? ToggleVarSum(Context ctx) {
            if (ToggleVar != null) {
                ArithExpr total = (ArithExpr)ctx.MkITE(ToggleVar, ctx.MkInt(1), ctx.MkInt(0));
                foreach (Ir r in Inner) {
                    total = total + r.ToggleVarSum(ctx);
                }
                return total;
            } else {
                ArithExpr? total = null;
                foreach (Ir r in Inner) {
                    ArithExpr? res = r.ToggleVarSum(ctx);
                    if (total == null) {
                        total = res;
                    } else if (res != null) {
                        total = total + r.ToggleVarSum(ctx);
                    }
                }
                return total;
            }
        }

        public override BoolExpr ToZ3(Context ctx, Form form) {
            if (form == Form.DNF) {
                if (ToggleVar == null) {
                    // This And clause is embedded within an Or clause, so we should compute the equivalent toggle variable
                    return ctx.MkAnd(ctx.MkOr(Inner.Select(x => x.ToggleVar)), ctx.MkAnd(Inner.Select(x => x.ToZ3(ctx, form))));
                } else {
                    // This And clause is embedded within an And clause as part of an "All"
                    return ctx.MkImplies(ToggleVar, ctx.MkAnd(Inner.Select(x => x.ToZ3(ctx, form))));
                }
            } else if (form == Form.CNF) {
                if (ToggleVar == null) {
                    // This And clause in the outermost form, and we should ignore any toggles
                    return ctx.MkAnd(Inner.Select(x => x.ToZ3(ctx, form)));
                } else {
                    // This And clause is embedded within an Or clause as part of an "All"
                    return ctx.MkAnd(ToggleVar, ctx.MkAnd(Inner.Select(x => x.ToZ3(ctx, form))));
                }
            }

            throw new NotImplementedException();
        }

        public override BooleanAst? Compile(Model z3Solution) {
            if (ToggleVar == null || ToggleSolution(z3Solution)) {
                List<BooleanAst> compiled = Inner.Select(x => x.Compile(z3Solution)).Where(x => x != null).ToList();
                if (compiled.Count > 0) {
                    return compiled.Aggregate((BooleanAst left, BooleanAst right) => new AndBool(left, right));
                } else {
                    return null;
                }
            } else {
                return null;
            }
        }
    }

    public class AnyIr : Ir
    {
        public readonly ObjectVariable ObjVar;
        public readonly Ir Inner;

        public AnyIr(ObjectVariable objVar, Ir inner, BoolExpr toggleVar) : base(toggleVar) {
            ObjVar = objVar;
            Inner = inner;
        }

        public override Ir Apply(ImmutableDictionary<ObjectVariable, Tuple<BoundingBox, ObjectLiteral>> env, IOExample example) {
            return new OrIr(example.GetBoxes().Select(box => Inner.Apply(env.Add(ObjVar, Tuple.Create(box, example.GetBase(box))), example)).ToList(), ToggleVar);
        }

        public override ArithExpr? ToggleVarSum(Context ctx) {
            if (ToggleVar != null) {
                return (ArithExpr)ctx.MkITE(ToggleVar, ctx.MkInt(1), ctx.MkInt(0)) + Inner.ToggleVarSum(ctx);
            } else {
                return Inner.ToggleVarSum(ctx);
            }
        }

        public override BoolExpr ToZ3(Context ctx, Form form) {
            throw new NotImplementedException("Unable to convert partially compiled formula to z3 form. This formula contains an any expression. Try compiling to completely reify all any statements");
        }

        public override BooleanAst? Compile(Model z3Solution) {
            if (ToggleVar == null || ToggleSolution(z3Solution)) {
                return new Any(new PredicateLambda(ObjVar, Inner.Compile(z3Solution)));
            } else {
                return null;
            }
        }
    }

    public class AllIr : Ir
    {
        public readonly ObjectVariable ObjVar;
        public readonly Ir Inner;

        public AllIr(ObjectVariable objVar, Ir inner, BoolExpr toggleVar) : base(toggleVar) {
            ObjVar = objVar;
            Inner = inner;
        }

        public override Ir Apply(ImmutableDictionary<ObjectVariable, Tuple<BoundingBox, ObjectLiteral>> env, IOExample example) {
            return new AndIr(example.GetBoxes().Select(box => Inner.Apply(env.Add(ObjVar, Tuple.Create(box, example.GetBase(box))), example)).ToList(), ToggleVar);
        }

        public override ArithExpr? ToggleVarSum(Context ctx) {
            if (ToggleVar != null) {
                return (ArithExpr)ctx.MkITE(ToggleVar, ctx.MkInt(1), ctx.MkInt(0)) + Inner.ToggleVarSum(ctx);
            } else {
                return Inner.ToggleVarSum(ctx);
            }
        }

        public override BoolExpr ToZ3(Context ctx, Form form) {
            throw new NotImplementedException("Unable to convert partially compiled formula to z3 form. This formula contains an all expression. Try compiling to completely reify all all statements");
        }

        public override BooleanAst? Compile(Model z3Solution) {
            if (ToggleVar == null || ToggleSolution(z3Solution)) {
                return new All(new PredicateLambda(ObjVar, Inner.Compile(z3Solution)));
            } else {
                return null;
            }
        }
    }

    public class BooleanIr : Ir
    {
        public readonly bool Value;

        public BooleanIr(bool value, BoolExpr toggleVar) : base(toggleVar) {
            Value = value;
        }

        public override Ir Apply(ImmutableDictionary<ObjectVariable, Tuple<BoundingBox, ObjectLiteral>> env, IOExample example) {
            return this;
        }

        public override ArithExpr? ToggleVarSum(Context ctx) {
            if (ToggleVar != null) {
                return (ArithExpr) ctx.MkITE(ToggleVar, ctx.MkInt(1), ctx.MkInt(0));
            } else {
                return null;
            }
        }

        public override BoolExpr ToZ3(Context ctx, Form form) {
            if (form == Form.DNF) {
                return ctx.MkImplies(ToggleVar, ctx.MkBool(Value));
            } else {
                return ctx.MkAnd(ToggleVar, ctx.MkBool(Value));
            }
        }

        public override BooleanAst? Compile(Model z3Solution) {
            throw new NotImplementedException("Cannot compile a boolean to an AST representation");
        }
    }

    public class MatchIr : Ir
    {
        public readonly ObjectAst ObjA;
        public readonly ObjectAst ObjB;
        public readonly bool Negated;

        public MatchIr(ObjectAst objA, ObjectAst objB, bool negated, BoolExpr toggleVar) : base(toggleVar) {
            ObjA = objA;
            ObjB = objB;
            Negated = negated;
        }

        public override Ir Apply(ImmutableDictionary<ObjectVariable, Tuple<BoundingBox, ObjectLiteral>> env, IOExample example) {
            ObjectAst a = ObjA;
            ObjectAst b = ObjB;

            if ((ObjA is ObjectVariable) && env.ContainsKey((ObjectVariable) ObjA)) {
                a = env[(ObjectVariable)ObjA].Item2;
            }
            if ((ObjB is ObjectVariable) && env.ContainsKey((ObjectVariable) ObjB)) {
                b = env[(ObjectVariable)ObjB].Item2;
            }

            if ((a is ObjectLiteral) && (b is ObjectLiteral)) {
                bool eq = ((ObjectLiteral)a).Equals((ObjectLiteral)b);
                if (Negated) {
                    return new BooleanIr(!eq, ToggleVar);
                } else {
                    return new BooleanIr(eq, ToggleVar);
                }
            } else {
                return new MatchIr(a, b, Negated, ToggleVar);
            }
        }

        public override ArithExpr? ToggleVarSum(Context ctx) {
            if (ToggleVar != null) {
                return (ArithExpr)ctx.MkITE(ToggleVar, ctx.MkInt(1), ctx.MkInt(0));
            } else {
                return null;
            }
        }

        public override BoolExpr ToZ3(Context ctx, Form form) {
            throw new NotImplementedException("Unable to convert partially compiled formula to z3 form. This formula contains a match expression. Try compiling to completely reify all match statements");
        }

        public override BooleanAst? Compile(Model z3Solution) {
            if (ToggleVar == null || ToggleSolution(z3Solution)) {
                Match m = new Match(ObjA, ObjB);
                if (Negated) {
                    return new NotBool(m);
                } else {
                    return m;
                }
            } else {
                return null;
            }
        }
    }

    public class GeqIr : Ir
    {
        public readonly double Val;
        public readonly RealExpr ComparisonVar;

        public GeqIr(double val, RealExpr comparisonVar, BoolExpr toggleVar) : base(toggleVar) {
            Val = val;
            ComparisonVar = comparisonVar;
        }

        public override Ir Apply(ImmutableDictionary<ObjectVariable, Tuple<BoundingBox, ObjectLiteral>> env, IOExample example) {
            return this;
        }

        public override ArithExpr? ToggleVarSum(Context ctx) {
            if (ToggleVar != null) {
                return (ArithExpr)ctx.MkITE(ToggleVar, ctx.MkInt(1), ctx.MkInt(0));
            } else {
                return null;
            }
        }

        public override BoolExpr ToZ3(Context ctx, Form form) {
            if (form == Form.DNF) {
                return ctx.MkImplies(ToggleVar, ctx.MkReal(Val.ToString()) >= ComparisonVar);
            } else if (form == Form.CNF) {
                return ctx.MkAnd(ToggleVar, ctx.MkReal(Val.ToString()) >= ComparisonVar);
            }

            throw new NotImplementedException();
        }

        public override BooleanAst? Compile(Model z3Solution) {
            throw new NotImplementedException("Cannot compile a boolean to an AST representation");
        }
    }

    public class IOUIr : Ir
    {
        public readonly ObjectVariable ObjA;
        public readonly ObjectVariable ObjB;
        public RealExpr ComparisonVar;

        public IOUIr(ObjectVariable objA, ObjectVariable objB, RealExpr comparisonVar, BoolExpr toggleVar) : base(toggleVar) {
            ObjA = objA;
            ObjB = objB;
            ComparisonVar = comparisonVar;
        }

        public override Ir Apply(ImmutableDictionary<ObjectVariable, Tuple<BoundingBox, ObjectLiteral>> env, IOExample example) {
            if (env.ContainsKey(ObjA) && env.ContainsKey(ObjB)) {
                BoundingBox boxA = env[ObjA].Item1;
                BoundingBox boxB = env[ObjB].Item1;
                return new GeqIr(boxA.JaccardIndex(boxB), ComparisonVar, ToggleVar);
            } else {
                throw new KeyNotFoundException("Unable to find keys in environment when applying an IOU node");
            }
        }

        public override ArithExpr? ToggleVarSum(Context ctx) {
            if (ToggleVar != null) {
                return (ArithExpr) ctx.MkITE(ToggleVar, ctx.MkInt(1), ctx.MkInt(0));
            } else {
                return null;
            }
        }

        public override BoolExpr ToZ3(Context ctx, Form form) {
            throw new NotImplementedException("Unable to convert partially compiled formula to z3 form. This formula contains a IOU expression. Try compiling to completely reify all IOU statements");
        }

        public override BooleanAst? Compile(Model z3Solution) {
            if (ToggleVar == null || ToggleSolution(z3Solution)) {
                return new IOU(ObjA, ObjB, ((RatNum)z3Solution.Eval(ComparisonVar)).Double);
            } else {
                return null;
            }
        }
    }
}
