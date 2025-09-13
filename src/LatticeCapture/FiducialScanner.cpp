#include "FiducialScanner.h"

#include <algorithm>

namespace lattice {

namespace {
constexpr int kWarpSize = 64;
constexpr int kGrid = 6;
constexpr float kTileMm = 50.0f;
}

FiducialScanner::FiducialScanner()
    : m_minSide(40), m_maxSide(800),
      m_threshold(110), m_approxPolyEps(0.03), m_borderFraction(0.18) {}

void FiducialScanner::modelCorners(std::vector<Vec3f>& out) const {
    const float h = kTileMm * 0.001f * 0.5f;
    out = {
        Vec3f(-h,  h, 0.0f),
        Vec3f( h,  h, 0.0f),
        Vec3f( h, -h, 0.0f),
        Vec3f(-h, -h, 0.0f),
    };
}

bool FiducialScanner::scan(const Pixel* pixels, int width, int height, FiducialHit& out) {
    cv::Mat src(height, width, CV_8UC4, const_cast<Pixel*>(pixels));
    cv::Mat gray;
    cv::cvtColor(src, gray, cv::COLOR_BGRA2GRAY);
    return scanMat(gray, out);
}

bool FiducialScanner::scanMat(cv::Mat& gray, FiducialHit& out) {
    cv::Mat binary;
    cv::threshold(gray, binary, m_threshold, 255, cv::THRESH_BINARY_INV);

    std::vector<std::vector<cv::Point>> contours;
    cv::findContours(binary, contours, cv::RETR_LIST, cv::CHAIN_APPROX_SIMPLE);

    for (auto& contour : contours) {
        const double peri = cv::arcLength(contour, true);
        if (peri < 4 * m_minSide || peri > 4 * m_maxSide) continue;

        std::vector<cv::Point2f> poly;
        cv::approxPolyDP(contour, poly, m_approxPolyEps * peri, true);
        if (poly.size() != 4 || !cv::isContourConvex(poly)) continue;

        if (!reorderCw(poly)) continue;
        refineCorners(gray, poly);

        std::vector<cv::Point2f> canonical = {
            {0.0f, 0.0f},
            {static_cast<float>(kWarpSize - 1), 0.0f},
            {static_cast<float>(kWarpSize - 1), static_cast<float>(kWarpSize - 1)},
            {0.0f, static_cast<float>(kWarpSize - 1)},
        };
        cv::Mat H = cv::getPerspectiveTransform(poly, canonical);
        cv::Mat warped;
        cv::warpPerspective(gray, warped, H, cv::Size(kWarpSize, kWarpSize));
        cv::threshold(warped, warped, 0, 255, cv::THRESH_BINARY | cv::THRESH_OTSU);

        const int code = decodeId(warped);
        if (code < 0) continue;

        out.id = code;
        out.imageCorners.clear();
        for (const auto& p : poly) out.imageCorners.emplace_back(p.x, p.y);
        modelCorners(out.localCorners);
        return true;
    }
    return false;
}

bool FiducialScanner::reorderCw(std::vector<cv::Point2f>& corners) const {
    if (corners.size() != 4) return false;
    cv::Point2f centroid(0, 0);
    for (auto& c : corners) centroid += c;
    centroid *= 0.25f;
    std::sort(corners.begin(), corners.end(), [&](const cv::Point2f& a, const cv::Point2f& b) {
        return std::atan2(a.y - centroid.y, a.x - centroid.x) <
               std::atan2(b.y - centroid.y, b.x - centroid.x);
    });
    return true;
}

void FiducialScanner::refineCorners(cv::Mat& gray, std::vector<cv::Point2f>& corners) const {
    cv::cornerSubPix(
        gray, corners, cv::Size(5, 5), cv::Size(-1, -1),
        cv::TermCriteria(cv::TermCriteria::EPS + cv::TermCriteria::COUNT, 30, 0.01));
}

int FiducialScanner::decodeId(const cv::Mat& warped) const {
    const int cellSize = kWarpSize / kGrid;
    const int border = static_cast<int>(m_borderFraction * kGrid + 0.5);

    for (int i = 0; i < kGrid; ++i) {
        for (int j = 0; j < kGrid; ++j) {
            const bool isBorder = i < border || j < border ||
                                  i >= kGrid - border || j >= kGrid - border;
            if (!isBorder) continue;
            cv::Rect tile(j * cellSize, i * cellSize, cellSize, cellSize);
            if (cv::mean(warped(tile))[0] > 64.0) return -1;
        }
    }

    int id = 0;
    int bit = 0;
    for (int i = border; i < kGrid - border; ++i) {
        for (int j = border; j < kGrid - border; ++j) {
            cv::Rect tile(j * cellSize, i * cellSize, cellSize, cellSize);
            if (cv::mean(warped(tile))[0] > 128.0) id |= (1 << bit);
            ++bit;
        }
    }
    return id;
}

}
