from enum import Enum
from functools import reduce

import face_recognition
from z3 import *
from dsl import *
import eval


def generate_face_library(io_examples):
    library = []
    for example in io_examples:
        for face in example.get_descriptors():
            seen_previously = False
            for prev_face in library:
                # Is this a new face?
                if face_recognition.compare_faces([prev_face], face, .55)[0]:
                    # This is not a new face, skip it and move on
                    seen_previously = True
                    break
            if not seen_previously:
                # This is indeed a new face, add it to the library
                library.append(face)
    return library

class Form(Enum):
    DNF = 0
    CNF = 1

def synthesize(io_examples):
    library = generate_face_library(io_examples)
    # Map the I/O examples so that the descriptors correspond to elements in the
    # face library instead of face vectors
    io_examples_library = []
    for example in io_examples:
        mapped_example = IOExample(example.resource)

        for face in example.get_descriptors():
            for (i, library_face) in enumerate(library):
                if face_recognition.compare_faces([library_face], face, .55)[0]:
                    mapped_example.add_descriptor(i)
                    if example.has_action(face):
                        mapped_example.apply_action(i, example[face])
        io_examples_library.append(mapped_example)

    atomic_predicates = []
    for (i, _) in enumerate(library):
        m = Match(i)
        atomic_predicates.append(m)
        atomic_predicates.append(NotPred(m))

    smallest_objective = float('inf')
    best_program = None

    for num_clauses in [10]:
        s_dnf = Optimize()
        s_cnf = Optimize()

        objective_dnf = 0
        objective_cnf = 0
        clauses_dnf = []
        clauses_cnf = []
        for clause_i in range(num_clauses):
            clause_dnf = []
            clause_cnf = []
            for pred_i in range(len(atomic_predicates)):
                name = "v_" + str(pred_i) + "_" + str(clause_i)
                v_dnf = Bool(name)
                v_cnf = Bool(name)
                objective_dnf += If(v_dnf, 1, 0)
                objective_cnf += If(v_cnf, 1, 0)
                clause_dnf.append(v_dnf)
                clause_cnf.append(v_cnf)
            clauses_dnf.append(clause_dnf)
            clauses_cnf.append(clause_cnf)

        minimization_dnf = s_dnf.minimize(objective_dnf)
        minimization_cnf = s_cnf.minimize(objective_cnf)

        # For each example
        for example in io_examples_library:
            for face_a in example.get_descriptors():
                atomic_pred_evaluations = []
                for (face_b, _) in enumerate(library):
                    if face_b == face_a:
                        # Evaluate match
                        atomic_pred_evaluations.append(True)
                        # Evaluate not match
                        atomic_pred_evaluations.append(False)
                    else:
                        # Evaluate match
                        atomic_pred_evaluations.append(False)
                        # Evaluate not match
                        atomic_pred_evaluations.append(True)
                action_applied = example.has_action(face_a)
                concrete_clauses_dnf = Or(
                    [And(
                        Or(clause),
                        And([Implies(variable, evaluation)
                             for (variable, evaluation) in zip(clause, atomic_pred_evaluations)
                             if evaluation is not None]))
                        for clause in clauses_dnf])
                concrete_clauses_cnf = And(
                    [Or(
                        Not(Or(clause)),
                        Or([And(variable, evaluation)
                            for (variable, evaluation) in zip(clause, atomic_pred_evaluations)]))
                        for clause in clauses_cnf])
                s_dnf.add(concrete_clauses_dnf == action_applied)
                s_cnf.add(concrete_clauses_cnf == action_applied)

        for (form, s, clauses, minimization) in [(Form.DNF, s_dnf, clauses_dnf, minimization_dnf),
                                                 (Form.CNF, s_cnf, clauses_cnf, minimization_cnf)]:
            if s.check() != unsat:
                m = s.model()
                objective_value = s.lower(minimization).as_long()
                if objective_value < smallest_objective:
                    smallest_objective = objective_value
                    predicate_clauses = []
                    for clause in clauses:
                        pred_clause = []
                        for (atomic_pred, variable) in zip(atomic_predicates, clause):
                            if bool(m[variable]):
                                pred_clause.append(atomic_pred)
                        if len(pred_clause) > 0:
                            predicate_clauses.append(pred_clause)
                    if len(predicate_clauses) > 0:
                        if form == Form.DNF:
                            synthesized_pred = reduce(OrPred, [reduce(AndPred, clause) for clause in predicate_clauses])
                        elif form == Form.CNF:
                            synthesized_pred = reduce(AndPred, [reduce(OrPred, clause) for clause in predicate_clauses])
                    else:
                        synthesized_pred = TruePred()
                    print("Discovered a new best program with objective value of {}".format(objective_value))
                    best_program = Program(Blur(), synthesized_pred, library)
                    print(best_program)
    return best_program
