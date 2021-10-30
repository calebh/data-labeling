import numpy

class Ast:
    def __str__(self):
        return type(self).__name__

    def __eq__(self, other):
        return isinstance(other, Ast)

class Program(Ast):
    def __init__(self, action, predicate, face_library):
        self.action = action
        self.predicate = predicate
        self.face_library = face_library

    def __str__(self):
        return type(self).__name__ + "(" + str(self.action) + ", " + str(self.predicate) + ")"

class Action(Ast):
    pass

class Blur(Action):
    pass

class Blackout(Action):
    pass

class Predicate(Ast):
    pass

class TruePred(Predicate):
    def __str__(self):
        return type(self).__name__ + "()"

class FalsePred(Predicate):
    def __str__(self):
        return type(self).__name__ + "()"

class Match(Predicate):
    def __init__(self, face_index):
        self.face_index = face_index

    def __str__(self):
        return type(self).__name__ + "(" + str(self.face_index) + ")"

    def __eq__(self, other):
        if isinstance(other, Match):
            return self.face_index == other.face_index
        return False

class NotPred(Predicate):
    def __init__(self, inner):
        self.inner = inner

    def __str__(self):
        return type(self).__name__ + "(" + str(self.inner) + ")"

    def __eq__(self, other):
        if isinstance(other, NotPred):
            return self.inner == other.inner
        return False

class OrPred(Predicate):
    def __init__(self, left, right):
        self.left = left
        self.right = right

    def __str__(self):
        return type(self).__name__ + "(" + str(self.left) + "," + str(self.right) + ")"

    def __eq__(self, other):
        if isinstance(other, OrPred):
            return (self.left == other.left and self.right == other.right) or \
                    (self.left == other.right and self.right == other.left)
        return False

class AndPred(Predicate):
    def __init__(self, left, right):
        self.left = left
        self.right = right

    def __str__(self):
        return type(self).__name__ + "(" + str(self.left) + "," + str(self.right) + ")"

    def __eq__(self, other):
        if isinstance(other, AndPred):
            return (self.left == other.left and self.right == other.right) or \
                    (self.left == other.right and self.right == other.left)
        return False

# An IOExample can live separately from a program, including a face library
# It is an abstract container representing applying actions to certain descriptors
# found in the resource. An action is an AST node that indicates an action (right now
# this is Blur and Blackout). The resource parameter could be something like a file path
# or a string describing the example. A descriptor could be something like an embedding
# vector representing a face, or an object class from a detector, etc.
class IOExample:
    def __init__(self, resource=None, descriptors=None):
        self.resource = resource
        if descriptors is None:
            self.descriptors = []
        else:
            self.descriptors = descriptors
        self.mappings = {}

    def add_descriptor(self, descriptor):
        self.descriptors.append(descriptor)

    def apply_action(self, descriptor, action):
        if isinstance(descriptor, numpy.ndarray):
            self.mappings[descriptor.tobytes()] = action
        else:
            self.mappings[descriptor] = action

    def apply_action_by_index(self, i, action):
        descriptor = self.descriptors[i]
        if isinstance(descriptor, numpy.ndarray):
            self.mappings[descriptor.tobytes()] = action
        else:
            self.mappings[descriptor] = action

    def has_action(self, descriptor):
        if isinstance(descriptor, numpy.ndarray):
            return descriptor.tobytes() in self.mappings
        else:
            return descriptor in self.mappings

    def get_descriptors(self):
        return self.descriptors

    def __getitem__(self, descriptor):
        if isinstance(descriptor, numpy.ndarray):
            return self.mappings[descriptor.tobytes()]
        else:
            return self.mappings[descriptor]
