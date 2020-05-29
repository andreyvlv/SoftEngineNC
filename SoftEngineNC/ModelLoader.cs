using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Numerics;

namespace SoftEngineNC
{
    public class ModelLoader
    {
        public static async Task<Mesh[]> LoadBabylonModel(string fileName)
        {
            var meshes = new List<Mesh>();
            var data = "";
            using (var reader = new StreamReader(fileName))
            {
                data = await reader.ReadToEndAsync();
            }

            dynamic jsonObject = Newtonsoft.Json.JsonConvert.DeserializeObject(data);

            for (var meshIndex = 0; meshIndex < jsonObject.meshes.Count; meshIndex++)
            {
                var verticesArray = jsonObject.meshes[meshIndex].positions;

                // Faces
                var indicesArray = jsonObject.meshes[meshIndex].indices;

                var normalsArray = jsonObject.meshes[meshIndex].normals;

                var verticesStep = 3;

                // the number of interesting vertices information for us
                var verticesCount = verticesArray.Count / verticesStep;

                // number of faces is logically the size of the array divided by 3 (A, B, C)
                var facesCount = indicesArray.Count / 3;
                var mesh = new Mesh(jsonObject.meshes[meshIndex].name.Value, verticesCount, facesCount);

                // Filling the Vertices array of our mesh first               
                for (var index = 0; index < verticesCount; index++)
                {
                    var x = (float)verticesArray[index * verticesStep].Value;
                    var y = (float)verticesArray[index * verticesStep + 1].Value;
                    var z = (float)verticesArray[index * verticesStep + 2].Value;

                    // Loading the vertex normal exported by Blender
                    var nx = (float)normalsArray[index * verticesStep].Value;
                    var ny = (float)normalsArray[index * verticesStep + 1].Value;
                    var nz = (float)normalsArray[index * verticesStep + 2].Value;
                    mesh.Vertices[index] = new Vertex { Coordinates = new Vector3(x, y, z), Normal = new Vector3(nx, ny, nz) };
                }

                // Then filling the Faces array
                for (var index = 0; index < facesCount; index++)
                {
                    var a = (int)indicesArray[index * 3].Value;
                    var b = (int)indicesArray[index * 3 + 1].Value;
                    var c = (int)indicesArray[index * 3 + 2].Value;
                    mesh.Faces[index] = new Face { A = a, B = b, C = c };
                }

                // Getting the position you've set in Blender
                var position = jsonObject.meshes[meshIndex].position;
                mesh.Position = new Vector3((float)position[0].Value, (float)position[1].Value, (float)position[2].Value);

                var rotation = jsonObject.meshes[meshIndex].rotation;
                mesh.Rotation = new Vector3((float)rotation[0].Value, (float)rotation[1].Value, (float)rotation[2].Value);

                mesh.ComputeFacesNormal();

                meshes.Add(mesh);
            }
            return meshes.ToArray();
        }        
    }
}
