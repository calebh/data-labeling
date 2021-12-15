using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLabeling
{
    public abstract class Ast
    {

    }

    public class Program : Ast
    {
        public List<MapApply> ApplyList { get; private set; }

        public Program(List<MapApply> applyList) {
            ApplyList = applyList;
        } 
    }

    public class MapApply : Ast
    {
        public ObjectLiteral Action { get; private set; }
        public ObjectList ObjectList { get; private set; }

        public MapApply(ObjectLiteral action, ObjectList objectList) {
            Action = action;
            ObjectList = objectList;
        }
    }

    public abstract class ObjectList : Ast
    {

    }

    public class AllObjects : ObjectList
    {
        public AllObjects() { }
    }

    public class Filter : ObjectList
    {
        public PredicateLambda Predicate { get; private set; }
        public ObjectList ObjectList { get; private set; }

        public Filter(PredicateLambda predicate, ObjectList objectList) {
            Predicate = predicate;
            ObjectList = objectList;
        }
    }

    public abstract class ObjectAst : Ast
    {

    }

    public class ObjectLiteral : ObjectAst
    {
        public string LabelName { get; private set; }

        public ObjectLiteral(string labelName) {
            LabelName = labelName;
        }

        public override bool Equals(object? obj) {
            ObjectLiteral other = obj as ObjectLiteral;
            if (other == null) {
                return false;
            } else {
                return LabelName.Equals(other.LabelName);
            }
        }

        public override int GetHashCode() {
            return LabelName.GetHashCode();
        }
    }

    public class ObjectVariable : ObjectAst
    {
        public string VariableName { get; private set; }

        public ObjectVariable(string variableName) {
            VariableName = variableName;
        }

        public override int GetHashCode() {
            return VariableName.GetHashCode();
        }

        public override bool Equals(object? obj) {
            return Equals(obj as ObjectVariable);
        }

        public bool Equals(ObjectVariable other) {
            return other != null && VariableName == other.VariableName;
        }
    }

    public class PredicateLambda : Ast
    {
        public ObjectVariable VarName { get; private set; }
        public BooleanAst Body { get; private set; }

        public PredicateLambda(ObjectVariable varName, BooleanAst body) {
            VarName = varName;
            Body = body;
        }
    }

    public abstract class BooleanAst : Ast {
    
    }

    public class TrueBool : BooleanAst
    {
        public TrueBool() { }
    }

    public class FalseBool : BooleanAst
    {
        public FalseBool() { }
    }

    public class Match : BooleanAst
    {
        public ObjectAst ObjectA { get; private set; }
        public ObjectAst ObjectB { get; private set; }

        public Match(ObjectAst objectA, ObjectAst objectB) {
            ObjectA = objectA;
            ObjectB = objectB;
        }
    }

    public class NotBool : BooleanAst
    {
        public BooleanAst Inner { get; private set; }

        public NotBool(BooleanAst inner) {
            Inner = inner;
        }
    }

    public class OrBool : BooleanAst
    {
        public BooleanAst Left { get; private set; }
        public BooleanAst Right { get; private set; }

        public OrBool(BooleanAst left, BooleanAst right) {
            Left = left;
            Right = right;
        }
    }

    public class AndBool : BooleanAst
    {
        public BooleanAst Left { get; private set; }
        public BooleanAst Right { get; private set; }

        public AndBool(BooleanAst left, BooleanAst right) {
            Left = left;
            Right = right;
        }
    }

    public class Any : BooleanAst
    {
        public PredicateLambda Predicate { get; private set; }

        public Any(PredicateLambda predicate) {
            Predicate = predicate;
        }
    }

    public class All : BooleanAst
    {
        public PredicateLambda Predicate { get; private set; }

        public All(PredicateLambda predicate) {
            Predicate = predicate;
        }
    }

    public class IOU : BooleanAst
    {
        public ObjectAst ObjectA { get; private set; }
        public ObjectAst ObjectB { get; private set; }
        public double Threshold { get; private set; }

        public IOU(ObjectAst objectA, ObjectAst objectB, double threshold) {
            ObjectA = objectA;
            ObjectB = objectB;
            Threshold = threshold;
        }
    }
}
