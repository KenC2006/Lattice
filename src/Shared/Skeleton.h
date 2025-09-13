#pragma once

#include <array>

#include "Math/Vec3.h"

namespace lattice {

enum class JointId : uint8_t {
    SpineBase = 0, SpineMid, Neck, Head,
    ShoulderLeft, ElbowLeft, WristLeft, HandLeft,
    ShoulderRight, ElbowRight, WristRight, HandRight,
    HipLeft, KneeLeft, AnkleLeft, FootLeft,
    HipRight, KneeRight, AnkleRight, FootRight,
    SpineShoulder, HandTipLeft, ThumbLeft, HandTipRight, ThumbRight,
    kCount
};

enum class TrackState : uint8_t {
    Missing = 0,
    Inferred = 1,
    Confident = 2,
};

struct Joint {
    Vec3f position;
    TrackState state;
    Joint() : state(TrackState::Missing) {}
};

struct SkeletonFrame {
    uint64_t trackingId;
    std::array<Joint, static_cast<size_t>(JointId::kCount)> joints;
    bool tracked;
    SkeletonFrame() : trackingId(0), tracked(false) {}
};

}
