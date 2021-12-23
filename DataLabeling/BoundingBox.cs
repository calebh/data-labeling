using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLabeling
{
    public class BoundingBox
    {
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

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

        public double ContainmentFraction(BoundingBox other) {
            double x1 = Left;
            double y1 = Top;
            double x2 = Left + Width;
            double y2 = Top + Height;

            double x1Other = other.Left;
            double y1Other = other.Top;
            double x2Other = other.Left + other.Width;
            double y2Other = other.Top + other.Height;

            double x1OtherClipped = Math.Clamp(x1Other, x1, x2);
            double y1OtherClipped = Math.Clamp(y1Other, y1, y2);
            double x2OtherClipped = Math.Clamp(x2Other, x1, x2);
            double y2OtherClipped = Math.Clamp(y2Other, y1, y2);

            double clippedArea = (x2OtherClipped - x1OtherClipped) * (y2OtherClipped - y1OtherClipped);
            return clippedArea / (Width * Height);
        }
    }
}
