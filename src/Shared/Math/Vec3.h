#pragma once

#include <cmath>
#include <cstdint>

namespace lattice {

template <typename T>
struct Vec3 {
    T x;
    T y;
    T z;

    Vec3() : x(T(0)), y(T(0)), z(T(0)) {}
    Vec3(T xv, T yv, T zv) : x(xv), y(yv), z(zv) {}

    Vec3 operator+(const Vec3& rhs) const { return Vec3(x + rhs.x, y + rhs.y, z + rhs.z); }
    Vec3 operator-(const Vec3& rhs) const { return Vec3(x - rhs.x, y - rhs.y, z - rhs.z); }
    Vec3 operator*(T s) const { return Vec3(x * s, y * s, z * s); }

    T dot(const Vec3& o) const { return x * o.x + y * o.y + z * o.z; }
    T sqrMagnitude() const { return x * x + y * y + z * z; }
    T magnitude() const { return std::sqrt(static_cast<double>(sqrMagnitude())); }
};

using Vec3f = Vec3<float>;
using Vec3s = Vec3<int16_t>;
using Vec2f = Vec3<float>;

inline Vec3s toMillimeters(const Vec3f& metres) {
    return Vec3s(
        static_cast<int16_t>(metres.x * 1000.0f),
        static_cast<int16_t>(metres.y * 1000.0f),
        static_cast<int16_t>(metres.z * 1000.0f)
    );
}

}
