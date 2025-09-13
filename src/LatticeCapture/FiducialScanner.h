#pragma once

#include <opencv2/opencv.hpp>
#include <vector>

#include "Math/Pixel.h"
#include "Math/Vec2.h"
#include "Math/Vec3.h"

namespace lattice {

struct FiducialHit {
    int id = -1;
    std::vector<Vec2f> imageCorners;
    std::vector<Vec3f> localCorners;
};

class FiducialScanner {
public:
    FiducialScanner();

    bool scan(const Pixel* pixels, int width, int height, FiducialHit& out);

private:
    bool scanMat(cv::Mat& gray, FiducialHit& out);
    bool reorderCw(std::vector<cv::Point2f>& corners) const;
    int  decodeId(const cv::Mat& warped) const;
    void refineCorners(cv::Mat& gray, std::vector<cv::Point2f>& corners) const;
    void modelCorners(std::vector<Vec3f>& out) const;

    int     m_minSide;
    int     m_maxSide;
    int     m_threshold;
    double  m_approxPolyEps;
    double  m_borderFraction;
};

}
