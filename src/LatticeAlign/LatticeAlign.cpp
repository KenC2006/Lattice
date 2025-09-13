#define LATTICE_ALIGN_BUILD
#include "LatticeAlign.h"

#include <algorithm>
#include <array>
#include <cmath>
#include <vector>

#include "nanoflann.hpp"

namespace {

struct TargetAdaptor {
    const float* xyz;
    int n;

    inline size_t kdtree_get_point_count() const { return static_cast<size_t>(n); }

    inline float kdtree_get_pt(const size_t idx, int dim) const {
        return xyz[idx * 3 + dim];
    }

    template <class BBOX>
    bool kdtree_get_bbox(BBOX&) const { return false; }
};

using KDTree = nanoflann::KDTreeSingleIndexAdaptor<
    nanoflann::L2_Simple_Adaptor<float, TargetAdaptor>,
    TargetAdaptor, 3>;

struct Vec3 {
    double x = 0, y = 0, z = 0;
    Vec3() = default;
    Vec3(double xv, double yv, double zv) : x(xv), y(yv), z(zv) {}
    Vec3 operator+(const Vec3& o) const { return {x + o.x, y + o.y, z + o.z}; }
    Vec3 operator-(const Vec3& o) const { return {x - o.x, y - o.y, z - o.z}; }
    Vec3 operator*(double s) const { return {x * s, y * s, z * s}; }
};

void svd3x3(const std::array<double, 9>& A,
            std::array<double, 9>& U,
            std::array<double, 3>& sigma,
            std::array<double, 9>& Vt) {
    std::array<double, 9> AtA{};
    for (int i = 0; i < 3; ++i)
        for (int j = 0; j < 3; ++j)
            for (int k = 0; k < 3; ++k)
                AtA[i * 3 + j] += A[k * 3 + i] * A[k * 3 + j];

    std::array<double, 9> V = { 1,0,0, 0,1,0, 0,0,1 };
    for (int sweep = 0; sweep < 32; ++sweep) {
        double off = 0;
        for (int i = 0; i < 3; ++i)
            for (int j = i + 1; j < 3; ++j)
                off += AtA[i * 3 + j] * AtA[i * 3 + j];
        if (off < 1e-18) break;
        for (int p = 0; p < 2; ++p) {
            for (int q = p + 1; q < 3; ++q) {
                const double app = AtA[p * 3 + p];
                const double aqq = AtA[q * 3 + q];
                const double apq = AtA[p * 3 + q];
                if (std::fabs(apq) < 1e-20) continue;
                const double theta = (aqq - app) / (2.0 * apq);
                const double t = (theta >= 0)
                    ? 1.0 / (theta + std::sqrt(1.0 + theta * theta))
                    : 1.0 / (theta - std::sqrt(1.0 + theta * theta));
                const double c = 1.0 / std::sqrt(1.0 + t * t);
                const double s = t * c;
                for (int r = 0; r < 3; ++r) {
                    const double a = AtA[r * 3 + p];
                    const double b = AtA[r * 3 + q];
                    AtA[r * 3 + p] = c * a - s * b;
                    AtA[r * 3 + q] = s * a + c * b;
                }
                for (int r = 0; r < 3; ++r) {
                    const double a = AtA[p * 3 + r];
                    const double b = AtA[q * 3 + r];
                    AtA[p * 3 + r] = c * a - s * b;
                    AtA[q * 3 + r] = s * a + c * b;
                }
                for (int r = 0; r < 3; ++r) {
                    const double a = V[r * 3 + p];
                    const double b = V[r * 3 + q];
                    V[r * 3 + p] = c * a - s * b;
                    V[r * 3 + q] = s * a + c * b;
                }
            }
        }
    }
    for (int i = 0; i < 3; ++i) {
        sigma[i] = std::sqrt(std::max(0.0, AtA[i * 3 + i]));
    }
    std::array<int, 3> order{0, 1, 2};
    std::sort(order.begin(), order.end(), [&](int a, int b){ return sigma[a] > sigma[b]; });
    std::array<double, 3> sortedSigma;
    std::array<double, 9> sortedV{};
    for (int i = 0; i < 3; ++i) {
        sortedSigma[i] = sigma[order[i]];
        for (int r = 0; r < 3; ++r) sortedV[r * 3 + i] = V[r * 3 + order[i]];
    }
    sigma = sortedSigma;
    V = sortedV;

    std::array<double, 9> AV{};
    for (int i = 0; i < 3; ++i)
        for (int j = 0; j < 3; ++j)
            for (int k = 0; k < 3; ++k)
                AV[i * 3 + j] += A[i * 3 + k] * V[k * 3 + j];

    for (int j = 0; j < 3; ++j) {
        const double inv = sigma[j] > 1e-12 ? 1.0 / sigma[j] : 0.0;
        for (int i = 0; i < 3; ++i) U[i * 3 + j] = AV[i * 3 + j] * inv;
    }

    for (int i = 0; i < 3; ++i)
        for (int j = 0; j < 3; ++j)
            Vt[i * 3 + j] = V[j * 3 + i];
}

void procrustes(const std::vector<Vec3>& src, const std::vector<Vec3>& dst,
                std::array<float, 9>& R, std::array<float, 3>& t) {
    const size_t n = src.size();
    Vec3 cs, cd;
    for (size_t i = 0; i < n; ++i) { cs = cs + src[i]; cd = cd + dst[i]; }
    cs = cs * (1.0 / n);
    cd = cd * (1.0 / n);

    std::array<double, 9> H{};
    for (size_t i = 0; i < n; ++i) {
        const Vec3 a = src[i] - cs;
        const Vec3 b = dst[i] - cd;
        H[0] += a.x * b.x; H[1] += a.x * b.y; H[2] += a.x * b.z;
        H[3] += a.y * b.x; H[4] += a.y * b.y; H[5] += a.y * b.z;
        H[6] += a.z * b.x; H[7] += a.z * b.y; H[8] += a.z * b.z;
    }

    std::array<double, 9> U, Vt;
    std::array<double, 3> sigma;
    svd3x3(H, U, sigma, Vt);

    std::array<double, 9> Rd{};
    for (int i = 0; i < 3; ++i)
        for (int j = 0; j < 3; ++j)
            for (int k = 0; k < 3; ++k)
                Rd[i * 3 + j] += Vt[k * 3 + i] * U[j * 3 + k];

    const double det = Rd[0]*(Rd[4]*Rd[8] - Rd[5]*Rd[7])
                     - Rd[1]*(Rd[3]*Rd[8] - Rd[5]*Rd[6])
                     + Rd[2]*(Rd[3]*Rd[7] - Rd[4]*Rd[6]);
    if (det < 0) {
        for (int i = 0; i < 3; ++i) Rd[i * 3 + 2] = -Rd[i * 3 + 2];
    }

    for (int i = 0; i < 9; ++i) R[i] = static_cast<float>(Rd[i]);

    const Vec3 rotCs(
        Rd[0]*cs.x + Rd[1]*cs.y + Rd[2]*cs.z,
        Rd[3]*cs.x + Rd[4]*cs.y + Rd[5]*cs.z,
        Rd[6]*cs.x + Rd[7]*cs.y + Rd[8]*cs.z);
    t[0] = static_cast<float>(cd.x - rotCs.x);
    t[1] = static_cast<float>(cd.y - rotCs.y);
    t[2] = static_cast<float>(cd.z - rotCs.z);
}

}

