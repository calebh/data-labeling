using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLabeling
{
    public class IOExample
    {
        public ImageResource Resource { get; private set; }
        private Dictionary<BoundingBox, List<ObjectLiteral>> BoundingBoxes = new Dictionary<BoundingBox, List<ObjectLiteral>>();

        public IOExample(ImageResource resource) {
            Resource = resource;
        }

        public void AddBox(BoundingBox box, ObjectLiteral label) {
            BoundingBoxes[box] = new List<ObjectLiteral>() { label };
        }

        public void MakePrecise(BoundingBox box, ObjectLiteral label) {
            BoundingBoxes[box].Add(label);
        }

        public List<ObjectLiteral> GetPrecise(BoundingBox box) {
            List<ObjectLiteral> allLabels = BoundingBoxes[box];
            return BoundingBoxes[box].GetRange(1, allLabels.Count - 1);
        }

        public void ClearPrecise(BoundingBox box) {
            List<ObjectLiteral> allLabels = BoundingBoxes[box];
            if (allLabels.Count > 1) {
                allLabels.RemoveRange(1, allLabels.Count - 1);
            }
        }

        public ObjectLiteral GetBase(BoundingBox box) {
            return BoundingBoxes[box][0];
        }

        public List<BoundingBox> GetBoxes() {
            return BoundingBoxes.Keys.ToList();
        }

        public List<BoundingBox> GetBoxes(ObjectLiteral label) {
            List<BoundingBox> boxes = new List<BoundingBox>();
            foreach (BoundingBox box in BoundingBoxes.Keys) {
                if (BoundingBoxes[box].Contains(label)) {
                    boxes.Add(box);
                }
            }

            return boxes;
        }

        public void RemoveBoxes(ObjectLiteral label) {
            foreach (BoundingBox box in GetBoxes(label)) {
                BoundingBoxes.Remove(box);
            }
        }
    }
}
