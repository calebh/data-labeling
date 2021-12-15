using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLabeling
{
    public class BoundingBox
    {
        public readonly double Left;
        public readonly double Top;
        public readonly double Width;
        public readonly double Height;

        public BoundingBox(double left, double top, double width, double height) {
            Left = left;
            Top = top;
            Width = width;
            Height = height;
        }

        public double JaccardIndex(BoundingBox other) {
            double x1 = Left;
            double y1 = Top;
            double x2 = Left + Width;
            double y2 = Top + Height;

            double x1_other = other.Left;
            double y1_other = other.Top;
            double x2_other = other.Left + other.Width;
            double y2_other = other.Top + other.Height;

            double intersection_area = Math.Max(0, Math.Min(x2, x2_other) - Math.Max(x1, x1_other)) * Math.Max(0, Math.Min(y2, y2_other) - Math.Max(y1, y1_other));
            double union_area = (x2 - x1) * (y2 - y1) + (x2_other - x1_other) * (y2_other - y1_other) - intersection_area;
            return intersection_area / union_area;
        }
    }
}
