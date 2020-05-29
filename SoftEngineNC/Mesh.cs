using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SoftEngineNC
{
    public class Mesh
    {
        public string Name { get; set; }
        public Vertex[] Vertices { get; private set; }
        public Face[] Faces { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }

        public Mesh(string name, int verticesCount, int facesCount)
        {
            Vertices = new Vertex[verticesCount];
            Faces = new Face[facesCount];
            Name = name;
        }

        public void ComputeFacesNormal()
        {
            Parallel.For(0, Faces.Length, faceIndex =>
            {
                var face = Faces[faceIndex];
                var vertexA = Vertices[face.A];
                var vertexB = Vertices[face.B];
                var vertexC = Vertices[face.C];

                // compute face normal for face culling
                Faces[faceIndex].Normal = Vector3.Normalize(Vector3.
                    Cross((vertexA.Coordinates - vertexB.Coordinates), (vertexB.Coordinates - vertexC.Coordinates)));
            });
        }
    }
}