extern "C" LATTICE_API float __stdcall lattice_align(
    const float* sourceXYZ, int sourceCount,
    const float* targetXYZ, int targetCount,
    float* outRotation,
    float* outTranslation,
    int maxIterations,
    float trimRatio)
{
    if (sourceCount < 4 || targetCount < 4) return -1.0f;
    if (maxIterations <= 0) maxIterations = 10;
    if (trimRatio < 0.1f || trimRatio > 1.0f) trimRatio = 0.8f;

    TargetAdaptor adaptor{targetXYZ, targetCount};
    KDTree tree(3, adaptor, nanoflann::KDTreeSingleIndexAdaptorParams(16));
    tree.buildIndex();

    std::vector<Vec3> src;
    src.reserve(sourceCount);
    for (int i = 0; i < sourceCount; ++i) {
        src.emplace_back(sourceXYZ[i * 3 + 0], sourceXYZ[i * 3 + 1], sourceXYZ[i * 3 + 2]);
    }

    std::array<float, 9> R = { 1,0,0, 0,1,0, 0,0,1 };
    std::array<float, 3> t = { 0,0,0 };

    float prevError = 1e30f;
    for (int iter = 0; iter < maxIterations; ++iter) {
        std::vector<Vec3> transformed(sourceCount);
        for (int i = 0; i < sourceCount; ++i) {
            const Vec3& p = src[i];
            transformed[i] = {
                R[0] * p.x + R[1] * p.y + R[2] * p.z + t[0],
                R[3] * p.x + R[4] * p.y + R[5] * p.z + t[1],
                R[6] * p.x + R[7] * p.y + R[8] * p.z + t[2],
            };
        }

        std::vector<std::pair<float, int>> dists(sourceCount);
        std::vector<size_t> nn(sourceCount, 0);

        #pragma omp parallel for
        for (int i = 0; i < sourceCount; ++i) {
            const float q[3] = {
                static_cast<float>(transformed[i].x),
                static_cast<float>(transformed[i].y),
                static_cast<float>(transformed[i].z),
            };
            size_t idx = 0;
            float d2 = 0.0f;
            nanoflann::KNNResultSet<float> rs(1);
            rs.init(&idx, &d2);
            tree.findNeighbors(rs, q, nanoflann::SearchParams(10));
            nn[i] = idx;
            dists[i] = { d2, i };
        }

        std::sort(dists.begin(), dists.end(),
                  [](const auto& a, const auto& b){ return a.first < b.first; });

        const int keep = std::max(4, static_cast<int>(sourceCount * trimRatio));
        std::vector<Vec3> pairsSrc, pairsDst;
        pairsSrc.reserve(keep);
        pairsDst.reserve(keep);
        double err = 0.0;
        for (int i = 0; i < keep; ++i) {
            const int srcIdx = dists[i].second;
            const size_t dstIdx = nn[srcIdx];
            pairsSrc.push_back(src[srcIdx]);
            pairsDst.emplace_back(targetXYZ[dstIdx * 3 + 0],
                                  targetXYZ[dstIdx * 3 + 1],
                                  targetXYZ[dstIdx * 3 + 2]);
            err += std::sqrt(dists[i].first);
        }
        err /= keep;

        procrustes(pairsSrc, pairsDst, R, t);

        if (std::fabs(prevError - err) < 1e-5) break;
        prevError = static_cast<float>(err);
    }

    if (outRotation) for (int i = 0; i < 9; ++i) outRotation[i] = R[i];
    if (outTranslation) for (int i = 0; i < 3; ++i) outTranslation[i] = t[i];
    return prevError;
}
