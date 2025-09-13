#include "SensorBridge.h"

#include <chrono>
#include <thread>

namespace lattice {

namespace {

template <class T>
void safeRelease(T*& p) {
    if (p) {
        p->Release();
        p = nullptr;
    }
}

constexpr int kColorWidth = 1920;
constexpr int kColorHeight = 1080;
constexpr int kDepthWidth = 512;
constexpr int kDepthHeight = 424;

}

SensorBridge::SensorBridge() = default;

SensorBridge::~SensorBridge() {
    safeRelease(m_reader);
    safeRelease(m_mapper);
    if (m_sensor) {
        m_sensor->Close();
        safeRelease(m_sensor);
    }
}

bool SensorBridge::open() {
    if (FAILED(GetDefaultKinectSensor(&m_sensor)) || !m_sensor) {
        return false;
    }

    if (FAILED(m_sensor->Open())) {
        return false;
    }

    m_sensor->get_CoordinateMapper(&m_mapper);

    const auto sources =
        FrameSourceTypes_Color | FrameSourceTypes_Depth |
        FrameSourceTypes_Body | FrameSourceTypes_BodyIndex;

    if (FAILED(m_sensor->OpenMultiSourceFrameReader(sources, &m_reader))) {
        return false;
    }

    m_buf.colorW = kColorWidth;
    m_buf.colorH = kColorHeight;
    m_buf.depthW = kDepthWidth;
    m_buf.depthH = kDepthHeight;
    m_buf.depth.assign(kDepthWidth * kDepthHeight, 0);
    m_buf.bodyIndex.assign(kDepthWidth * kDepthHeight, 0xFF);
    m_buf.color.assign(kColorWidth * kColorHeight, Pixel{});

    const auto deadline = std::chrono::steady_clock::now() + std::chrono::seconds(5);
    while (std::chrono::steady_clock::now() < deadline) {
        if (pullFrame()) {
            m_ready = true;
            return true;
        }
        std::this_thread::sleep_for(std::chrono::milliseconds(33));
    }
    return false;
}

bool SensorBridge::pullFrame() {
    if (!m_reader) return false;

    IMultiSourceFrame* multi = nullptr;
    if (FAILED(m_reader->AcquireLatestFrame(&multi)) || !multi) {
        return false;
    }

    readDepth(multi);
    readColor(multi);
    readBodies(multi);
    readBodyIndex(multi);

    safeRelease(multi);
    return true;
}

void SensorBridge::readDepth(IMultiSourceFrame* multi) {
    IDepthFrameReference* ref = nullptr;
    if (FAILED(multi->get_DepthFrameReference(&ref))) return;

    IDepthFrame* frame = nullptr;
    if (SUCCEEDED(ref->AcquireFrame(&frame)) && frame) {
        UINT cap = 0;
        UINT16* raw = nullptr;
        if (SUCCEEDED(frame->AccessUnderlyingBuffer(&cap, &raw)) && raw) {
            std::copy(raw, raw + cap, m_buf.depth.begin());
        }
        safeRelease(frame);
    }
    safeRelease(ref);
}

void SensorBridge::readColor(IMultiSourceFrame* multi) {
    IColorFrameReference* ref = nullptr;
    if (FAILED(multi->get_ColorFrameReference(&ref))) return;

    IColorFrame* frame = nullptr;
    if (SUCCEEDED(ref->AcquireFrame(&frame)) && frame) {
        frame->CopyConvertedFrameDataToArray(
            static_cast<UINT>(m_buf.color.size() * sizeof(Pixel)),
            reinterpret_cast<BYTE*>(m_buf.color.data()),
            ColorImageFormat_Bgra);
        safeRelease(frame);
    }
    safeRelease(ref);
}

void SensorBridge::readBodyIndex(IMultiSourceFrame* multi) {
    IBodyIndexFrameReference* ref = nullptr;
    if (FAILED(multi->get_BodyIndexFrameReference(&ref))) return;

    IBodyIndexFrame* frame = nullptr;
    if (SUCCEEDED(ref->AcquireFrame(&frame)) && frame) {
        UINT cap = 0;
        BYTE* raw = nullptr;
        if (SUCCEEDED(frame->AccessUnderlyingBuffer(&cap, &raw)) && raw) {
            std::copy(raw, raw + cap, m_buf.bodyIndex.begin());
        }
        safeRelease(frame);
    }
    safeRelease(ref);
}

void SensorBridge::readBodies(IMultiSourceFrame* multi) {
    IBodyFrameReference* ref = nullptr;
    if (FAILED(multi->get_BodyFrameReference(&ref))) return;

    IBodyFrame* frame = nullptr;
    if (SUCCEEDED(ref->AcquireFrame(&frame)) && frame) {
        IBody* bodies[BODY_COUNT] = { nullptr };
        if (SUCCEEDED(frame->GetAndRefreshBodyData(BODY_COUNT, bodies))) {
            m_buf.bodies.clear();
            m_buf.bodies.reserve(BODY_COUNT);
            for (int i = 0; i < BODY_COUNT; ++i) {
                IBody* b = bodies[i];
                SkeletonFrame sf;
                BOOLEAN tracked = FALSE;
                if (b && SUCCEEDED(b->get_IsTracked(&tracked)) && tracked) {
                    sf.tracked = true;
                    UINT64 id = 0;
                    b->get_TrackingId(&id);
                    sf.trackingId = id;

                    ::Joint joints[JointType_Count];
                    if (SUCCEEDED(b->GetJoints(JointType_Count, joints))) {
                        for (int j = 0; j < JointType_Count && j < static_cast<int>(JointId::kCount); ++j) {
                            auto& dst = sf.joints[j];
                            dst.position = Vec3f(joints[j].Position.X, joints[j].Position.Y, joints[j].Position.Z);
                            switch (joints[j].TrackingState) {
                                case TrackingState_NotTracked: dst.state = TrackState::Missing; break;
                                case TrackingState_Inferred:   dst.state = TrackState::Inferred; break;
                                case TrackingState_Tracked:    dst.state = TrackState::Confident; break;
                            }
                        }
                    }
                }
                m_buf.bodies.push_back(sf);
                safeRelease(bodies[i]);
            }
        }
        safeRelease(frame);
    }
    safeRelease(ref);
}

void SensorBridge::mapDepthToCamera(Vec3f* out) const {
    if (!m_mapper) return;
    const UINT n = m_buf.depthW * m_buf.depthH;
    m_mapper->MapDepthFrameToCameraSpace(
        n, m_buf.depth.data(),
        n, reinterpret_cast<CameraSpacePoint*>(out));
}

void SensorBridge::mapColorToCamera(Vec3f* out) const {
    if (!m_mapper) return;
    m_mapper->MapColorFrameToCameraSpace(
        static_cast<UINT>(m_buf.depth.size()), m_buf.depth.data(),
        static_cast<UINT>(m_buf.colorW * m_buf.colorH),
        reinterpret_cast<CameraSpacePoint*>(out));
}

void SensorBridge::mapDepthToColor(Vec2f* out) const {
    if (!m_mapper) return;
    m_mapper->MapDepthFrameToColorSpace(
        static_cast<UINT>(m_buf.depth.size()), m_buf.depth.data(),
        static_cast<UINT>(m_buf.depthW * m_buf.depthH),
        reinterpret_cast<ColorSpacePoint*>(out));
}

void SensorBridge::mapColorToDepth(Vec2f* out) const {
    if (!m_mapper) return;
    m_mapper->MapColorFrameToDepthSpace(
        static_cast<UINT>(m_buf.depth.size()), m_buf.depth.data(),
        static_cast<UINT>(m_buf.colorW * m_buf.colorH),
        reinterpret_cast<DepthSpacePoint*>(out));
}

}
