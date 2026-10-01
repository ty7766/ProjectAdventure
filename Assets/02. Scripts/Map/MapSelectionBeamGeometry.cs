using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 맵 선택 빔의 지오메트리 계산 (MonoBehaviour 비의존).
/// - 맵 콜라이더 footprint 샘플링 + 마칭 스퀘어 윤곽 추출
/// - 윤곽 세그먼트로 벽 빔 + 안쪽 링 메시 생성
/// 세그먼트는 [a, b, a, b, ...] 형태의 월드 XZ 평탄 목록이며,
/// 모든 세그먼트는 진행 방향 기준 '왼쪽'이 맵 내부가 되도록 일관된 방향을 가진다.
/// </summary>
public static class MapSelectionBeamGeometry
{
    private const int MaxGridSize = 96;
    private const int OverlapBufferSize = 64;
    private static readonly Collider[] s_overlapBuffer = new Collider[OverlapBufferSize];

    // ------------------------------------------------------------------
    // Footprint 샘플링 + 마칭 스퀘어
    // ------------------------------------------------------------------

    /// <summary>
    /// 맵 콜라이더를 그리드로 샘플링해 footprint 윤곽 세그먼트를 추출합니다. 실패 시 null.
    /// </summary>
    public static List<Vector2> ExtractFootprintSegments(GameObject mapInstance, Bounds worldBounds, float margin, float samplingResolution)
    {
        var colliders = mapInstance.GetComponentsInChildren<Collider>();
        if (colliders == null || colliders.Length == 0)
        {
            return null;
        }

        // 맵 콜라이더가 속한 레이어만 질의해 플레이어/보석 등 다른 오브젝트가 버퍼를 채우는 것을 방지
        int layerMask = 0;
        foreach (Collider collider in colliders)
        {
            if (!collider.isTrigger)
            {
                layerMask |= 1 << collider.gameObject.layer;
            }
        }
        if (layerMask == 0)
        {
            return null;
        }

        // 마진을 더한 확장 영역을 샘플링
        Bounds sampleBounds = worldBounds;
        sampleBounds.Expand(new Vector3(margin * 2f, 0f, margin * 2f));

        float res = Mathf.Max(0.1f, samplingResolution);

        // 셀 기반 샘플링: 각 셀의 중심에서 콜라이더 적중을 검사하고,
        // 밖(=false)셀과 안(=true)셀 사이에서 경계가 검출되도록 한 겹의 바깥 패딩을 둔다.
        int paddedCols = Mathf.CeilToInt(sampleBounds.size.x / res) + 2;
        int paddedRows = Mathf.CeilToInt(sampleBounds.size.z / res) + 2;

        // 과도한 해상도 방지
        if (paddedCols > MaxGridSize || paddedRows > MaxGridSize)
        {
            float scale = Mathf.Max((float)paddedCols / MaxGridSize, (float)paddedRows / MaxGridSize);
            res *= scale;
            paddedCols = Mathf.CeilToInt(sampleBounds.size.x / res) + 2;
            paddedRows = Mathf.CeilToInt(sampleBounds.size.z / res) + 2;
        }

        // 패딩 제외한 실제 샘플 영역: sampleBounds를 셀 중심들로 덮도록 조정
        float cellOriginX = sampleBounds.min.x + (sampleBounds.size.x - (paddedCols - 2) * res) * 0.5f - res * 0.5f;
        float cellOriginZ = sampleBounds.min.z + (sampleBounds.size.z - (paddedRows - 2) * res) * 0.5f - res * 0.5f;
        float centerY = sampleBounds.center.y;
        float halfHeight = Mathf.Max(0.5f, sampleBounds.extents.y);

        // 샘플: true면 셀이 맵 내부. 가장자리 1열/1행은 항상 false(바깥 보장)
        bool[,] inside = new bool[paddedRows, paddedCols];
        Vector3 half = new Vector3(res * 0.45f, halfHeight, res * 0.45f);
        Transform mapRoot = mapInstance.transform;

        for (int i = 1; i < paddedRows - 1; i++)
        {
            float z = cellOriginZ + (i - 0.5f) * res;
            for (int j = 1; j < paddedCols - 1; j++)
            {
                float x = cellOriginX + (j - 0.5f) * res;
                Vector3 center = new Vector3(x, centerY, z);

                int hitCount = Physics.OverlapBoxNonAlloc(center, half, s_overlapBuffer, Quaternion.identity,
                    layerMask, QueryTriggerInteraction.Ignore);
                bool isInside = false;
                for (int k = 0; k < hitCount; k++)
                {
                    if (s_overlapBuffer[k] != null && s_overlapBuffer[k].transform.IsChildOf(mapRoot))
                    {
                        isInside = true;
                        break;
                    }
                }
                inside[i, j] = isInside;
            }
        }

        // 마칭 스퀘어는 셀 중심을 노드로 보고 노드 사이에서 경계를 찾는다.
        // 패딩(=false) 프레임이 있으므로 모든 외곽 경계가 검출된다.
        float nodeOriginX = cellOriginX - res * 0.5f;
        float nodeOriginZ = cellOriginZ - res * 0.5f;
        List<Vector2> contour = MarchingSquares(inside, nodeOriginX, nodeOriginZ, res);
        return contour != null && contour.Count > 0 ? contour : null;
    }

