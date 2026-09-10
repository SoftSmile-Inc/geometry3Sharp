using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace g3
{
    // ported from WildMagic 5 
    // https://www.geometrictools.com/Downloads/Downloads.html

    public class DistSegment3Triangle3
    {
        private Segment3d _segment;
        private Triangle3d _triangle;
        private DistLine3Triangle3 _queryLt;

        public Segment3d Segment
        {
            get { return _segment; }
            set { _segment = value; DistanceSquared = -1.0; }
        }

        public Triangle3d Triangle
        {
            get { return _triangle; }
            set { _triangle = value; DistanceSquared = -1.0; }
        }

        public double DistanceSquared = -1.0;

        public Vector3d SegmentClosest;
        public double SegmentParam;
        public Vector3d TriangleClosest;
        public Vector3d TriangleBaryCoords;

        public DistSegment3Triangle3(Segment3d segmentIn, Triangle3d triangleIn)
        {
            _triangle = triangleIn;
            _segment = segmentIn;
        }


        public DistSegment3Triangle3 Compute()
        {
            GetSquared();
            return this;
        }

        public double Get()
        {
            return Math.Sqrt(GetSquared());
        }


        public double GetSquared()
        {
            if (DistanceSquared >= 0)
                return DistanceSquared;
            Line3d line = new Line3d(_segment.Center, _segment.Direction);
            _queryLt ??= new DistLine3Triangle3(line, _triangle);
            _queryLt.Line = line;
            _queryLt.Triangle = _triangle;
            double sqrDist = _queryLt.GetSquared();
            SegmentParam = _queryLt.LineParam;

            if (SegmentParam >= -_segment.Extent) {
                if (SegmentParam <= _segment.Extent) {
                    SegmentClosest = _queryLt.LineClosest;
                    TriangleClosest = _queryLt.TriangleClosest;
                    TriangleBaryCoords = _queryLt.TriangleBaryCoords;
                } else {
                    SegmentClosest = _segment.P1;
                    sqrDist = DistPoint3Triangle3.DistanceSqr(ref SegmentClosest, ref _triangle, out TriangleClosest, out TriangleBaryCoords);
                    SegmentParam = _segment.Extent;
                }
            } else {
                SegmentClosest = _segment.P0;
                sqrDist = DistPoint3Triangle3.DistanceSqr(ref SegmentClosest, ref _triangle, out TriangleClosest, out TriangleBaryCoords);
                SegmentParam = -_segment.Extent;
            }

            DistanceSquared = sqrDist;
            return DistanceSquared;
        }
    }
}
