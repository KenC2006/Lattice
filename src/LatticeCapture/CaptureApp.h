#pragma once

#include <atomic>
#include <mutex>
#include <string>
#include <thread>
#include <vector>

#include "FrameAnchor.h"
#include "HubLink.h"
#include "Math/Pixel.h"
#include "Math/Vec3.h"
#include "PointCloud.h"
#include "SensorBridge.h"

namespace lattice {

struct CaptureSettings {
    Vec3f minBounds = Vec3f(-2.0f, -2.0f,  0.3f);
    Vec3f maxBounds = Vec3f( 2.0f,  2.0f,  4.0f);
    bool  filter    = true;
    bool  bodyOnly  = false;
    int   markerId  = -1;
    bool  compress  = true;
};

class CaptureApp {
public:
    CaptureApp();
    ~CaptureApp();

    int run(const std::string& hubHost);

private:
    void networkLoop();
    void produceFrame();
    void sendFrame(const PointCloud& cloud);
    void emitAck(uint8_t kind);
    bool handleInbound();
    void readSettings();
    void readWorldTransform();

    SensorBridge       m_sensor;
    FrameAnchor        m_anchor;
    HubLink            m_link;
    CaptureSettings    m_settings;
    PointCloud         m_lastCloud;
    std::vector<PointCloud> m_storedClouds;
    std::mutex         m_state;
    std::atomic<bool>  m_running { true };
    std::atomic<bool>  m_grabRequested { false };
    std::atomic<bool>  m_calibRequested { false };
    std::thread        m_net;
};

}