    /// <summary>
    /// 이진 그리드에서 마칭 스퀘어로 윤곽 세그먼트를 추출합니다.
    /// 그리드 (i, j)는 월드 (originX + j*res, originZ + i*res)에 대응하며,
    /// 모든 세그먼트는 진행 방향의 왼쪽이 내부(true)가 되도록 방향이 정해진다.
    /// </summary>
    public static List<Vector2> MarchingSquares(bool[,] grid, float originX, float originZ, float res)
    {
        int rows = grid.GetLength(0);
        int cols = grid.GetLength(1);
        if (rows < 2 || cols < 2)
        {
            return null;
        }

        Vector2 P(int i, int j) => new Vector2(originX + j * res, originZ + i * res);
        Vector2 M(int i0, int j0, int i1, int j1) => (P(i0, j0) + P(i1, j1)) * 0.5f;

        var flat = new List<Vector2>();
        void Add(Vector2 from, Vector2 to)
        {
            flat.Add(from);
            flat.Add(to);
        }

        for (int i = 0; i < rows - 1; i++)
        {
            for (int j = 0; j < cols - 1; j++)
            {
                // a = (i, j), b = (i, j+1), c = (i+1, j+1), d = (i+1, j)
                bool a = grid[i, j];
                bool b = grid[i, j + 1];
                bool c = grid[i + 1, j + 1];
                bool d = grid[i + 1, j];

                int code = (a ? 1 : 0) | (b ? 2 : 0) | (c ? 4 : 0) | (d ? 8 : 0);
                if (code == 0 || code == 15)
                {
                    continue;
                }

                Vector2 ab = M(i, j, i, j + 1);
                Vector2 bc = M(i, j + 1, i + 1, j + 1);
                Vector2 dc = M(i + 1, j, i + 1, j + 1);
                Vector2 ad = M(i, j, i + 1, j);

                switch (code)
                {
                    case 1: Add(ab, ad); break;                 // a
                    case 2: Add(bc, ab); break;                 // b
                    case 3: Add(bc, ad); break;                 // a+b
                    case 4: Add(dc, bc); break;                 // c
                    case 5: Add(ab, ad); Add(dc, bc); break;    // a+c (모호: 분리된 두 모서리로 처리)
                    case 6: Add(dc, ab); break;                 // b+c
                    case 7: Add(dc, ad); break;                 // d 제외
                    case 8: Add(ad, dc); break;                 // d
                    case 9: Add(ab, dc); break;                 // a+d
                    case 10: Add(bc, ab); Add(ad, dc); break;   // b+d (모호: 분리된 두 모서리로 처리)
                    case 11: Add(bc, dc); break;                // c 제외
                    case 12: Add(ad, bc); break;                // c+d
                    case 13: Add(ab, bc); break;                // b 제외
                    case 14: Add(ad, ab); break;                // a 제외
                }
            }
        }

        return flat.Count > 0 ? flat : null;
    }

    /// <summary>
    /// 사각 윤곽 세그먼트를 만듭니다 (AABB 폴백). 반시계 방향이므로 왼쪽이 내부.
    /// </summary>
    public static List<Vector2> BuildRectangleSegments(Vector2 center, Vector2 size)
    {
        float hx = size.x * 0.5f;
        float hz = size.y * 0.5f;
        var p0 = new Vector2(center.x - hx, center.y - hz);
        var p1 = new Vector2(center.x + hx, center.y - hz);
        var p2 = new Vector2(center.x + hx, center.y + hz);
        var p3 = new Vector2(center.x - hx, center.y + hz);
        return new List<Vector2> { p0, p1, p1, p2, p2, p3, p3, p0 };
    }

