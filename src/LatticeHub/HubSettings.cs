using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

using Lattice.Hub.Math;

namespace Lattice.Hub
{
    [Serializable]
    public class HubSettings
    {
        public Vec3f MinBounds = new Vec3f(-2f, -2f, 0.3f);
        public Vec3f MaxBounds = new Vec3f(2f, 2f, 4f);
        public bool Filter = true;
        public bool BodyOnly = false;
        public int MarkerId = -1;
        public bool Compress = true;
        public List<AnchorPose> Anchors = new List<AnchorPose>();

        public void SaveTo(string path)
        {
            var ser = new XmlSerializer(typeof(HubSettings));
            using (var w = File.Create(path)) ser.Serialize(w, this);
        }

        public static HubSettings LoadFrom(string path)
        {
            if (!File.Exists(path)) return new HubSettings();
            var ser = new XmlSerializer(typeof(HubSettings));
            using (var r = File.OpenRead(path)) return (HubSettings)ser.Deserialize(r);
        }
    }
}
