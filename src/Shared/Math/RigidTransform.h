#pragma once

#include <array>
#include <vector>

#include "Vec3.h"

namespace lattice {

struct RigidTransform {
    std::array<std::array<float, 3>, 3> R;
    Vec3f t;

    RigidTransform() {
        R = {{ {{1.0f, 0.0f, 0.0f}},
               {{0.0f, 1.0f, 0.0f}},
               {{0.0f, 0.0f, 1.0f}} }};
    }

    Vec3f apply(const Vec3f& p) const {
        Vec3f q;
        q.x = R[0][0] * p.x + R[0][1] * p.y + R[0][2] * p.z + t.x;
        q.y = R[1][0] * p.x + R[1][1] * p.y + R[1][2] * p.z + t.y;
        q.z = R[2][0] * p.x + R[2][1] * p.y + R[2][2] * p.z + t.z;
        return q;
    }

    Vec3f applyRotation(const Vec3f& p) const {
        Vec3f q;
        q.x = R[0][0] * p.x + R[0][1] * p.y + R[0][2] * p.z;
        q.y = R[1][0] * p.x + R[1][1] * p.y + R[1][2] * p.z;
        q.z = R[2][0] * p.x + R[2][1] * p.y + R[2][2] * p.z;
        return q;
    }

    Vec3f inverseRotate(const Vec3f& p) const {
        Vec3f q;
        q.x = R[0][0] * p.x + R[1][0] * p.y + R[2][0] * p.z;
        q.y = R[0][1] * p.x + R[1][1] * p.y + R[2][1] * p.z;
        q.z = R[0][2] * p.x + R[1][2] * p.y + R[2][2] * p.z;
        return q;
    }
};

}
