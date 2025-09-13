#pragma once

#include <Kinect.h>
#include <cstdint>
#include <vector>

#include "Math/Pixel.h"
#include "Math/Vec2.h"
#include "Math/Vec3.h"
#include "Skeleton.h"

namespace lattice {

struct SensorBuffers {
    int colorW = 0;
    int colorH = 0;
    int depthW = 0;
    int depthH = 0;

    std::vector<uint16_t> depth;
    std::vector<uint8_t>  bodyIndex;
    std::vector<Pixel>    color;
    std::vector<SkeletonFrame> bodies;
};

class SensorBridge {
public:
    SensorBridge();
    ~SensorBridge();

    bool open();
    bool pullFrame();

    void mapDepthToCamera(Vec3f* out) const;
    void mapColorToCamera(Vec3f* out) const;
    void mapDepthToColor(Vec2f* out) const;
    void mapColorToDepth(Vec2f* out) const;

    const SensorBuffers& buffers() const { return m_buf; }
    SensorBuffers& buffers() { return m_buf; }
    bool ready() const { return m_ready; }

private:
    void readDepth(IMultiSourceFrame* multi);
    void readColor(IMultiSourceFrame* multi);
    void readBodies(IMultiSourceFrame* multi);
    void readBodyIndex(IMultiSourceFrame* multi);

    IKinectSensor*           m_sensor   = nullptr;
    ICoordinateMapper*       m_mapper   = nullptr;
    IMultiSourceFrameReader* m_reader   = nullptr;
    bool                     m_ready    = false;
    SensorBuffers            m_buf;
};

}
