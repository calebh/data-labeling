from dsl import *
import boto3

client = boto3.client('rekognition')

def bounding_boxes(image_resource):
    response = client.detect_labels(Image={'Bytes': image_resource.read()})

    ret = IOExample(image_resource)

    for label in response['Labels']:
        name = label['Name']
        for instance in label['Instances']:
            bounding_box = BoundingBox(instance['BoundingBox']['Left'], instance['BoundingBox']['Top'], instance['BoundingBox']['Width'], instance['BoundingBox']['Height'])
            ret.add_box(bounding_box, ObjectLiteral(name))

    return ret