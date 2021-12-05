from enum import Enum

import detect
import ir
from z3 import *
from dsl import *
import eval

class Form(Enum):
    DNF = 0
    CNF = 1

def generate_base_library(io_examples):
    library = set()
    for example in io_examples:
        for box in example:
            library.add(example.get_base(box))
    return list(library)

def generate_precise_library(io_examples):
    library = set()
    for example in io_examples:
        for box in example:
            library = library.union(example.get_precise(box))
    return list(library)

class SynthesisState(Enum):
    ONLY_CLAUSES = 0
    QUANTIFIERS = 1

def synthesize(io_examples):
    base_library = generate_base_library(io_examples)
    precise_library = generate_precise_library(io_examples)

    precise_worklist = list(precise_library)

    synthesized_maps = []

    while len(precise_worklist) > 0:
        precise_label = precise_worklist.pop()
        synthesis_succeeded = False
        num_clauses = 5
        quantifier_nested_level = 1
        state = SynthesisState.ONLY_CLAUSES

        while not synthesis_succeeded:
            if state == SynthesisState.ONLY_CLAUSES:
                clauses_dnf = []
                clauses_cnf = []
                for clause_i in range(num_clauses):
                    clause_dnf = ir.AndIr([match for label in base_library for match in [ir.MatchIr(ObjectVariable("x"), label, False, ir.get_fresh_toggle_var()), ir.MatchIr(ObjectVariable("x"), label, True, ir.get_fresh_toggle_var())]])
                    clause_cnf = ir.OrIr([match for label in base_library for match in [ir.MatchIr(ObjectVariable("x"), label, False, ir.get_fresh_toggle_var()), ir.MatchIr(ObjectVariable("x"), label, True, ir.get_fresh_toggle_var())]])
                    clauses_dnf.append(clause_dnf)
                    clauses_cnf.append(clause_cnf)

                dnf = ir.OrIr(clauses_dnf)
                cnf = ir.AndIr(clauses_cnf)

                s_dnf = Optimize()
                s_cnf = Optimize()

                for example in io_examples:
                    for box in example:
                        compiled_dnf = dnf.apply({ObjectVariable("x"): (box, example.get_base(box))}, example).to_z3(ir.Form.DNF)
                        compiled_cnf = cnf.apply({ObjectVariable("x"): (box, example.get_base(box))}, example).to_z3(ir.Form.CNF)
                        action_applied = precise_label in example.get_precise(box)
                        s_dnf.add(compiled_dnf == action_applied)
                        s_cnf.add(compiled_cnf == action_applied)

                smallest_objective = float('inf')

                minimization_dnf = s_dnf.minimize(dnf.toggle_var_sum())
                minimization_cnf = s_cnf.minimize(cnf.toggle_var_sum())

                best_program = None

                for (s, ir_nf, minimization) in [(s_dnf, dnf, minimization_dnf),
                                                 (s_cnf, cnf, minimization_cnf)]:
                    if s.check() != unsat:
                        m = s.model()
                        objective_value = s.lower(minimization).as_long()
                        if objective_value < smallest_objective:
                            smallest_objective = objective_value
                            synthesized_pred = ir.compile(ir_nf, m)
                            best_program = MapApply(precise_label, Filter(Predicate(ObjectVariable("x"), synthesized_pred), AllObjects()))

                if best_program is not None:
                    synthesized_maps.append(best_program)
                    synthesis_succeeded = True
                else:
                    # Synthesis failed in this iteration. In the next iteration try synthesis with anys and alls
                    state = SynthesisState.QUANTIFIERS
            elif state == SynthesisState.QUANTIFIERS:
                def rec_generate(level, accum_levels):
                    level_var = ObjectVariable("x" + str(level))
                    next_level_var = ObjectVariable("x" + str(level - 1))
                    next_accum_level = accum_levels + [next_level_var]

                    clauses_dnf = []
                    clauses_cnf = []
                    for clause_i in range(num_clauses):
                        clause_dnf = [match for label in base_library for match in [ir.MatchIr(level_var, label, False, ir.get_fresh_toggle_var()), ir.MatchIr(level_var, label, True, ir.get_fresh_toggle_var())]]
                        clause_cnf = [match for label in base_library for match in [ir.MatchIr(level_var, label, False, ir.get_fresh_toggle_var()), ir.MatchIr(level_var, label, True, ir.get_fresh_toggle_var())]]
                        for (i, level_a) in enumerate(accum_levels):
                            for level_b in accum_levels[i+1:]:
                                clause_dnf.append(ir.MatchIr(level_a, level_b, True, ir.get_fresh_toggle_var()))
                                clause_dnf.append(ir.MatchIr(level_a, level_b, False, ir.get_fresh_toggle_var()))
                                clause_cnf.append(ir.MatchIr(level_a, level_b, True, ir.get_fresh_toggle_var()))
                                clause_cnf.append(ir.MatchIr(level_a, level_b, False, ir.get_fresh_toggle_var()))
                                clause_dnf.append(ir.JaccardIndexIr(level_a, level_b, ir.get_fresh_real_var(), ir.get_fresh_toggle_var()))
                                clause_cnf.append(ir.JaccardIndexIr(level_a, level_b, ir.get_fresh_real_var(), ir.get_fresh_toggle_var()))
                        if level > 0:
                            (dnf_res, cnf_res) = rec_generate(level - 1, next_accum_level)
                            clause_dnf.append(ir.AnyIr(next_level_var, dnf_res, ir.get_fresh_toggle_var()))
                            clause_dnf.append(ir.AllIr(next_level_var, dnf_res, ir.get_fresh_toggle_var()))
                            clause_cnf.append(ir.AnyIr(next_level_var, cnf_res, ir.get_fresh_toggle_var()))
                            clause_cnf.append(ir.AllIr(next_level_var, cnf_res, ir.get_fresh_toggle_var()))
                        clauses_dnf.append(ir.AndIr(clause_dnf))
                        clauses_cnf.append(ir.OrIr(clause_cnf))

                    dnf = ir.OrIr(clauses_dnf)
                    cnf = ir.AndIr(clauses_cnf)
                    return (dnf, cnf)

                outermost_variable = ObjectVariable("x" + str(quantifier_nested_level))

                (dnf, cnf) = rec_generate(quantifier_nested_level, [outermost_variable])

                s_dnf = Optimize()
                s_cnf = Optimize()

                for example in io_examples:
                    for box in example:
                        compiled_dnf_0 = dnf.apply({outermost_variable: (box, example.get_base(box))}, example)
                        compiled_dnf = compiled_dnf_0.to_z3(ir.Form.DNF)
                        compiled_cnf_0 = cnf.apply({outermost_variable: (box, example.get_base(box))}, example)
                        compiled_cnf = compiled_cnf_0.to_z3(ir.Form.CNF)
                        action_applied = precise_label in example.get_precise(box)
                        s_dnf.add(compiled_dnf == action_applied)
                        s_cnf.add(compiled_cnf == action_applied)

                smallest_objective = float('inf')

                minimization_dnf = s_dnf.minimize(dnf.toggle_var_sum())
                minimization_cnf = s_cnf.minimize(cnf.toggle_var_sum())

                best_program = None

                for (s, ir_nf, minimization) in [(s_dnf, dnf, minimization_dnf),
                                                 (s_cnf, cnf, minimization_cnf)]:
                    if s.check() != unsat:
                        m = s.model()
                        objective_value = s.lower(minimization).as_long()
                        if objective_value < smallest_objective:
                            smallest_objective = objective_value
                            synthesized_pred = ir.compile(ir_nf, m)
                            best_program = MapApply(precise_label, Filter(Predicate(outermost_variable, synthesized_pred), AllObjects()))

                if best_program is not None:
                    synthesized_maps.append(best_program)
                    synthesis_succeeded = True
                else:
                    quantifier_nested_level += 1
                    num_clauses += 5
                    state = SynthesisState.ONLY_CLAUSES

    return Program(synthesized_maps)

