#pragma once

#include <cstdint>

namespace lattice::wire {

inline constexpr uint16_t kCapturePort   = 48201;
inline constexpr uint16_t kBroadcastPort = 48202;

inline constexpr uint32_t kProtocolMagic   = 0x4C415454u;
inline constexpr uint16_t kProtocolVersion = 1;

enum class Inbound : uint8_t {
    GrabFrame        = 0,
    RunCalibration   = 1,
    ApplySettings    = 2,
    FetchStored      = 3,
    FetchLast        = 4,
    PushCalibration  = 5,
    DropStored       = 6,
};

enum class Outbound : uint8_t {
    GrabAck          = 0,
    CalibrationAck   = 1,
    StoredFrame      = 2,
    LastFrame        = 3,
};

#pragma pack(push, 1)
struct FrameHeader {
    uint32_t magic;
    uint16_t version;
    uint16_t sensorId;
    uint32_t pointCount;
    uint64_t captureTicks;
    uint8_t  compressed;
    uint8_t  reserved[3];
};
#pragma pack(pop)

}
