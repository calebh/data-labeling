import boto3
import sys
import tkinter as tk
import tkinter.simpledialog
from PIL import Image, ImageTk, ImageDraw
import json

client = boto3.client('rekognition')

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

    def to_dict(self):
        return {
            "Left": self.left,
            "Top": self.top,
            "Width": self.width,
            "Height": self.height
        }

def bounding_boxes(image_path):
    with open(image_path, 'rb') as image:
        response = client.detect_labels(Image={'Bytes': image.read()})

        ret = []

        for label in response['Labels']:
            name = label['Name']
            for instance in label['Instances']:
                bounding_box = BoundingBox(instance['BoundingBox']['Left'], instance['BoundingBox']['Top'], instance['BoundingBox']['Width'], instance['BoundingBox']['Height'])
                ret.append((bounding_box, name))

        return ret

def process_image(image_path):
    root = tk.Tk()

    # load image
    image0 = Image.open(image_path)

    width, height = image0.size

    # Draw rectangle on image
    image1 = ImageDraw.Draw(image0)

    boxes = bounding_boxes(image_path)
    for (box, name) in boxes:
        abs_box = box.to_absolute(width, height)
        image1.rectangle([(abs_box.left, abs_box.top), (abs_box.left + abs_box.width, abs_box.top + abs_box.height)], fill=None, outline ="red")
    
    photo = ImageTk.PhotoImage(image1._image)

    # label with image
    l = tk.Label(root, image=photo)
    l.pack()

    precise_labels = [None] * len(boxes)

    def on_click(event=None):
        x = event.x
        y = event.y
        for (i, (box, name)) in enumerate(boxes):
            abs_box = box.to_absolute(width, height)
            if abs_box.left <= x <= abs_box.left + abs_box.width and abs_box.top <= y <= abs_box.top + abs_box.height:
                precise_labels[i] = tk.simpledialog.askstring("Input", "What is the more precise label for this box?", parent=root, initialvalue = name if precise_labels[i] is None else precise_labels[i])
                if precise_labels[i] == "" or precise_labels[i] == name:
                    precise_labels[i] = None
                break

    # bind click event to image
    l.bind('<Button-1>', on_click)

    # button with text closing window
    b = tk.Button(root, text="Close", command=root.destroy)
    b.pack()

    # "start the engine"
    root.mainloop()

    return (width, height, [(box, name, precise_label) for ((box, name), precise_label) in zip(boxes, precise_labels)])

accumulated_info = {}

image_paths = sys.argv[1:-1]
for img in image_paths:
    (width, height, box_info) = process_image(img)
    accumulated_info[img] = {"Boxes": [], "Width": width, "Height": height}
    for (box, name, precise_label) in box_info:
        accumulated_info[img]["Boxes"].append({"Box": box.to_dict(), "Name": name, "PreciseLabel": precise_label})

output_path = sys.argv[-1]
f = open(output_path, "w")
json.dump(accumulated_info, f)
f.close()