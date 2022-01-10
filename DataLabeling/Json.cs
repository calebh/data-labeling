using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DataLabeling.Json
{
    public class ImageInfo {
        public string Path { get; set; }
        public List<BoxInfo> Boxes { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public ImageInfo(string path, List<BoxInfo> boxes, int width, int height) {
            Path = path;
            Boxes = boxes;
            Width = width;
            Height = height;
        }

        public IOExample ToIOExample() {
            ImageResource resource = new ImageResource(Path);
            IOExample ret = new IOExample(resource);
            foreach (BoxInfo box in Boxes) {
                ret.AddBox(box.Box, new ObjectLiteral(box.Label));
                if (box.PreciseLabel != null) {
                    ret.MakePrecise(box.Box, new ObjectLiteral(box.PreciseLabel));
                }
            }
            return ret;
        }
    }

    public class BoxInfo
    {
        public string Label { get; set; }
        public string? PreciseLabel { get; set; }
        public string? GroupLabel { get; set; }
        public BoundingBox Box { get; set; }

        public BoxInfo(string label, string? preciseLabel, string? groupLabel, BoundingBox box) {
            Label = label;
            PreciseLabel = preciseLabel;
            GroupLabel = groupLabel;
            Box = box;
        }
    }

    public static class JsonMethods
    {
        public static List<IOExample> Read(string jsonPath) {
            string jsonString = File.ReadAllText(jsonPath);
            return JsonSerializer.Deserialize<List<ImageInfo>>(jsonString).Select(imgInfo => imgInfo.ToIOExample()).ToList();
        }
    }
}
