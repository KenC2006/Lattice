#pragma once

#include <vector>

#include "Math/Pixel.h"
#include "Math/Vec3.h"

namespace lattice {

struct PointCloud {
    std::vector<Vec3f> positions;
    std::vector<Pixel> colors;

    void clear() {
        positions.clear();
        colors.clear();
    }

    size_t size() const { return positions.size(); }
    bool empty() const { return positions.empty(); }

    void reserve(size_t n) {
        positions.reserve(n);
        colors.reserve(n);
    }

    void push(const Vec3f& p, Pixel c) {
        positions.push_back(p);
        colors.push_back(c);
    }
};

}
