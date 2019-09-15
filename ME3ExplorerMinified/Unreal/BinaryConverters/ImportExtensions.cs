﻿using System.Linq;
using Guid = System.Guid;

namespace ME3Explorer.Unreal.BinaryConverters
{
    public static class ConvertExtensions
    {
        public static StaticMesh ConvertToME3StaticMesh(this SkeletalMesh skeletalMesh)
        {
            StaticLODModel lodModel = skeletalMesh.LODModels[0];
            uint numVertices = lodModel.NumVertices;
            var stm = new StaticMesh
            {
                Bounds = skeletalMesh.Bounds,
                BodySetup = new UIndex(0),
                LODModels = new[] { new StaticMeshRenderData
                {
                    IndexBuffer = lodModel.IndexBuffer.TypedClone(),
                    NumVertices = numVertices,
                    Edges = new MeshEdge[0],
                    RawTriangles = new StaticMeshTriangle[0],
                    ColorVertexBuffer = new ColorVertexBuffer(),
                    ShadowTriangleDoubleSided = new byte[0],
                    WireframeIndexBuffer = new ushort[0],
                    ShadowExtrusionVertexBuffer = new ExtrusionVertexBuffer
                    {
                        Stride = 4,
                        VertexData = new float[0]
                    },
                    PositionVertexBuffer = new PositionVertexBuffer
                    {
                        NumVertices = numVertices,
                        Stride = 12,
                        VertexData = new Vector3[numVertices]
                    },
                    VertexBuffer = new StaticMeshVertexBuffer
                    {
                        bUseFullPrecisionUVs = false,
                        NumTexCoords = 1,
                        NumVertices = numVertices,
                        VertexData = new StaticMeshVertexBuffer.StaticMeshFullVertex[numVertices]
                    },
                    Elements = lodModel.Sections.Select(sec =>
                    {
                        var indices = lodModel.IndexBuffer.Skip((int)sec.BaseIndex).Take(sec.NumTriangles * 3).ToList();
                        return new StaticMeshElement
                        {
                            bEnableShadowCasting = true,
                            EnableCollision = true,
                            OldEnableCollision = true,
                            FirstIndex = sec.BaseIndex,
                            NumTriangles = (uint)sec.NumTriangles,
                            MaterialIndex = sec.MaterialIndex,
                            Material = skeletalMesh.Materials[sec.MaterialIndex],
                            Fragments = new FragmentRange[0],
                            MinVertexIndex = indices.Min(),
                            MaxVertexIndex = indices.Max()
                        };
                    }).ToArray()
                } },
                InternalVersion = 18,
                LightingGuid = Guid.NewGuid()
            };

            Vector3[] posVertData = stm.LODModels[0].PositionVertexBuffer.VertexData;
            StaticMeshVertexBuffer.StaticMeshFullVertex[] stmVertData = stm.LODModels[0].VertexBuffer.VertexData;
            if (lodModel.ME1VertexBufferGPUSkin != null)
            {
                for (int i = 0; i < lodModel.ME1VertexBufferGPUSkin.Length; i++)
                {
                    SoftSkinVertex vert = lodModel.ME1VertexBufferGPUSkin[i];
                    posVertData[i] = vert.Position;
                    stmVertData[i] = new StaticMeshVertexBuffer.StaticMeshFullVertex
                    {
                        HalfPrecisionUVs = new Vector2DHalf[] { vert.UV },
                        TangentX = vert.TangentX,
                        TangentZ = vert.TangentZ
                    };
                }
            }
            else
            {
                for (int i = 0; i < lodModel.VertexBufferGPUSkin.VertexData.Length; i++)
                {
                    GPUSkinVertex vert = lodModel.VertexBufferGPUSkin.VertexData[i];
                    posVertData[i] = vert.Position;
                    stmVertData[i] = new StaticMeshVertexBuffer.StaticMeshFullVertex
                    {
                        HalfPrecisionUVs = new[] { vert.UV },
                        TangentX = vert.TangentX,
                        TangentZ = vert.TangentZ
                    };
                }
            }
            var tris = new kDOPCollisionTriangle[lodModel.IndexBuffer.Length / 3];
            for (int i = 0, elIdx = 0, triCount = 0; i < lodModel.IndexBuffer.Length; i += 3, ++triCount)
            {
                if (triCount > lodModel.Sections[elIdx].NumTriangles)
                {
                    triCount = 0;
                    ++elIdx;
                }
                tris[i / 3] = new kDOPCollisionTriangle(lodModel.IndexBuffer[i], lodModel.IndexBuffer[i + 1], lodModel.IndexBuffer[i + 2],
                                                            lodModel.Sections[elIdx].MaterialIndex);
            }

            stm.kDOPTreeME3UDK = KDOPTreeBuilder.ToCompact(tris, stm.LODModels[0].PositionVertexBuffer.VertexData);

            return stm;
        }
    }
}
