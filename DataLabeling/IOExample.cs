using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLabeling
{
    public struct BoundingBoxInfo
    {
        public readonly List<ObjectLiteral> Labels;
        public readonly List<GroupLiteral> Groups;

        public BoundingBoxInfo(List<ObjectLiteral> labels, List<GroupLiteral> groups) {
            Labels = labels;
            Groups = groups;
        }
    }

    public class IOExample
    {
        public ImageResource Resource { get; private set; }
        private Dictionary<BoundingBox, BoundingBoxInfo> BoundingBoxes = new Dictionary<BoundingBox, BoundingBoxInfo>();

        public IOExample(ImageResource resource) {
            Resource = resource;
        }

        public void AddBox(BoundingBox box, ObjectLiteral label) {
            BoundingBoxes[box] = new BoundingBoxInfo(new List<ObjectLiteral>() { label }, new List<GroupLiteral>());
        }

        public void MakePrecise(BoundingBox box, ObjectLiteral label) {
            BoundingBoxes[box].Labels.Add(label);
        }

        public void MakeGroup(BoundingBox box, GroupLiteral label) {
            BoundingBoxes[box].Groups.Add(label);
        }

        public List<ObjectLiteral> GetPrecise(BoundingBox box) {
            List<ObjectLiteral> allLabels = BoundingBoxes[box].Labels;
            return BoundingBoxes[box].Labels.GetRange(1, allLabels.Count - 1);
        }

        public List<GroupLiteral> GetGroups(BoundingBox box) {
            return BoundingBoxes[box].Groups;
        }

        public void ClearPrecise(BoundingBox box) {
            List<ObjectLiteral> allLabels = BoundingBoxes[box].Labels;
            if (allLabels.Count > 1) {
                allLabels.RemoveRange(1, allLabels.Count - 1);
            }
        }

        public List<BoundingBox> GetBoxes(GroupLiteral label) {
            List<BoundingBox> boxes = new List<BoundingBox>();
            foreach (BoundingBox box in BoundingBoxes.Keys) {
                if (BoundingBoxes[box].Groups.Contains(label)) {
                    boxes.Add(box);
                }
            }

            return boxes;
        }

        public ObjectLiteral GetBase(BoundingBox box) {
            return BoundingBoxes[box].Labels[0];
        }

        public List<BoundingBox> GetBoxes() {
            return BoundingBoxes.Keys.ToList();
        }

        public List<BoundingBox> GetBoxes(ObjectLiteral label) {
            List<BoundingBox> boxes = new List<BoundingBox>();
            foreach (BoundingBox box in BoundingBoxes.Keys) {
                if (BoundingBoxes[box].Labels.Contains(label)) {
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
