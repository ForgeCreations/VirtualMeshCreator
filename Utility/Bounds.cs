using System;
using VirtualMeshCreator.Math;

namespace VirtualMeshCreator.Utility
{
    public struct Bounds
    {
        public Vector3 center;
        public Vector3 extends;
        public Vector3 Min { get; private set; }
        public Vector3 Max { get; private set; }

        public Bounds(Vector3 center, Vector3 extends)
        {
            this.center = center;
            this.extends = extends;
            Min = Vector3.zero;
            Max = Vector3.one;
        }

        public static Bounds operator +(Bounds a, Bounds b)
        {
            Bounds bounds = new Bounds
            {
                Min = new Vector3
                (
                    System.Math.Min(a.Min.x, b.Min.x),
                    System.Math.Min(a.Min.y, b.Min.y),
                    System.Math.Min(a.Min.z, b.Min.z)
                ),

                Max = new Vector3
                (
                    System.Math.Max(a.Max.x, b.Max.x),
                    System.Math.Max(a.Max.y, b.Max.y),
                    System.Math.Max(a.Max.z, b.Max.z)
                )
            };
            return bounds;
        }

        public static Bounds operator +(Bounds a, Vector3 b)
        {
            Bounds bounds = new Bounds
            {
                Min = new Vector3
                (
                    System.Math.Min(a.Min.x, b.x),
                    System.Math.Min(a.Min.y, b.y),
                    System.Math.Min(a.Min.z, b.z)
                ),

                Max = new Vector3
                (
                    System.Math.Max(a.Max.x, b.x),
                    System.Math.Max(a.Max.y, b.y),
                    System.Math.Max(a.Max.z, b.z)
                )
            };
            return bounds;
        }
    }

    public struct Sphere
    {
        public Vector3 center;
        public float radius;

        public Sphere(Vector3 center, float radius)
        {
            this.center = center;
            this.radius = radius;
        }

        public static Sphere FromPoints(Vector3[] points, int size)
        {
            int[] min_idx = new int[3];
            int[] max_idx = new int[3];
            for(int i = 0; i < size; i++)
            {
                for(int k = 0; k < 3; k++)
                {
                    if(points[i] < points[min_idx[k]]) min_idx[k] = i;
                    if(points[i] > points[max_idx[k]]) max_idx[k] = i;
                }
            }
            float max_len = 0;
            int max_axis = 0;
            for(int k = 0; k < 3; k++)
            {
                Vector3 pmin1 = points[min_idx[k]];
                Vector3 pmax1 = points[max_idx[k]];
                float tlen = (pmax1 - pmin1).magnitude;
                if(tlen > max_len) max_len = tlen; max_axis = k;
            }
            Vector3 pmin = points[min_idx[max_axis]];
            Vector3 pmax = points[max_idx[max_axis]];

            Sphere sphere;
            sphere.center = (pmin + pmax) * 0.5f;
            sphere.radius = 0.5f * (float)System.Math.Sqrt(max_len);
            max_len = sphere.radius * sphere.radius;

            for(int i = 0; i < size; i++)
            {
                float len = (points[i] - sphere.center).magnitudeSqr;
                if(len > max_len)
                {
                    len = (float)System.Math.Sqrt(len);
                    float t = 0.5f - 0.5f * (sphere.radius / len);
                    sphere.center = sphere.center + (points[i] - sphere.center) * t;
                    sphere.radius = (sphere.radius + len) * 0.5f;
                    max_len = sphere.radius * sphere.radius;
                }
            }
            //
            for(int i = 0; i < size; i++)
            {
                float len = (points[i] - sphere.center).magnitude;
                //assert(len - 1e-6 <= sphere.radius);
                Console.WriteLine(len - 1e-6f <= sphere.radius);
            }
            return sphere;
        }

        public static Sphere FromSpheres(Sphere[] spheres, int size)
        {
            int[] min_idx = new int[3];
            int[] max_idx = new int[3];
            for(int i = 0; i < size; i++)
            {
                for(int k = 0; k < 3; k++)
                {
                    if(spheres[i].center - spheres[i].radius < spheres[min_idx[k]].center - spheres[min_idx[k]].radius)
                        min_idx[k] = i;
                    if(spheres[i].center + spheres[i].radius < spheres[max_idx[k]].center + spheres[max_idx[k]].radius)
                        max_idx[k] = i;
                }
            }
            float max_len = 0.0f;
            int max_axis = 0;
            for(int k = 0; k < 3; k++)
            {
                Sphere spmin = spheres[min_idx[k]];
                Sphere spmax = spheres[max_idx[k]];
                float tlen = (spmax.center - spmin.center).magnitude + spmax.radius + spmin.radius;
                if(tlen > max_len) max_len = tlen;  max_axis = k;
            }
            Sphere sphere = spheres[min_idx[max_axis]];
            sphere = sphere + spheres[max_idx[max_axis]];
            for(int i = 0; i < size; i++)
            {
                sphere = sphere + spheres[i];
            }
            //
            for(int i = 0; i < size; i++)
            {
                float t1 = (float)System.Math.Sqrt(sphere.radius - spheres[i].radius);
                float t2 = (sphere.center - spheres[i].center).magnitudeSqr;
                //assert(t1 + 1e-6f >= t2);
                Console.WriteLine(t1 + 1e-6f >= t2);
            }
            return sphere;
        }

        public static Sphere operator +(Sphere a, Sphere b)
        {
            Vector3 t = b.center - a.center;
            float tlen2 = t.magnitudeSqr;
            if((float)System.Math.Sqrt(a.radius - b.radius) >= tlen2)
            {
                return a.radius < b.radius ? b : a;
            }
            Sphere sphere;
            float tlen = (float)System.Math.Sqrt(tlen2);
            sphere.radius = (tlen + a.radius + b.radius) * 0.5f;
            sphere.center = a.center + t * ((sphere.radius - a.radius) / tlen);
            return sphere;
        }
    }
}
