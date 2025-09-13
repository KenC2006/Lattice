#include "FrameAnchor.h"

#include <cmath>
#include <cstdio>
#include <fstream>
#include <opencv2/core.hpp>

namespace lattice {

namespace {
constexpr int kSamplesNeeded = 20;
}

FrameAnchor::FrameAnchor() : m_required(kSamplesNeeded) {
    m_samples.resize(4);
}

bool FrameAnchor::ingest(const Pixel* color, const Vec3f* cameraPoints,
                         int colorW, int colorH) {
    if (locked) return true;

    FiducialHit hit;
    if (!m_scanner.scan(color, colorW, colorH, hit)) {
        return false;
    }

    std::vector<Vec3f> corners3d;
    if (!lookupCorner3d(hit, cameraPoints, colorW, colorH, corners3d)) {
        return false;
    }

    for (int i = 0; i < 4; ++i) {
        m_samples[i].push_back(corners3d[i]);
    }
    ++m_collected;

    if (m_collected < m_required) return false;

    std::vector<Vec3f> averaged(4);
    for (int i = 0; i < 4; ++i) {
        Vec3f acc(0, 0, 0);
        for (const auto& s : m_samples[i]) acc = acc + s;
        averaged[i] = acc * (1.0f / m_collected);
    }

    RigidTransform T;
    procrustes(averaged, hit.localCorners, T);

    AnchorPose ap;
    ap.fiducialId = hit.id;
    ap.pose = T;
    anchors.push_back(ap);

    if (selectedId < 0 || hit.id == selectedId) {
        worldFromSensor = T;
        selectedId = hit.id;
        locked = true;
    }
    return locked;
}

bool FrameAnchor::lookupCorner3d(const FiducialHit& hit, const Vec3f* cameraPoints,
                                  int colorW, int colorH, std::vector<Vec3f>& out) const {
    out.clear();
    for (const auto& corner : hit.imageCorners) {
        const int u = static_cast<int>(std::round(corner.x));
        const int v = static_cast<int>(std::round(corner.y));
        if (u < 0 || u >= colorW || v < 0 || v >= colorH) return false;
        const Vec3f& p = cameraPoints[v * colorW + u];
        if (!std::isfinite(p.x) || !std::isfinite(p.y) || !std::isfinite(p.z)) return false;
        if (p.z <= 0.05f) return false;
        out.push_back(p);
    }
    return out.size() == hit.imageCorners.size();
}

void FrameAnchor::procrustes(const std::vector<Vec3f>& src, const std::vector<Vec3f>& dst,
                              RigidTransform& T) const {
    const size_t n = src.size();
    Vec3f cs(0, 0, 0), cd(0, 0, 0);
    for (size_t i = 0; i < n; ++i) {
        cs = cs + src[i];
        cd = cd + dst[i];
    }
    cs = cs * (1.0f / n);
    cd = cd * (1.0f / n);

    cv::Mat H = cv::Mat::zeros(3, 3, CV_64F);
    for (size_t i = 0; i < n; ++i) {
        const Vec3f a = src[i] - cs;
        const Vec3f b = dst[i] - cd;
        const double av[3] = { a.x, a.y, a.z };
        const double bv[3] = { b.x, b.y, b.z };
        for (int r = 0; r < 3; ++r)
            for (int c = 0; c < 3; ++c)
                H.at<double>(r, c) += av[r] * bv[c];
    }

    cv::Mat U, S, Vt;
    cv::SVD::compute(H, S, U, Vt);
    cv::Mat R = Vt.t() * U.t();
    if (cv::determinant(R) < 0) {
        cv::Mat correction = cv::Mat::eye(3, 3, CV_64F);
        correction.at<double>(2, 2) = -1.0;
        R = Vt.t() * correction * U.t();
    }

    for (int r = 0; r < 3; ++r)
        for (int c = 0; c < 3; ++c)
            T.R[r][c] = static_cast<float>(R.at<double>(r, c));

    const Vec3f rotated = T.applyRotation(cs);
    T.t = cd - rotated;
}

bool FrameAnchor::persist(const char* path) const {
    std::ofstream f(path);
    if (!f) return false;
    f << selectedId << '\n';
    for (int r = 0; r < 3; ++r)
        f << worldFromSensor.R[r][0] << ' '
          << worldFromSensor.R[r][1] << ' '
          << worldFromSensor.R[r][2] << '\n';
    f << worldFromSensor.t.x << ' '
      << worldFromSensor.t.y << ' '
      << worldFromSensor.t.z << '\n';
    return f.good();
}

bool FrameAnchor::load(const char* path) {
    std::ifstream f(path);
    if (!f) return false;
    f >> selectedId;
    for (int r = 0; r < 3; ++r)
        f >> worldFromSensor.R[r][0] >> worldFromSensor.R[r][1] >> worldFromSensor.R[r][2];
    f >> worldFromSensor.t.x >> worldFromSensor.t.y >> worldFromSensor.t.z;
    locked = f.good();
    return locked;
}

}
