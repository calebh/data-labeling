from dsl import *
import detect
import synthesize

ex1 = detect.bounding_boxes(ImageResource("images/guitarist1.jpg"))
ex1.make_precise(ex1.get_boxes(ObjectLiteral("Person"))[0], ObjectLiteral("Guitarist"))

ex2 = detect.bounding_boxes(ImageResource("images/guitarist2.jpg"))
ex2.make_precise(ex2.get_boxes(ObjectLiteral("Person"))[0], ObjectLiteral("Guitarist"))

ex3 = detect.bounding_boxes(ImageResource("images/person1.jpg"))
ex4 = detect.bounding_boxes(ImageResource("images/person2.jpg"))

synthesize.synthesize([ex1, ex2, ex3, ex4])