    // ------------------------------------------------------------------
    // 메시 생성
    // ------------------------------------------------------------------

    /// <summary>
    /// 윤곽 세그먼트로 벽 빔 + 안쪽 링 메시를 pivot 기준 로컬 좌표로 만들어 mesh에 적용합니다.
    /// </summary>
    /// <returns>생성된 삼각형이 없으면 false (mesh는 변경되지 않음)</returns>
    public static bool BuildMesh(Mesh mesh, List<Vector2> segments, Vector3 pivot, float beamHeight, float ringWidth)
    {
        int capacity = segments.Count * 4;
        var vertices = new List<Vector3>(capacity);
        var normals = new List<Vector3>(capacity);
        var uvs = new List<Vector2>(capacity);
        var triangles = new List<int>(segments.Count * 6);

        // 벽: 세그먼트마다 쿼드 1개. UV.x는 0.5 고정(스캔만 사용), UV.y는 높이.
        for (int s = 0; s + 1 < segments.Count; s += 2)
        {
            Vector2 a = segments[s];
            Vector2 b = segments[s + 1];
            if (!TryGetInward(a, b, out Vector2 inward))
            {
                continue;
            }

            Vector3 outward = new Vector3(-inward.x, 0f, -inward.y);
            int start = vertices.Count;
            vertices.Add(ToLocal(a, 0f, pivot));
            vertices.Add(ToLocal(b, 0f, pivot));
            vertices.Add(ToLocal(b, beamHeight, pivot));
            vertices.Add(ToLocal(a, beamHeight, pivot));
            for (int k = 0; k < 4; k++)
            {
                normals.Add(outward);
            }
            uvs.Add(new Vector2(0.5f, 0f));
            uvs.Add(new Vector2(0.5f, 0f));
            uvs.Add(new Vector2(0.5f, 1f));
            uvs.Add(new Vector2(0.5f, 1f));
            AddQuad(triangles, start);
        }

        // 안쪽 링: 세그먼트의 내부 방향(왼쪽)으로 ringWidth 만큼 들어온 평행 쿼드 (바닥 하이라이트)
        // 세그먼트 방향 자체가 내부를 가리키므로 오목한(L자 등) 맵에서도 링이 뒤집히지 않는다.
        const float RingY = 0.02f;
        if (ringWidth > 0.001f)
        {
            for (int s = 0; s + 1 < segments.Count; s += 2)
            {
                Vector2 a = segments[s];
                Vector2 b = segments[s + 1];
                if (!TryGetInward(a, b, out Vector2 inward))
                {
                    continue;
                }

                Vector2 aIn = a + inward * ringWidth;
                Vector2 bIn = b + inward * ringWidth;

                int start = vertices.Count;
                vertices.Add(ToLocal(a, RingY, pivot));
                vertices.Add(ToLocal(b, RingY, pivot));
                vertices.Add(ToLocal(bIn, RingY, pivot));
                vertices.Add(ToLocal(aIn, RingY, pivot));
                for (int k = 0; k < 4; k++)
                {
                    normals.Add(Vector3.up); // 쉐이더의 floor(링) 브랜치 사용
                }
                uvs.Add(new Vector2(0f, 0.5f));
                uvs.Add(new Vector2(1f, 0.5f));
                uvs.Add(new Vector2(1f, 0.5f));
                uvs.Add(new Vector2(0f, 0.5f));
                AddQuad(triangles, start);
            }
        }

        if (triangles.Count == 0)
        {
            return false;
        }

        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return true;
    }

    private static bool TryGetInward(Vector2 a, Vector2 b, out Vector2 inward)
    {
        Vector2 dir = b - a;
        float len = dir.magnitude;
        if (len < 0.0001f)
        {
            inward = default;
            return false;
        }
        // 진행 방향의 왼쪽 법선 = 내부
        inward = new Vector2(-dir.y, dir.x) / len;
        return true;
    }

    private static Vector3 ToLocal(Vector2 point, float y, Vector3 pivot)
    {
        return new Vector3(point.x - pivot.x, y, point.y - pivot.z);
    }

    private static void AddQuad(List<int> triangles, int start)
    {
        triangles.Add(start + 0);
        triangles.Add(start + 1);
        triangles.Add(start + 2);
        triangles.Add(start + 0);
        triangles.Add(start + 2);
        triangles.Add(start + 3);
    }
}
