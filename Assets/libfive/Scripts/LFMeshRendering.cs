﻿using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using libfivesharp.libFiveInternal;

namespace libfivesharp
{
    public class LFMeshRendering
    {
        unsafe public struct RenderLibFiveMeshJob : IJob
        {
            [NativeDisableUnsafePtrRestriction]
            public IntPtr treeToRender;

            [ReadOnly]
            public libfive_region3 bounds;

            [ReadOnly]
            public float resolution;

            [NativeDisableUnsafePtrRestriction]
            public IntPtr* libFiveMeshPtr;

            public void Execute()
            {
                if (treeToRender == IntPtr.Zero) return;
                *libFiveMeshPtr = libfive.libfive_tree_render_mesh(treeToRender, bounds, resolution);
            }
        }

        public unsafe class RenderJobPayload
        {
            public JobHandle handle;

            public IntPtr* libFiveMeshPtr;

            public LFTree tree;

            public RenderJobPayload(ref LFTree tree)
            {
                unsafe { libFiveMeshPtr = (IntPtr*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<IntPtr>(), 4, Allocator.Persistent); }
                this.tree = tree;
            }
        }

        public static void ScheduleRenderLibFiveMesh(LFTree tree, Bounds bounds, float resolution, ref RenderJobPayload payload)
        {
            UnityEngine.Profiling.Profiler.BeginSample("Schedule Render Mesh");

            var bound = new libfive_region3();
            bound.X.lower = bounds.min.x;
            bound.Y.lower = bounds.min.y;
            bound.Z.lower = bounds.min.z;
            bound.X.upper = bounds.max.x;
            bound.Y.upper = bounds.max.y;
            bound.Z.upper = bounds.max.z;

            RenderLibFiveMeshJob curRenderJob;

            unsafe
            {
                curRenderJob = new RenderLibFiveMeshJob()
                {
                    treeToRender = tree.tree,
                    bounds = bound,
                    resolution = resolution,
                    libFiveMeshPtr = payload.libFiveMeshPtr
                };
            }

            payload.handle = curRenderJob.Schedule(payload.handle);
            JobHandle.ScheduleBatchedJobs();

            UnityEngine.Profiling.Profiler.EndSample();
        }

        unsafe public static void CompleteRenderLibFiveMesh(ref RenderJobPayload payload, Mesh toFill, float vertexSplittingAngle = 180f)
        {
            UnityEngine.Profiling.Profiler.BeginSample("Complete Rendering Mesh");

            if (payload != null && (*payload.libFiveMeshPtr) != IntPtr.Zero)
            {
                IntPtr meshPtr;
                unsafe { meshPtr = *payload.libFiveMeshPtr; }

                if (meshPtr != IntPtr.Zero)
                {
                    // Marshal the mesh into vertex and triangle arrays and assign to unity mesh.
                    var libFiveMesh = (libfive_mesh)Marshal.PtrToStructure(meshPtr, typeof(libfive_mesh));

                    var vertexFloats = new float[libFiveMesh.vert_count * 3];
                    var triangleIndices = new int[libFiveMesh.tri_count * 3];

                    Marshal.Copy(libFiveMesh.verts, vertexFloats, 0, vertexFloats.Length);
                    Marshal.Copy(libFiveMesh.tris, triangleIndices, 0, triangleIndices.Length);

                    var vertices = new List<Vector3>(vertexFloats.Length * 2 / 3);

                    for (var i = 0; i < vertexFloats.Length / 3; ++i) vertices.Add(new Vector3(vertexFloats[(i * 3)], vertexFloats[(i * 3) + 1], vertexFloats[(i * 3) + 2]));
                    for (var i = 0; i < triangleIndices.Length; ++i) triangleIndices[i] = (int)((UInt32)triangleIndices[i]); // (The original indices were UInt32, so cast to and from those since Unity still needs them as ints.)

                    toFill.Clear();
                    toFill.SetVertices(vertices);
                    toFill.SetTriangles(triangleIndices, 0);
                    toFill.RecalculateBounds();
                    toFill.RecalculateNormals(vertexSplittingAngle);

                    if (meshPtr != IntPtr.Zero) libfive.libfive_mesh_delete(meshPtr);
                }
            }

            UnityEngine.Profiling.Profiler.EndSample();
        }

        public static string createSTL(Mesh mesh, string solidName = "")
        {
            var vertices = mesh.vertices;
            var triangles = mesh.triangles;

            var builder = new System.Text.StringBuilder();
            builder.AppendLine("solid " + solidName);

            for (var i = 0; i < triangles.Length; i += 3)
            {
                builder.AppendLine("facet normal 0.0 0.0 0.0\r\nouter loop");

                for (var j = 0; j < 3; ++j)
                    builder.AppendLine("vertex " + vertices[triangles[i + j]].x + " " +
                                                   vertices[triangles[i + j]].y + " " +
                                                   vertices[triangles[i + j]].z);

                builder.AppendLine("endloop\r\nendfacet");
            }

            builder.AppendLine("endsolid");

            return builder.ToString();
        }
    }
}
