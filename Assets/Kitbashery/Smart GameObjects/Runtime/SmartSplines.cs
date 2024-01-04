using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* 

Smart GameObjects Copyright(c) 2023 Kitbashery. All rights reserved.
Development lead @ Kitbashery: Tolin Simpson

Permission is hereby granted to use & modify the software, to any person obtaining a copy
of this software and associated documentation files (the "Software") from the official 
Unity Asset Store & maintaining a valid license. Support for the software may be reserved for but not limited to
customers who provide a valid invoice number as confirmation of license ownership. Usage restrictions & other legal terms are defined by the
Unity Asset Store End-User License Agreement (EULA) For more information see: https://unity.com/legal/as-terms

Piracy is not a victimless crime!

The unathorized reproduction or distribution of this copyrighted work is illegal. Criminal copyright infringment is investigated by federal law enforcement agencies
and is punishable by up to 5 years of prison time and a fine of $250,000, for more information on how digital theft harms the economy see: https://www.iprcenter.gov/
if you suspect this software to be pirated please report the source to the copyright holder.

Need support or additional features? Please visit https://kitbashery.com/

*/

namespace Kitbashery.SmartGO
{
    public static class SmartSplines
    {
        public static Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            Vector3 p = uuu * p0;
            p += 3 * uu * t * p1;
            p += 3 * u * tt * p2;
            p += ttt * p3;

            return p;
        }

        public static Vector3 CalculateHermitePoint(float t, Vector3 p0, Vector3 p1, Vector3 m0, Vector3 m1)
        {
            float tt = t * t;
            float ttt = tt * t;
            float h1 = 2 * ttt - 3 * tt + 1;
            float h2 = -2 * ttt + 3 * tt;
            float h3 = ttt - 2 * tt + t;
            float h4 = ttt - tt;

            return h1 * p0 + h2 * p1 + h3 * m0 + h4 * m1;
        }

        public static Vector3 CalculateCatmullRomPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            float tt = t * t;
            float ttt = tt * t;

            return 0.5f * (2 * p1 + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * tt + (-p0 + 3 * p1 - 3 * p2 + p3) * ttt);
        }

        public static Vector3 CalculateBSplinePoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            float tt = t * t;
            float ttt = tt * t;

            return (1 / 6f) * (p0 * (-ttt + 3 * tt - 3 * t + 1) + p1 * (3 * ttt - 6 * tt + 4) + p2 * (-3 * ttt + 3 * tt + 3 * t + 1) + p3 * (ttt));
        }

        public static Vector3 CalculateLinearPoint(float t, Vector3 p0, Vector3 p1)
        {
            return Vector3.Lerp(p0, p1, t);
        }

        /// <summary>
        /// Calculates arch length parameterization.
        /// </summary>
        /// <param name="splinePoints">Must have at least 2 points.</param>
        /// <param name="numPoints">Must be at least 2.</param>
        /// <returns>Evenly spaced points.</returns>
        public static List<Vector3> GetEvenlySpacedPoints(List<Vector3> splinePoints, int numPoints)
        {
            float[] cumulativeLengths = new float[splinePoints.Count];
            cumulativeLengths[0] = 0f;
            float totalLength = 0f;

            for (int i = 1; i < splinePoints.Count; i++)
            {
                float segmentLength = (splinePoints[i] - splinePoints[i - 1]).magnitude;
                cumulativeLengths[i] = cumulativeLengths[i - 1] + segmentLength;
                totalLength += segmentLength;
            }

            List<Vector3> evenlySpacedPoints = new List<Vector3>();
            float step = totalLength / (numPoints - 1);
            float currentLength = 0f;
            int currentIndex = 0;

            for (int i = 0; i < numPoints - 1; i++)
            {
                while (currentIndex < splinePoints.Count - 2 && currentLength > cumulativeLengths[currentIndex + 1])
                {
                    currentIndex++;
                }

                evenlySpacedPoints.Add(Vector3.Lerp(splinePoints[currentIndex], splinePoints[currentIndex + 1], (currentIndex == splinePoints.Count - 2) ? 1f : (currentLength - cumulativeLengths[currentIndex]) / (cumulativeLengths[currentIndex + 1] - cumulativeLengths[currentIndex])));
                currentLength += step;
            }

            evenlySpacedPoints.Add(splinePoints[splinePoints.Count - 1]);
            return evenlySpacedPoints;
        }
    }

}