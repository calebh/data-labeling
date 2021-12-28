using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLabeling.Color
{
    public struct RGB
    {
        public readonly double R;
        public readonly double G;
        public readonly double B;

        public RGB(double r, double g, double b) {
            R = r;
            G = g;
            B = b;
        }

        public RGB(YUV yuv) {
            R = yuv.Y + 1.13983 * yuv.V;
            G = yuv.Y - 0.39465 * yuv.U - 0.58060 * yuv.V;
            B = yuv.Y + 2.03211 * yuv.U;
        }
    }

    public struct YUV
    {
        public readonly double Y;
        public readonly double U;
        public readonly double V;

        public YUV(double y, double u, double v) {
            Y = y;
            U = u;
            V = v;
        }

        public YUV(RGB rgb) {
            Y = 0.299 * rgb.R + 0.587 * rgb.G + 0.114 * rgb.B;
            U = -0.14713 * rgb.R - 0.28886 * rgb.G + 0.436 * rgb.B;
            V = 0.615 * rgb.R - 0.51499 * rgb.G - 0.10001 * rgb.B;
        }

        public static YUV operator+(YUV a, YUV b) {
            return new YUV(a.Y + b.Y, a.U + b.U, a.V + b.V);
        }

        public static YUV operator/(YUV color, double divisor) {
            return new YUV(color.Y / divisor, color.U / divisor, color.V / divisor);
        }
    }
}
