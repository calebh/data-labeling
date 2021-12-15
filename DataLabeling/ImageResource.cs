using System;
using System.Collections.Generic;
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
    }
}
