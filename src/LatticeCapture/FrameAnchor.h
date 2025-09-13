#pragma once

#include <vector>

#include "FiducialScanner.h"
#include "Math/Pixel.h"
#include "Math/RigidTransform.h"
#include "Math/Vec3.h"

namespace lattice {

struct AnchorPose {
    int fiducialId = -1;
    RigidTransform pose;
};

class FrameAnchor {
public:
    FrameAnchor();

    bool ingest(const Pixel* color, const Vec3f* cameraPoints,
                int colorW, int colorH);
    bool persist(const char* path) const;
    bool load(const char* path);

    bool                 locked   = false;
    int                  selectedId = -1;
    RigidTransform       worldFromSensor;
    std::vector<AnchorPose> anchors;

private:
    bool lookupCorner3d(const FiducialHit& hit, const Vec3f* cameraPoints,
                        int colorW, int colorH, std::vector<Vec3f>& out) const;
    void procrustes(const std::vector<Vec3f>& src, const std::vector<Vec3f>& dst,
                    RigidTransform& T) const;

    FiducialScanner m_scanner;
    int             m_collected = 0;
    int             m_required;
    std::vector<std::vector<Vec3f>> m_samples;
};

}