def main():
    ex1 = detect.bounding_boxes(ImageResource("images/guitarist1.jpg"))
    #ex1.remove_boxes(ObjectLiteral("Microphone"))
    for person in ex1.get_boxes(ObjectLiteral("Person")):
        ex1.make_precise(person, ObjectLiteral("Guitarist"))

    ex2 = detect.bounding_boxes(ImageResource("images/guitarist2.jpg"))
    #ex2.remove_boxes(ObjectLiteral("Microphone"))
    for person in ex2.get_boxes(ObjectLiteral("Person")):
        ex2.make_precise(person, ObjectLiteral("Guitarist"))

    ex3 = detect.bounding_boxes(ImageResource("images/person1.jpg"))
    #ex3.remove_boxes(ObjectLiteral("Microphone"))
    ex4 = detect.bounding_boxes(ImageResource("images/person2.jpg"))
    #ex4.remove_boxes(ObjectLiteral("Microphone"))

    ex5 = detect.bounding_boxes(ImageResource("images/band1.jpg"))
    people5 = ex5.get_boxes(ObjectLiteral("Person"))
    people5.sort(key=lambda box: box.left)
    ex5.make_precise(people5[0], ObjectLiteral("Guitarist"))
    ex5.make_precise(people5[2], ObjectLiteral("Guitarist"))

    ex6 = detect.bounding_boxes(ImageResource("images/singer1.jpg"))

    ex7 = detect.bounding_boxes(ImageResource("images/street.jpg"))

    prog = synthesize([ex1, ex2, ex3, ex4, ex5])
    print(prog)

    ex1.clear_precise()

    eval.evaluate(ex1, prog)

    print(ex1)

    #prog = synthesize([ex1, ex3])
    #print(prog)

if __name__ == "__main__":
    main()