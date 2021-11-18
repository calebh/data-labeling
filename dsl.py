class Ast:
    def __str__(self):
        return type(self).__name__

    def __eq__(self, other):
        return isinstance(other, Ast)

class Program(Ast):
    def __init__(self, apply_list):
        self.apply_list = apply_list

    def __str__(self):
        return type(self).__name__ + "(" + str(self.apply_list) + ")"

class MapApply(Ast):
    def __init__(self, action, object_list):
        self.action = action
        self.object_list = object_list

class ObjectList(Ast):
    pass

class AllObjects(ObjectList):
    pass

class Filter(ObjectList):
    def __init__(self, predicate, object_list):
        self.predicate = predicate
        self.object_list = object_list

class Action(Ast):
    pass

class ApplyLabel(Action):
    def __init__(self, label_name):
        self.label_name = label_name

    def __eq__(self, other):
        return isinstance(other, ApplyLabel) and self.label_name == other.label_name

    def __hash__(self):
        return hash(self.label_name)

    def __str__(self):
        return type(self).__name__ + "(" + self.label_name + ")"

class Object(Ast):
    pass

class ObjectLiteral(Object):
    def __init__(self, label_name):
        self.label_name = label_name

    def __eq__(self, other):
        return isinstance(other, ObjectLiteral) and self.label_name == other.label_name

    def __hash__(self):
        return hash(self.label_name)

class ObjectVariable(Object):
    def __init__(self, variable_name):
        self.variable_name = variable_name

    def __eq__(self, other):
        return isinstance(other, ObjectVariable) and self.variable_name == other.variable_name

    def __hash__(self):
        return hash(self.variable_name)

class Predicate(Ast):
    def __init__(self, object_var, boolean):
        self.object_var = object_var
        self.boolean = boolean

class Boolean(Ast):
    pass

class TruePred(Boolean):
    def __str__(self):
        return type(self).__name__ + "()"

class FalsePred(Boolean):
    def __str__(self):
        return type(self).__name__ + "()"

class Match(Boolean):
    def __init__(self, object_a, object_b):
        self.object_a = object_a
        self.object_b = object_b

    def __str__(self):
        return type(self).__name__ + "(" + str(self.object_a) + "," + str(self.object_b) + ")"

class NotPred(Boolean):
    def __init__(self, inner):
        self.inner = inner

    def __str__(self):
        return type(self).__name__ + "(" + str(self.inner) + ")"

class OrPred(Boolean):
    def __init__(self, left, right):
        self.left = left
        self.right = right

    def __str__(self):
        return type(self).__name__ + "(" + str(self.left) + "," + str(self.right) + ")"

class AndPred(Boolean):
    def __init__(self, left, right):
        self.left = left
        self.right = right

    def __str__(self):
        return type(self).__name__ + "(" + str(self.left) + "," + str(self.right) + ")"

class Any(Boolean):
    def __init__(self, predicate):
        self.predicate = predicate

    def __str__(self):
        return type(self).__name__ + "(" + str(self.predicate) + "," + ")"

class All(Boolean):
    def __init__(self, predicate):
        self.predicate = predicate

    def __str__(self):
        return type(self).__name__ + "(" + str(self.predicate) + "," + ")"

class BoundingBox:
    def __init__(self, left, top, width, height):
        self.left = left
        self.top = top
        self.width = width
        self.height = height

    def __str__(self):
        return type(self).__name__ + "(" + str(self.left) + "," + str(self.top) + "," + str(self.width) + "," + str(self.height) + ")"

    def __eq__(self, other):
        if isinstance(other, BoundingBox):
            return self.left == other.left and self.top == other.top and self.width == other.width and self.height == other.height
        return False

    def to_absolute(self, image_width, image_height):
        return BoundingBox(self.left * image_width, self.top * image_height, self.width * image_width, self.height * image_height)

    def __hash__(self):
        return hash((self.left, self.top, self.width, self.height))

class ImageResource:
    def __init__(self, path):
        self.path = path

    def read(self):
        with open(self.path, 'rb') as image:
            return image.read()

class IOExample:
    def __init__(self, resource):
        self.resource = resource
        self.bounding_boxes = {}

    def add_box(self, box, label):
        self.bounding_boxes[box] = [label]
        return box

    def make_precise(self, box, label):
        self.bounding_boxes[box].append(label)

    def add_union(self, boxes, label):
        ret = frozenset(boxes)
        self.bounding_boxes[ret] = [label]
        return ret

    def get_precise(self, box):
        return self.bounding_boxes[box][1:]

    def get_base(self, box):
        return self.bounding_boxes[box][0]

    def get_boxes(self, label):
        ret = []
        for (box, labels) in self.bounding_boxes.items():
            if label in labels:
                ret.append(box)
        return ret

    def __getitem__(self, box):
        return self.bounding_boxes[box]

    def __iter__(self):
        return iter(self.bounding_boxes)
