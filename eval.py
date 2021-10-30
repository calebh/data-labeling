import cv2
import face_recognition
import numpy as np
from dsl import *

def get_face_library(img_dir):
    img = cv2.imread(img_dir, 1)
    rgb = cv2.cvtColor(img, cv2.COLOR_BGR2RGB)
    face_locations = face_recognition.face_locations(rgb, model="hog")
    face_encodings = face_recognition.face_encodings(rgb, face_locations)
    return face_encodings

def eval_program(prog, img_dir):
    face_library = prog.face_library
    img = cv2.imread(img_dir, 1)
    rgb = cv2.cvtColor(img, cv2.COLOR_BGR2RGB)
    face_locations = face_recognition.face_locations(rgb, model="hog")
    face_encodings = face_recognition.face_encodings(rgb, face_locations)
    for (face_loc, face_enc) in zip(face_locations, face_encodings):
        face_index = None
        # Compare the detected faces to what is in the face library
        for (i, face_in_library) in enumerate(face_library):
            match = face_recognition.compare_faces([face_in_library], face_enc, .55)
            if match[0]:
                face_index = i
                break
        # If face_index is still None at the end of the loop, it means this face
        # could not be found in the face library
        def eval_predicate(pred):
            if isinstance(pred, Match):
                if face_index is not None and face_index == pred.face_index:
                    return True
                else:
                    return False
            elif isinstance(pred, NotPred):
                return not eval_predicate(pred.inner)
            elif isinstance(pred, OrPred):
                return eval_predicate(pred.left) or eval_predicate(pred.right)
            elif isinstance(pred, AndPred):
                return eval_predicate(pred.left) and eval_predicate(pred.right)
            elif isinstance(pred, TruePred):
                return True
            elif isinstance(pred, FalsePred):
                return False
            else:
                raise NotImplementedError("Unknown predicate: " + str(pred))
        if eval_predicate(prog.predicate):
            # This face passed the predicate, apply the action
            action = prog.action
            if isinstance(action, Blur):
                ROI = img[face_loc[0]:face_loc[2], face_loc[3]:face_loc[1]]
                blurred_face = cv2.GaussianBlur(ROI, (51,51), 0)
                # Insert ROI back into image
                img[face_loc[0]:face_loc[2], face_loc[3]:face_loc[1]] = blurred_face
            elif isinstance(action, Blackout):
                ROI = np.array([[face_loc[3], face_loc[1]],
                                [face_loc[3], face_loc[2]],
                                [face_loc[0], face_loc[2]],
                                [face_loc[0], face_loc[1]]])
                cv2.fillPoly(img, pts = [ROI], color =(0,0,0))
    # Displaying the image
    cv2.imshow('image', img)
    # Wait for a key to be pressed to exit
    cv2.waitKey(0)
    # Close the window
    cv2.destroyAllWindows()
