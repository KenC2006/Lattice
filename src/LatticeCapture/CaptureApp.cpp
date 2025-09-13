#include "CaptureApp.h"

#include <chrono>
#include <cstring>

#include "Protocol/Wire.h"

namespace lattice {

namespace {
constexpr int kColorPixels  = 1920 * 1080;
constexpr int kDepthPixels  = 512 * 424;
constexpr float kZNearLimit = 0.30f;
constexpr float kZFarLimit  = 4.50f;
}

CaptureApp::CaptureApp() = default;

CaptureApp::~CaptureApp() {
    m_running = false;
    if (m_net.joinable()) m_net.join();
}

int CaptureApp::run(const std::string& hubHost) {
    if (!m_sensor.open()) return 1;

    while (!m_link.dial(hubHost, wire::kCapturePort) && m_running) {
        std::this_thread::sleep_for(std::chrono::seconds(1));
    }
    m_anchor.load("anchor.dat");

    m_net = std::thread([this]{ networkLoop(); });

    while (m_running) {
        if (!m_sensor.pullFrame()) {
            std::this_thread::sleep_for(std::chrono::milliseconds(5));
            continue;
        }
        if (m_calibRequested.exchange(false) || !m_anchor.locked) {
            std::vector<Vec3f> cameraPoints(kColorPixels);
            m_sensor.mapColorToCamera(cameraPoints.data());
            if (m_anchor.ingest(m_sensor.buffers().color.data(), cameraPoints.data(),
                                m_sensor.buffers().colorW, m_sensor.buffers().colorH)) {
                m_anchor.persist("anchor.dat");
                emitAck(static_cast<uint8_t>(wire::Outbound::CalibrationAck));
            }
        }
        produceFrame();
        if (m_grabRequested.exchange(false)) {
            std::lock_guard<std::mutex> lk(m_state);
            m_storedClouds.push_back(m_lastCloud);
            sendFrame(m_lastCloud);
            emitAck(static_cast<uint8_t>(wire::Outbound::GrabAck));
        }
    }
    m_link.hangup();
    return 0;
}

void CaptureApp::produceFrame() {
    const auto& buf = m_sensor.buffers();
    std::vector<Vec3f> camera(buf.colorW * buf.colorH);
    m_sensor.mapColorToCamera(camera.data());

    PointCloud cloud;
    cloud.reserve(buf.colorW * buf.colorH / 4);

    for (int i = 0; i < buf.colorW * buf.colorH; ++i) {
        const Vec3f& p = camera[i];
        if (!std::isfinite(p.x) || !std::isfinite(p.z)) continue;
        if (p.z < kZNearLimit || p.z > kZFarLimit) continue;

        Vec3f w = m_anchor.locked ? m_anchor.worldFromSensor.apply(p) : p;
        if (w.x < m_settings.minBounds.x || w.x > m_settings.maxBounds.x) continue;
        if (w.y < m_settings.minBounds.y || w.y > m_settings.maxBounds.y) continue;
        if (w.z < m_settings.minBounds.z || w.z > m_settings.maxBounds.z) continue;

        cloud.push(w, buf.color[i]);
    }

    std::lock_guard<std::mutex> lk(m_state);
    m_lastCloud = std::move(cloud);
}

void CaptureApp::sendFrame(const PointCloud& cloud) {
    wire::FrameHeader hdr{};
    hdr.magic = wire::kProtocolMagic;
    hdr.version = wire::kProtocolVersion;
    hdr.sensorId = 0;
    hdr.pointCount = static_cast<uint32_t>(cloud.size());
    hdr.captureTicks = static_cast<uint64_t>(
        std::chrono::steady_clock::now().time_since_epoch().count());
    hdr.compressed = m_settings.compress ? 1 : 0;

    m_link.send(static_cast<uint8_t>(wire::Outbound::LastFrame));
    m_link.sendPod(hdr);

    std::vector<Vec3s> packed;
    packed.reserve(cloud.size());
    for (const auto& p : cloud.positions) packed.push_back(toMillimeters(p));

    m_link.send(reinterpret_cast<const uint8_t*>(packed.data()),
                packed.size() * sizeof(Vec3s));
    m_link.send(reinterpret_cast<const uint8_t*>(cloud.colors.data()),
                cloud.colors.size() * sizeof(Pixel));
}

void CaptureApp::emitAck(uint8_t kind) {
    m_link.send(kind);
}

void CaptureApp::networkLoop() {
    while (m_running) {
        if (!handleInbound()) {
            std::this_thread::sleep_for(std::chrono::milliseconds(2));
        }
    }
}

bool CaptureApp::handleInbound() {
    uint8_t op = 0;
    if (!m_link.recv(&op, 1)) return false;

    switch (static_cast<wire::Inbound>(op)) {
        case wire::Inbound::GrabFrame:
            m_grabRequested = true;
            break;
        case wire::Inbound::RunCalibration:
            m_calibRequested = true;
            break;
        case wire::Inbound::ApplySettings:
            readSettings();
            break;
        case wire::Inbound::FetchLast:
            sendFrame(m_lastCloud);
            break;
        case wire::Inbound::FetchStored: {
            std::lock_guard<std::mutex> lk(m_state);
            if (!m_storedClouds.empty()) {
                sendFrame(m_storedClouds.front());
                m_storedClouds.erase(m_storedClouds.begin());
            }
            break;
        }
        case wire::Inbound::PushCalibration:
            readWorldTransform();
            break;
        case wire::Inbound::DropStored: {
            std::lock_guard<std::mutex> lk(m_state);
            m_storedClouds.clear();
            break;
        }
    }
    return true;
}

void CaptureApp::readSettings() {
    m_link.recvPod(m_settings);
}

void CaptureApp::readWorldTransform() {
    RigidTransform T;
    m_link.recvPod(T);
    int markerId = -1;
    m_link.recvPod(markerId);
    m_anchor.worldFromSensor = T;
    m_anchor.selectedId = markerId;
    m_anchor.locked = true;
    m_anchor.persist("anchor.dat");
}

}
