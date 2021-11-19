from dsl import *
import detect
import synthesize

def eval_bool(io_example, bool, obj, env):
    if isinstance(bool, TrueBool):
        return True
    elif isinstance(bool, FalseBool):
        return False
    elif isinstance(bool, NotBool):
        return not eval_bool(io_example, bool.inner, obj, env)
    elif isinstance(bool, AndBool):
        return eval_bool(io_example, bool.left, obj, env) and eval_bool(io_example, bool.right, obj, env)
    elif isinstance(bool, OrBool):
        return eval_bool(io_example, bool.left, obj, env) or eval_bool(io_example, bool.right, obj, env)
    elif isinstance(bool, Any):
        return len(eval_pred(io_example, bool.predicate, eval_obj_list(io_example, AllObjects()), env)) > 0
    elif isinstance(bool, All):
        return len(eval_pred(io_example, bool.predicate, eval_obj_list(io_example, AllObjects()), env)) == len(eval_obj_list(io_example, AllObjects()))
    elif isinstance(bool, Match):
        if isinstance(bool.object_a, ObjectVariable):
            obj_a = io_example.get_base(env[bool.object_a])
        elif isinstance(bool.object_a, ObjectLiteral):
            obj_a = bool.object_a
        if isinstance(bool.object_b, ObjectVariable):
            obj_b = io_example.get_base(env[bool.object_b])
        elif isinstance(bool.object_b, ObjectLiteral):
            obj_b = bool.object_b
        return obj_a == obj_b

def eval_pred(io_example, pred, obj_list, env=None):
    ret = []
    for obj in obj_list:
        if env is None:
            env = {pred.object_var: obj}
        else:
            env |= {pred.object_var: obj}
        if eval_bool(io_example, pred.boolean, obj, env):
            ret.append(obj)
    return ret

def eval_obj_list(io_example, object_list):
    if isinstance(object_list, AllObjects):
        return list(io_example.bounding_boxes.keys())
    elif isinstance(object_list, Filter):
        pred = object_list.predicate
        obj_list = eval_obj_list(io_example, object_list.object_list)
        return eval_pred(io_example, pred, obj_list)

def evaluate(io_example, program):
    for match in program.apply_list:
        label = match.action.label_name
        obj_list = eval_obj_list(io_example, match.object_list)
        for obj in obj_list:
            io_example.make_precise(obj, ObjectLiteral(label))

def main():
    ex1 = detect.bounding_boxes(ImageResource("images/guitarist1.jpg"))

    program = Program([
        MapApply(
            ApplyLabel("Guitarist"),
            Filter(
                Predicate(
                    ObjectVariable("x"),
                    AndBool(Match(ObjectVariable("x"), ObjectLiteral("Person")),
                            Any(Predicate(ObjectVariable("y"), Match(ObjectVariable("y"), ObjectLiteral("Guitar"))))
                            )
                ),
                AllObjects()
            )
        )
    ])

    evaluate(ex1, program)
    print(ex1)

if __name__ == "__main__":
    main()