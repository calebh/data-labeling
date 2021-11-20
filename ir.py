from enum import Enum

import z3
import dsl
from functools import reduce


class Form(Enum):
    DNF = 0
    CNF = 1

class Ir:
    pass

class OrIr(Ir):
    def __init__(self, inner, toggle_var=None):
        self.inner = inner
        self.toggle_var = toggle_var

    def apply(self, env, io_example):
        return OrIr([node.apply(env, io_example) for node in self.inner], self.toggle_var)

    def to_z3(self, form):
        if form == Form.DNF:
            if self.toggle_var is None:
                return z3.Or([x.to_z3(form) for x in self.inner])
            else:
                return z3.Implies(self.toggle_var, z3.Or([x.to_z3(form) for x in self.inner]))
        elif form == Form.CNF:
            if self.toggle_var is None:
                return z3.Or(z3.Not(z3.Or([x.toggle_var for x in self.inner])), z3.Or([x.to_z3(form) for x in self.inner]))
            else:
                return z3.And(self.toggle_var, z3.Or([x.to_z3(form) for x in self.inner]))

    def toggle_var_sum(self):
        if self.toggle_var is not None:
            total = z3.If(self.toggle_var, 1, 0)
        else:
            total = 0
        for x in self.inner:
            inner_total = x.toggle_var_sum()
            if inner_total is not 0:
                total += inner_total
        return total

class AndIr(Ir):
    def __init__(self, inner, toggle_var=None):
        self.inner = inner
        self.toggle_var = toggle_var

    def apply(self, env, io_example):
        return AndIr([node.apply(env, io_example) for node in self.inner], self.toggle_var)

    def to_z3(self, form):
        if form == Form.DNF:
            if self.toggle_var is None:
                return z3.And(z3.Or([x.toggle_var for x in self.inner]), z3.And([x.to_z3(form) for x in self.inner]))
            else:
                return z3.Implies(self.toggle_var, z3.And([x.to_z3(form) for x in self.inner]))
        elif form == Form.CNF:
            if self.toggle_var is None:
                return z3.And([x.to_z3(form) for x in self.inner])
            else:
                return z3.And(self.toggle_var, z3.And([x.to_z3(form) for x in self.inner]))

    def toggle_var_sum(self):
        if self.toggle_var is not None:
            total = z3.If(self.toggle_var, 1, 0)
        else:
            total = 0
        for x in self.inner:
            inner_total = x.toggle_var_sum()
            if inner_total is not 0:
                total += inner_total
        return total

class AnyIr(Ir):
    def __init__(self, object_var, inner, toggle_var):
        self.object_var = object_var
        self.inner = inner
        self.toggle_var = toggle_var

    def apply(self, env, io_example):
        return OrIr([self.inner.apply(env | {self.object_var: io_example.get_base(box)}, io_example) for box in io_example], self.toggle_var)

    def to_z3(self, form):
        raise Exception("Unable to convert partially compiled formula to z3 form. This formula contains an any expression. Try compiling to completely reify all any statements")

    def toggle_var_sum(self):
        if self.toggle_var is not None:
            return z3.If(self.toggle_var, 1, 0) + self.inner.toggle_var_sum()
        else:
            return self.inner.toggle_var_sum()

class AllIr(Ir):
    def __init__(self, object_var, inner, toggle_var):
        self.object_var = object_var
        self.inner = inner
        self.toggle_var = toggle_var

    def apply(self, env, io_example):
        return AndIr([self.inner.apply(env | {self.object_var: io_example.get_base(box)}, io_example) for box in io_example], self.toggle_var)

    def to_z3(self, form):
        raise Exception("Unable to convert partially compiled formula to z3 form. This formula contains an all expression. Try compiling to completely reify all all statements")

    def toggle_var_sum(self):
        if self.toggle_var is not None:
            return z3.If(self.toggle_var, 1, 0) + self.inner.toggle_var_sum()
        else:
            return self.inner.toggle_var_sum()

var_index = 0

def get_fresh_toggle_var():
    global var_index
    ret = z3.Bool("v" + str(var_index))
    var_index += 1
    return ret

class BooleanIr(Ir):
    def __init__(self, value, toggle_var):
        self.value = value
        self.toggle_var = toggle_var

    def apply(self, env, io_example):
        return self

    def to_z3(self, form):
        if form == Form.DNF:
            return z3.Implies(self.toggle_var, z3.Bool(self.value))
        elif form == Form.CNF:
            return z3.And(self.toggle_var, z3.Bool(self.value))

    def toggle_var_sum(self):
        if self.toggle_var is not None:
            return z3.If(self.toggle_var, 1, 0)
        else:
            return 0

class MatchIr(Ir):
    def __init__(self, object_a, object_b, negated, toggle_var):
        self.object_a = object_a
        self.object_b = object_b
        self.toggle_var = toggle_var
        self.negated = negated

    def apply(self, env, io_example):
        if self.object_a in env:
            a = env[self.object_a]
        else:
            a = self.object_a
        if self.object_b in env:
            b = env[self.object_b]
        else:
            b = self.object_b
        if isinstance(a, dsl.ObjectLiteral) and isinstance(b, dsl.ObjectLiteral):
            if self.negated:
                return BooleanIr(a != b, self.toggle_var)
            else:
                return BooleanIr(a == b, self.toggle_var)
        else:
            return MatchIr(a, b, self.negated, self.toggle_var)

    def to_z3(self, toggle_var_constructor):
        raise Exception("Unable to convert partially compiled formula to z3 form. This formula contains a match expression. Try compiling to completely reify all match statements")

    def toggle_var_sum(self):
        if self.toggle_var is not None:
            return z3.If(self.toggle_var, 1, 0)
        else:
            return 0

def compile(ir, z3_solution):
    if isinstance(ir, MatchIr):
        if ir.toggle_var is None or bool(z3_solution[ir.toggle_var]):
            m = dsl.Match(ir.object_a, ir.object_b)
            if ir.negated:
                return dsl.NotBool(m)
            else:
                return m
        else:
            return None
    elif isinstance(ir, AnyIr):
        if ir.toggle_var is None or bool(z3_solution[ir.toggle_var]):
            return dsl.Any(dsl.Predicate(ir.object_var, compile(ir.inner, z3_solution)))
        else:
            return None
    elif isinstance(ir, AllIr):
        if ir.toggle_var is None or bool(z3_solution[ir.toggle_var]):
            return dsl.All(dsl.Predicate(ir.object_var, compile(ir.inner, z3_solution)))
        else:
            return None
    elif isinstance(ir, OrIr):
        if ir.toggle_var is None or bool(z3_solution[ir.toggle_var]):
            compiled = [compile(x, z3_solution) for x in ir.inner]
            compiled = [x for x in compiled if x is not None]
            if len(compiled) > 0:
                return reduce(dsl.OrBool, compiled)
            else:
                return None
        else:
            return None
    elif isinstance(ir, AndIr):
        if ir.toggle_var is None or bool(z3_solution[ir.toggle_var]):
            compiled = [compile(x, z3_solution) for x in ir.inner]
            compiled = [x for x in compiled if x is not None]
            if len(compiled) > 0:
                return reduce(dsl.AndBool, compiled)
            else:
                return None
        else:
            return None
