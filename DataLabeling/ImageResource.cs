using DataLabeling.Color;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLabeling
{
    public class ImageResource
    {
        public string Path { get; private set; }

        public ImageResource(string path) {
            Path = path;
        }

        private Dictionary<BoundingBox, YUV> MemoizedAverageColors = new Dictionary<BoundingBox, YUV>();

        public YUV AverageColor(BoundingBox bbox) {
            if (MemoizedAverageColors.ContainsKey(bbox)) {
                return MemoizedAverageColors[bbox];
            }

            Bitmap bitmap = CropImage(bbox);
            double divisor = bitmap.Width * bitmap.Height;
            YUV ret = new YUV();
            for (int x = 0; x < bitmap.Width; x++) {
                for (int y = 0; y < bitmap.Height; y++) {
                    System.Drawing.Color color = bitmap.GetPixel(x, y);
                    RGB rgb = new RGB(color.R / 255.0, color.G / 255.0, color.B / 255.0);
                    YUV yuv = new YUV(rgb);
                    ret = ret + (yuv / divisor);
                }
            }
            bitmap.Dispose();

            MemoizedAverageColors[bbox] = ret;

            return ret;
        }

        public Bitmap CropImage(BoundingBox bbox) {
            // Example use:     
            Bitmap source = new Bitmap(Path);
            int x1 = (int) (bbox.Left * source.Width);
            int y1 = (int) (bbox.Top * source.Height);
            int width = (int) (bbox.Width * source.Width);
            int height = (int) (bbox.Height * source.Height);
            Rectangle section = new Rectangle(new Point(x1, y1), new Size(width, height));

            var bitmap = new Bitmap(section.Width, section.Height);
            using (var g = Graphics.FromImage(bitmap)) {
                g.DrawImage(source, 0, 0, section, GraphicsUnit.Pixel);
                source.Dispose();
                return bitmap;
            }
        }
    }
}
